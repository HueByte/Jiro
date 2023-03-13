using Jiro.Api.Configurator;
using Jiro.Api.Middlewares;
using Jiro.Commands.Base;
using Jiro.Core.Commands.GPT;
using Jiro.Core.Options;
using Jiro.Core.Utils;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;
using Serilog.Sinks.SystemConsole.Themes;

var builder = WebApplication.CreateBuilder(args);
var configRef = builder.Configuration;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .Enrich.FromLogContext()
    .CreateBootstrapLogger();

var logger = new SerilogLoggerProvider(Log.Logger)
    .CreateLogger(nameof(Program));

LogOptions loggerOptions = new();
configRef.GetSection(LogOptions.Log).Bind(loggerOptions);
LogEventLevel logLevelSystem = SerilogConfigurator.GetLogEventLevel(loggerOptions.SystemLevel);
LogEventLevel logLevelAspNetCore = SerilogConfigurator.GetLogEventLevel(loggerOptions.AspNetCoreLevel);
LogEventLevel logLevelDatabase = SerilogConfigurator.GetLogEventLevel(loggerOptions.DatabaseLevel);
LogEventLevel logLevel = SerilogConfigurator.GetLogEventLevel(loggerOptions.LogLevel);
RollingInterval logInterval = SerilogConfigurator.GetRollingInterval(loggerOptions.TimeInterval);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .MinimumLevel.Override("System", logLevelSystem)
    .MinimumLevel.Override("Microsoft.AspNetCore", logLevelAspNetCore)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", logLevelDatabase)
    .WriteTo.Async(e => e.Console(theme: AnsiConsoleTheme.Code))
    .WriteTo.Async(e => e.File(Path.Combine(AppContext.BaseDirectory, "logs/jiro.log"), rollingInterval: logInterval))
    .ReadFrom.Configuration(builder.Configuration)
    .ReadFrom.Services(services));

var modulePaths = configRef.GetSection("Modules").Get<string[]>();
PluginManager pluginManager = new(builder.Services, configRef, logger);

if (AppUtils.IsDebug()) pluginManager.BuildDevModules(modulePaths);
pluginManager.LoadModuleAssemblies();
pluginManager.LoadModuleControllers();
pluginManager.RegisterModuleServices();

var servicesRef = builder.Services;
servicesRef.AddControllers();
servicesRef.AddEndpointsApiExplorer();
servicesRef.AddSwaggerGen();

servicesRef.AddServices(configRef);
servicesRef.RegisterCommands(nameof(GPTCommand.Chat));
servicesRef.AddHttpClients(configRef);
servicesRef.AddOptions(configRef);

var app = builder.Build();

// Log loaded modules
var commandContainer = app.Services.GetRequiredService<CommandsContext>();
foreach (var module in commandContainer.CommandModules.Keys) Log.Information("Module {Module} loaded", module);

var appConf = new AppConfigurator(app)
    .ConfigureEvents()
    .ConfigureCors();

if (app.Environment.IsDevelopment())
{
    appConf.UseJiroSwagger();
}

pluginManager.RegisterAppExtensions(app);

app.UseStaticFiles();
app.UseErrorHandler();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.UseSerilogRequestLogging();

if (AppUtils.IsDebug()) app.Map("/", () => Results.Redirect("/swagger"));

app.Run();
