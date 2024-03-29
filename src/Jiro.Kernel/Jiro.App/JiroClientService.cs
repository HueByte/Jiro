﻿using Google.Protobuf;
using Grpc.Core;
using Grpc.Net.ClientFactory;
using Jiro.Commands;
using Jiro.Commands.Models;
using Jiro.Core.Interfaces.IServices;
using JiroCloud.Api.Proto;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text;
using static JiroCloud.Api.Proto.JiroHubProto;

namespace Jiro.App;

internal class JiroClientService : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<JiroClientService> _logger;
    private readonly ICommandHandlerService _commandHandler;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    public JiroClientService(IServiceScopeFactory scopeFactory, ILogger<JiroClientService> logger, ICommandHandlerService commandHandlerService)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _commandHandler = commandHandlerService;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        //await SayHello();

        do
        {
            try
            {
                Dictionary<string, Task> commandQueue = new();

                await using var connectionScope = _scopeFactory.CreateAsyncScope();
                var clientFactory = connectionScope.ServiceProvider.GetRequiredService<GrpcClientFactory>();
                var currentClient = clientFactory.CreateClient<JiroHubProtoClient>("JiroClient");

                Metadata connectionHeaders = new()
                {
                    { "Content-Type", "application/grpc" }
                };

                using var callInstance = currentClient.InstanceCommand(connectionHeaders, cancellationToken: cancellationToken);
                var listeningLoop = Task.Run(async () =>
                {
                    _logger.LogInformation("Connected to server");

                    await foreach (var serverMessage in callInstance.ResponseStream.ReadAllAsync())
                    {
                        _logger.LogInformation("Received message from server [{Message}]", serverMessage.CommandSyncId);
                        if (commandQueue.TryGetValue(serverMessage.CommandSyncId, out _))
                        {
                            throw new Exception("Command already in queue");
                        }

                        if (serverMessage.CommandSyncId == "dummy")
                            continue;

                        // capture variables
                        var scopedCommandSyncId = serverMessage.CommandSyncId;
                        var command = serverMessage.Command;
                        var instanceId = serverMessage.InstanceName;
                        var commandExecutionTask = Task.Run(async () =>
                        {
                            try
                            {
                                var commandResult = await ExecuteCommand(scopedCommandSyncId, instanceId, command);

                                _logger.LogInformation("Sending command [{syncId}]", commandResult.CommandSyncId);
                                await WriteMessageToServer(callInstance, commandResult);
                            }
                            finally
                            {
                                _logger.LogInformation("Command execution finished");
                            }
                        });
                    }

                    _logger.LogInformation("Disconnecting...");
                }, cancellationToken);

                var keepingAliveLoop = Task.Run(async () =>
                {
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        _logger.LogInformation("Alive ping");

                        await Task.Delay(60_000);
                        await WriteMessageToServer(callInstance, new ClientMessage() { CommandSyncId = "dummy" });
                    }
                }, cancellationToken);

                await listeningLoop;
                await keepingAliveLoop;
                await callInstance.RequestStream.CompleteAsync();
            }
            catch (RpcException ex)
            {
                _logger.LogError(ex, "bing bonk something wrong");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Connection failed");
            }
            finally
            {
                _logger.LogInformation("Attempting to reconnect in 5 seconds");
                await Task.Delay(10_000, cancellationToken);
            }
        } while (!cancellationToken.IsCancellationRequested);
    }

    private async Task WriteMessageToServer(AsyncDuplexStreamingCall<ClientMessage, ServerMessage> stream, ClientMessage message)
    {
        await _semaphore.WaitAsync();
        try
        {
            await stream.RequestStream.WriteAsync(message);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task<ClientMessage> ExecuteCommand(string syncId, string instanceId, string command)
    {
        await using var commandScope = _scopeFactory.CreateAsyncScope();
        var currentClient = commandScope.ServiceProvider.GetRequiredService<ICurrentUserService>();
        currentClient.SetCurrentUser(instanceId);

        var commandResult = await _commandHandler.ExecuteCommandAsync(commandScope.ServiceProvider, command);

        return CreateMessage(syncId, commandResult);
    }

    private ClientMessage CreateMessage(string syncId, CommandResponse commandResult)
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
                    XAxis = graph.XAxis,
                    YAxis = graph.YAxis,
                    GraphData = ByteString.CopyFrom(graph.Data as string, Encoding.UTF8)
                };

                response.GraphResult.Units.Add(graph.Units);
            }
        }

        return response;
    }

    // private async Task SayHello()
    // {

    //     Grpc.Core.Metadata headers = new Grpc.Core.Metadata
    //     {
    //         { "Content-Type", "application/grpc" }
    //     };

    //     await using var scope = _scopeFactory.CreateAsyncScope();
    //     var factory = scope.ServiceProvider.GetRequiredService<GrpcClientFactory>();

    //     var client = factory.CreateClient<JiroHubProtoClient>("JiroClient");

    //     var helloCall = await client.HelloAsync(new HelloRequest() { Message = "Hello From Client" }, headers);

    //     _logger.LogInformation(helloCall.Message);
    // }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private static JiroCloud.Api.Proto.CommandType GetCommandType(Jiro.Commands.CommandType commandType) => (int)commandType switch
    {
        0 => JiroCloud.Api.Proto.CommandType.Text,
        1 => JiroCloud.Api.Proto.CommandType.Graph,
        _ => JiroCloud.Api.Proto.CommandType.Text
    };
}
