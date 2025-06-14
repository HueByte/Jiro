using System.Collections.Concurrent;
using System.Text;

using Google.Protobuf;

using Grpc.Core;
using Grpc.Net.ClientFactory;

using Jiro.Commands.Models;
using Jiro.Core.Services.CommandContext;
using Jiro.Core.Services.CommandHandler;

using JiroCloud.Api.Proto;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using static JiroCloud.Api.Proto.JiroHubProto;

namespace Jiro.App;

internal class JiroClientService : IHostedService
{
	private const string SERVER_ALIVE = "SERVER_ALIVE";
	private const string CLIENT_ALIVE = "CLIENT_ALIVE";
	private const int BASE_RECONNECTION_TIME = 10_000;
	private const int ALIVE_PING_TIME = 60_000;
	private const int MAX_RETRY_COUNT = 5;
	private readonly IServiceScopeFactory _scopeFactory;
	private readonly ILogger<JiroClientService> _logger;
	private readonly ICommandHandlerService _commandHandler;
	private readonly SemaphoreSlim _semaphore = new(1, 1);
	private readonly ConcurrentDictionary<string, Task> _commandQueue = new();
	private CancellationToken _cancellationToken;
	private int _retryCount;

	public JiroClientService (IServiceScopeFactory scopeFactory, ILogger<JiroClientService> logger, ICommandHandlerService commandHandlerService)
	{
		_scopeFactory = scopeFactory;
		_logger = logger;
		_commandHandler = commandHandlerService;
	}

	public async Task StartAsync (CancellationToken cancellationToken)
	{
		//await SayHello();
		_cancellationToken = cancellationToken;

		do
		{
			AsyncDuplexStreamingCall<ClientMessage, ServerMessage>? callInstance = null;
			try
			{
				await using var connectionScope = _scopeFactory.CreateAsyncScope();
				var clientFactory = connectionScope.ServiceProvider.GetRequiredService<GrpcClientFactory>();
				var currentClient = clientFactory.CreateClient<JiroHubProtoClient>("JiroClient");

				Metadata connectionHeaders = new()
				{
					{ "Content-Type", "application/grpc" }
				};

				// Initialize the connection
				callInstance = currentClient.InstanceCommand(connectionHeaders, cancellationToken: cancellationToken);
				_logger.LogInformation("Connected to server");

				var serverListenTask = Task.Run(async () => await StartListeningLoopAsync(callInstance), cancellationToken);
				var keepAliveTask = Task.Run(async () => await StartKeepAliveLoopAsync(callInstance), cancellationToken);

				await serverListenTask;
				await callInstance.RequestStream.CompleteAsync();
			}
			catch (Exception ex) when (ex is TaskCanceledException
				|| ex is OperationCanceledException
				|| ex.InnerException is TaskCanceledException
				|| ex.InnerException is OperationCanceledException)
			{
				_logger.LogInformation("Task or operation cancelled. Closing hosted client service");
			}
			catch (Exception ex) when (_retryCount < MAX_RETRY_COUNT)
			{
				_logger.LogError(ex, "Something went wrong");

				var nextRetryTime = BASE_RECONNECTION_TIME * (_retryCount + 1);
				_logger.LogInformation("Attempting to reconnect in {time} seconds", nextRetryTime / 1000);
				await Task.Delay(nextRetryTime, cancellationToken);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Something went wrong and max retry count reached. Exiting");
			}
			finally
			{

				if (callInstance is not null && callInstance.RequestStream is not null)
				{
					_logger.LogInformation("Disconnecting...");
					await callInstance.RequestStream.CompleteAsync();
				}

				_logger.LogInformation("Clearing command queue");
				_commandQueue.Clear();

				callInstance?.Dispose();
			}
		} while (!cancellationToken.IsCancellationRequested && _retryCount++ < MAX_RETRY_COUNT);
	}

	private async Task StartListeningLoopAsync (AsyncDuplexStreamingCall<ClientMessage, ServerMessage>? callInstance)
	{
		if (callInstance is null)
		{
			_logger.LogError("Call instance is null");
			return;
		}

		// Main command loop
		await foreach (var serverMessage in callInstance.ResponseStream.ReadAllAsync(_cancellationToken))
		{
			try
			{
				_logger.LogInformation("Received message from server [{Message}]", serverMessage.CommandSyncId);
				if (_commandQueue.TryGetValue(serverMessage.CommandSyncId, out _))
				{
					throw new Exception("Command already in queue");
				}

				if (serverMessage.CommandSyncId == SERVER_ALIVE)
					continue;

				// capture variables
				var scopedCommandSyncId = serverMessage.CommandSyncId;
				var instanceId = serverMessage.InstanceId ?? throw new InvalidOperationException("Instance ID is null in server message");
				var command = serverMessage.Command;
				var sessionId = serverMessage.SessionId ?? throw new InvalidOperationException("Session ID is null in server message");

				// Fire and forget execute command
				var commandTask = Task.Run(async () => await ExecuteCommandAsync(scopedCommandSyncId, instanceId, sessionId, command, callInstance), _cancellationToken);
				var enqueueResult = _commandQueue.TryAdd(scopedCommandSyncId, commandTask);
			}
			catch (Exception ex) when (ex is TaskCanceledException
				|| ex is OperationCanceledException
				|| ex.InnerException is TaskCanceledException
				|| ex.InnerException is OperationCanceledException)
			{
				_logger.LogInformation("Command task cancelled.");
			}
			catch (ObjectDisposedException ex)
			{
				_logger.LogError(ex, "Error while sending alive ping.");
				break;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error while processing command [{Message}].", serverMessage.CommandSyncId);
			}
		}
	}

	private async Task StartKeepAliveLoopAsync (AsyncDuplexStreamingCall<ClientMessage, ServerMessage>? callInstance)
	{
		while (!_cancellationToken.IsCancellationRequested)
		{
			try
			{
				if (callInstance is null)
					break;

				await WriteMessageToServer(callInstance, new ClientMessage() { CommandSyncId = CLIENT_ALIVE });
				_retryCount = 0;
				_logger.LogInformation("Alive ping {time} UTC", DateTime.UtcNow);

			}
			catch (Exception ex) when (ex is TaskCanceledException
				|| ex is OperationCanceledException
				|| ex.InnerException is TaskCanceledException
				|| ex.InnerException is OperationCanceledException)
			{
				_logger.LogInformation("Alive ping task cancelled.");
			}

			await Task.Delay(ALIVE_PING_TIME, _cancellationToken);
		}
	}

	private async Task ExecuteCommandAsync (string scopedCommandSyncId, string instanceId, string sessionId, string command, AsyncDuplexStreamingCall<ClientMessage, ServerMessage> callInstance)
	{
		try
		{
			await using var commandScope = _scopeFactory.CreateAsyncScope();
			var currentClient = commandScope.ServiceProvider.GetRequiredService<ICommandContext>();
			currentClient.SetCurrentInstance(instanceId);
			currentClient.SetSessionId(sessionId);

			var commandResult = await _commandHandler.ExecuteCommandAsync(commandScope.ServiceProvider, command);
			var commandResponse = CreateMessage(scopedCommandSyncId, commandResult);

			_logger.LogInformation("Sending command [{syncId}]", commandResponse.CommandSyncId);
			await WriteMessageToServer(callInstance, commandResponse);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error while executing command [{syncId}]", scopedCommandSyncId);
			throw;
		}
		finally
		{
			_commandQueue.TryRemove(scopedCommandSyncId, out _);
			_logger.LogInformation("Command execution finished");
		}
	}

	private ClientMessage CreateMessage (string syncId, CommandResponse commandResult)
	{
		// todo
		// use mapper later

		var commandType = GetCommandType(commandResult.CommandType);

		ClientMessage response = new()
		{
			CommandSyncId = syncId,
			CommandName = commandResult.CommandName,
			CommandType = commandType,
			IsSuccess = commandResult.IsSuccess
		};

		try
		{
			if (commandType == JiroCloud.Api.Proto.CommandType.Text)
			{
				response.TextResult = new()
				{
					Response = commandResult.Result?.Message
				};
			}
			else if (commandType == JiroCloud.Api.Proto.CommandType.Graph)
			{
				if (commandResult.Result is Jiro.Commands.Results.GraphResult graph)
				{
					response.GraphResult = new()
					{
						Message = graph.Message,
						Note = graph.Note,
						XAxis = graph.XAxis ?? "",
						YAxis = graph.YAxis ?? "",
						GraphData = ByteString.CopyFrom(graph.Data as string ?? "", Encoding.UTF8)
					};

					response.GraphResult.Units.Add(graph.Units);
				}
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error while creating message.");
			response.IsSuccess = false;
			response.CommandType = JiroCloud.Api.Proto.CommandType.Text;
			response.TextResult = new()
			{
				Response = "Error while creating message. Look at logs for more information."
			};
		}

		return response;
	}

	private async Task WriteMessageToServer (AsyncDuplexStreamingCall<ClientMessage, ServerMessage> stream, ClientMessage message)
	{
		await _semaphore.WaitAsync(_cancellationToken);
		try
		{
			await stream.RequestStream.WriteAsync(message, cancellationToken: _cancellationToken);
		}
		finally
		{
			_semaphore.Release();
		}
	}

	public Task StopAsync (CancellationToken cancellationToken)
	{
		return Task.CompletedTask;
	}

	private static JiroCloud.Api.Proto.CommandType GetCommandType (Jiro.Commands.CommandType commandType) => (int)commandType switch
	{
		0 => JiroCloud.Api.Proto.CommandType.Text,
		1 => JiroCloud.Api.Proto.CommandType.Graph,
		_ => JiroCloud.Api.Proto.CommandType.Text
	};
}
