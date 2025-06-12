using Jiro.App;
using Jiro.App.Configurator;
using Jiro.Commands.Base;
using Jiro.Commands.Models;
using Jiro.Core.Commands.Chat;
using Jiro.Core.Options;
using Jiro.Core.Utils;
using Jiro.Infrastructure;

using JiroCloud.Api.Proto;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;
using Serilog.Sinks.SystemConsole.Themes;

AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

var host = Host.CreateDefaultBuilder(args);

ConfigurationManager configManager = new();
configManager.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables();

EnvironmentConfigurator environmentConfigurator = new EnvironmentConfigurator(configManager)
    .PrepareDefaultFolders()
    .PrepareConfigFiles()
    .PrepareLogsFolder();

Serilog.Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .Enrich.FromLogContext()
    .CreateBootstrapLogger();

var logger = new SerilogLoggerProvider(Serilog.Log.Logger)
    .CreateLogger(nameof(Program));

LogOptions loggerOptions = new();
configManager.GetSection(LogOptions.Log).Bind(loggerOptions);
LogEventLevel logLevelSystem = SerilogConfigurator.GetLogEventLevel(loggerOptions.SystemLevel);
LogEventLevel logLevelAspNetCore = SerilogConfigurator.GetLogEventLevel(loggerOptions.AspNetCoreLevel);
LogEventLevel logLevelDatabase = SerilogConfigurator.GetLogEventLevel(loggerOptions.DatabaseLevel);
LogEventLevel logLevel = SerilogConfigurator.GetLogEventLevel(loggerOptions.LogLevel);
RollingInterval logInterval = SerilogConfigurator.GetRollingInterval(loggerOptions.TimeInterval);

host.UseSerilog((context, services, configuration) => configuration
        .MinimumLevel.Override("System", logLevelSystem)
        .MinimumLevel.Override("Microsoft.AspNetCore", logLevelAspNetCore)
        .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", logLevelDatabase)
        .WriteTo.Async(e => e.Console(theme: AnsiConsoleTheme.Code))
        .WriteTo.Async(e => e.File(configManager.GetValue<string>("API_LOGS_PATH") ?? Path.Combine(AppContext.BaseDirectory, "logs/jiro.log"), rollingInterval: logInterval))
        .ReadFrom.Configuration(configManager)
        .ReadFrom.Services(services));

var modulePaths = configManager.GetSection("Modules").Get<string[]>();
PluginManager? pluginManager = null;


host.ConfigureAppConfiguration(options =>
{
    options.AddConfiguration(configManager);
});

host.ConfigureServices(services =>
{
    string? apiKey = configManager.GetValue<string>("API_KEY");
    string? apiUrl = configManager.GetValue<string>("JIRO_API");

    // todo
    // add link to guide
    if (string.IsNullOrWhiteSpace(apiKey))
        throw new Exception("Please provide API_KEY");

    if (string.IsNullOrWhiteSpace(apiUrl))
        throw new Exception("Couldn't connect to API");

    services.AddGrpcClient<JiroHubProto.JiroHubProtoClient>("JiroClient", options =>
        {
            options.Address = new Uri(apiUrl);
        })
        .AddCallCredentials((context, metadata) =>
        {
            metadata.Add("X-Api-Key", apiKey);

            return Task.CompletedTask;
        })
        .ConfigureChannel(options =>
        {
            options.HttpHandler = new SocketsHttpHandler
            {
                PooledConnectionIdleTimeout = Timeout.InfiniteTimeSpan,
                KeepAlivePingDelay = TimeSpan.FromSeconds(60),
                KeepAlivePingTimeout = TimeSpan.FromSeconds(30),
                EnableMultipleHttp2Connections = true
            };
        });

    pluginManager = new(services, configManager, logger);

    string? connString = configManager.GetValue<string>("JIRO_DB_CONN");
    if (string.IsNullOrEmpty(connString))
        connString = configManager.GetConnectionString("JiroContext");

    services.AddJiroSQLiteContext(string.IsNullOrEmpty(connString) ? Path.Join(AppContext.BaseDirectory, "save", "jiro.db") : connString);
    services.AddMemoryCache();
    services.AddServices(configManager);
    services.RegisterCommands(nameof(ChatCommand.Chat));
    services.AddHttpClients(configManager);
    services.AddOptions(configManager);
    services.AddHostedService<JiroClientService>();
    services.AddHttpContextAccessor();
});

if (AppUtils.IsDebug()) pluginManager?.BuildDevModules(modulePaths);
pluginManager?.LoadModuleAssemblies();
pluginManager?.LoadModuleControllers();
pluginManager?.RegisterModuleServices();

//pluginManager.RegisterAppExtensions(host);

var app = host.Build();

// Log loaded modules
var commandContainer = app.Services.GetRequiredService<CommandsContext>();
foreach (var module in commandContainer.CommandModules.Keys) Serilog.Log.Information("Module {Module} loaded", module);

var appConf = new AppConfigurator(app)
      .Migrate();

await app.RunAsync();
