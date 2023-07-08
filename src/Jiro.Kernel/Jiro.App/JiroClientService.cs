using Google.Protobuf;
using Grpc.Core;
using Grpc.Net.ClientFactory;
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
    private readonly IServiceProvider _serviceProvider;
    public JiroClientService(IServiceScopeFactory scopeFactory, ILogger<JiroClientService> logger, IServiceProvider serviceProvider)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        do
        {
            await using var scope = _scopeFactory.CreateAsyncScope();

            var commandHandler = scope.ServiceProvider.GetRequiredService<ICommandHandlerService>();
            var factory = scope.ServiceProvider.GetRequiredService<GrpcClientFactory>();

            var client = factory.CreateClient<JiroHubProtoClient>("JiroClient");

            TaskCompletionSource<InstanceCommandResponse> serverListenerResponse = new();

            // create connection
            using var call = client.InstanceCommand();
            _logger.LogInformation("Connected to server");

            // create listening task
            var listeningTask = Task.Run(async () =>
            {
                // await for server messages
                await foreach (var response in call.ResponseStream.ReadAllAsync())
                {
                    // notify response loop about new message
                    serverListenerResponse.SetResult(response);
                }
            });

            // create response loop
            while (!cancellationToken.IsCancellationRequested)
            {
                // await for server message
                var result = await serverListenerResponse.Task;
                serverListenerResponse = new();

                // execute command from prompt
                await using var commandScope = _scopeFactory.CreateAsyncScope();
                
                var currentUserService = commandScope.ServiceProvider.GetRequiredService<ICurrentUserService>();
                currentUserService.SetCurrentUser(result.InstanceName);

                var commandResult = await commandHandler.ExecuteCommandAsync(commandScope.ServiceProvider, result.Command);

                // todo
                // use mapper
                var commandType = GetCommandType(commandResult.CommandType);

                InstanceCommandRequest response = new()
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

            _logger.LogInformation("Disconnecting");

            await call.RequestStream.CompleteAsync();
            await listeningTask;
        } while (!cancellationToken.IsCancellationRequested);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    private static JiroCloud.Api.Proto.CommandType GetCommandType(Jiro.Commands.CommandType commandType) => (int)commandType switch
    {
        0 => JiroCloud.Api.Proto.CommandType.Text,
        1 => JiroCloud.Api.Proto.CommandType.Graph,
        _ => JiroCloud.Api.Proto.CommandType.Text
    };
}
