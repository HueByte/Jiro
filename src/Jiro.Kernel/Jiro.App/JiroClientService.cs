using Google.Protobuf;
using Grpc.Core;
using Grpc.Net.ClientFactory;
using Jiro.Core.Interfaces.IServices;
using JiroCloud.Api.Proto;
using Microsoft.CodeAnalysis;
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
    private readonly IServiceProvider _serviceProvider;
    public JiroClientService(IServiceScopeFactory scopeFactory, ILogger<JiroClientService> logger, IServiceProvider serviceProvider)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Initial idea for command exchange
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        //await SayHello();

        do
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();

                var commandHandler = scope.ServiceProvider.GetRequiredService<ICommandHandlerService>();
                var factory = scope.ServiceProvider.GetRequiredService<GrpcClientFactory>();

                var client = factory.CreateClient<JiroHubProtoClient>("JiroClient");

                TaskCompletionSource<ServerMessage> serverListenerResponse = new(cancellationToken);

                Grpc.Core.Metadata headers = new Grpc.Core.Metadata
                {
                    { "Content-Type", "application/grpc" }
                };

                // create connection
                using var call = client.InstanceCommand(headers);

                // create listening task
                var listeningTask = Task.Run(async () =>
                {
                    try
                    {
                        _logger.LogInformation("Connected to server");

                        // await for server messages
                        await foreach (var response in call.ResponseStream.ReadAllAsync())
                        {
                            try
                            {
                                _logger.LogInformation("Command arrived: [{command}]", response.Command);

                                // notify response loop about new message
                                serverListenerResponse.SetResult(response);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, ex.Message);
                                serverListenerResponse.SetException(ex);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, ex.Message);
                        serverListenerResponse.SetException(ex);
                    }
                });

                // create response loop
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        _logger.LogInformation("Awaiting command");
                        // await for server message
                        var result = await serverListenerResponse.Task;
                        serverListenerResponse = new();

                        // create scope for command
                        await using var commandScope = _scopeFactory.CreateAsyncScope();

                        var currentUserService = commandScope.ServiceProvider.GetRequiredService<ICurrentUserService>();
                        currentUserService.SetCurrentUser(result.InstanceName);

                        // execute command from prompt
                        var commandResult = await commandHandler.ExecuteCommandAsync(commandScope.ServiceProvider, result.Command);

                        // todo
                        // use mapper
                        var commandType = GetCommandType(commandResult.CommandType);

                        ClientMessage response = new()
                        {
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

                        await call.RequestStream.WriteAsync(response);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, ex.Message);
                        serverListenerResponse = new();
                    }
                }

                _logger.LogInformation("Disconnecting");

                await call.RequestStream.CompleteAsync();
                await listeningTask;

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
                await Task.Delay(10_000);
            }
        } while (!cancellationToken.IsCancellationRequested);
    }

    private async Task SayHello()
    {

        Grpc.Core.Metadata headers = new Grpc.Core.Metadata
        {
            { "Content-Type", "application/grpc" }
        };

        await using var scope = _scopeFactory.CreateAsyncScope();
        var factory = scope.ServiceProvider.GetRequiredService<GrpcClientFactory>();

        var client = factory.CreateClient<JiroHubProtoClient>("JiroClient");

        var helloCall = await client.HelloAsync(new HelloRequest() { Message = "Hello From Client" }, headers);

        _logger.LogInformation(helloCall.Message);
    }

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
