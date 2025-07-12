using System.Text;

using Google.Protobuf;

using Grpc.Core;
using Grpc.Net.ClientFactory;

using Jiro.App.Options;
using Jiro.Commands.Models;
using Jiro.Commands.Results;

using JiroCloud.Api.Proto;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using static JiroCloud.Api.Proto.JiroHubProto;

namespace Jiro.App.Services;

/// <summary>
/// gRPC service implementation for sending command results back to the server
/// </summary>
internal class JiroGrpcService : IJiroGrpcService
{
	private readonly JiroHubProtoClient _client;
	private readonly ILogger<JiroGrpcService> _logger;
	private readonly GrpcOptions _options;

	public JiroGrpcService(
		GrpcClientFactory clientFactory,
		ILogger<JiroGrpcService> logger,
		IOptions<GrpcOptions> options)
	{
		_client = clientFactory.CreateClient<JiroHubProtoClient>("JiroClient");
		_logger = logger;
		_options = options.Value;
	}

	public async Task SendCommandResultAsync(string commandSyncId, CommandResponse commandResult)
	{
		try
		{
			var clientMessage = CreateMessage(commandSyncId, commandResult);
			await SendMessageWithRetryAsync(clientMessage);

			_logger.LogInformation("Command result sent successfully [{syncId}]", commandSyncId);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to send command result [{syncId}]", commandSyncId);
			throw;
		}
	}

	public async Task SendCommandErrorAsync(string commandSyncId, string errorMessage)
	{
		try
		{
			var errorResult = new CommandResponse
			{
				CommandName = "Error",
				CommandType = Jiro.Commands.CommandType.Text,
				IsSuccess = false,
				Result = Jiro.Commands.Results.TextResult.Create(errorMessage)
			};

			var clientMessage = CreateMessage(commandSyncId, errorResult);
			await SendMessageWithRetryAsync(clientMessage);

			_logger.LogInformation("Command error sent successfully [{syncId}]", commandSyncId);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to send command error [{syncId}]", commandSyncId);
			throw;
		}
	}

	private async Task SendMessageWithRetryAsync(ClientMessage message)
	{
		var retryCount = 0;
		Exception? lastException = null;

		while (retryCount <= _options.MaxRetries)
		{
			try
			{
				using var cancellationTokenSource = new CancellationTokenSource(_options.TimeoutMs);

				var response = await _client.SendCommandResultAsync(message,
					cancellationToken: cancellationTokenSource.Token);

				if (response.Success)
				{
					return; // Success
				}

				throw new InvalidOperationException($"Server returned unsuccessful response: {response.Message}");
			}
			catch (Exception ex) when (retryCount < _options.MaxRetries)
			{
				lastException = ex;
				retryCount++;

				var delay = TimeSpan.FromMilliseconds(1000 * Math.Pow(2, retryCount - 1)); // Exponential backoff
				_logger.LogWarning(ex, "Failed to send message, retrying in {delay}ms (attempt {attempt}/{max})",
					delay.TotalMilliseconds, retryCount, _options.MaxRetries);

				await Task.Delay(delay);
			}
		}

		throw new InvalidOperationException($"Failed to send message after {_options.MaxRetries} retries", lastException);
	}

	/// <summary>
	/// Creates a protobuf client message from a command response, handling different command result types.
	/// </summary>
	/// <param name="syncId">The synchronization ID for the command.</param>
	/// <param name="commandResult">The command execution result to serialize.</param>
	/// <returns>A protobuf client message ready for transmission.</returns>
	private ClientMessage CreateMessage(string syncId, CommandResponse commandResult)
	{
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

	/// <summary>
	/// Converts internal command types to protobuf command types for network communication.
	/// </summary>
	/// <param name="commandType">The internal command type to convert.</param>
	/// <returns>The corresponding protobuf command type.</returns>
	private static JiroCloud.Api.Proto.CommandType GetCommandType(Jiro.Commands.CommandType commandType) => (int)commandType switch
	{
		0 => JiroCloud.Api.Proto.CommandType.Text,
		1 => JiroCloud.Api.Proto.CommandType.Graph,
		_ => JiroCloud.Api.Proto.CommandType.Text
	};
}
