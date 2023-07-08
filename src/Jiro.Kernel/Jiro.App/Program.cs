using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Jiro.App;
using JiroCloud.Api.Proto;
using Jiro.App.Configurator;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Extensions.Logging;
using Jiro.Core.Options;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
using Jiro.Commands.Base;
using Jiro.Core.Utils;
using Jiro.Infrastructure;
using Jiro.Core.Commands.GPT;
using Jiro.Commands.Models;

var host = Host.CreateDefaultBuilder(args);

host.ConfigureHostConfiguration(options =>
{

});

ConfigurationManager configManager = new();
configManager.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

EnvironmentConfigurator environmentConfigurator = new(configManager);
environmentConfigurator.PrepareDefaultFolders();
environmentConfigurator.PrepareConfigFiles();
environmentConfigurator.PrepareLogsFolder();

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
    services.AddGrpcClient<JiroHubProto.JiroHubProtoClient>("JiroClient", options =>
    {
        options.Address = new Uri("https://localhost:18092");
    })
    .AddCallCredentials((context, metadata) =>
    {
        metadata.Add("X-Api-Key", "e7+s7hpjjAUvASi7GorGLK0bm+J3fN//xq/IQRpQwVA=");

        return Task.CompletedTask;
    });

    pluginManager = new(services, configManager, logger);


    string? connString = configManager.GetValue<string>("JIRO_DB_CONN");
    if (string.IsNullOrEmpty(connString))
        connString = configManager.GetConnectionString("JiroContext");

    services.AddJiroSQLiteContext(string.IsNullOrEmpty(connString) ? Path.Join(AppContext.BaseDirectory, "save", "jiro.db") : connString);

    services.AddServices(configManager);
    services.RegisterCommands(nameof(GPTCommand.Chat));
    services.AddHttpClients(configManager);
    services.AddOptions(configManager);
    services.AddHostedService<JiroClientService>();
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