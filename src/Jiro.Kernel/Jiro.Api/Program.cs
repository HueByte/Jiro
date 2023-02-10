using Jiro.Api;
using Jiro.Api.Configurator;
using Jiro.Api.Middlewares;
using Jiro.Core.Options;
using Jiro.Core.Utils;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;

var builder = WebApplication.CreateBuilder(args);
var configRef = builder.Configuration;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .Enrich.FromLogContext()
    .CreateBootstrapLogger();

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

ModuleManager moduleManager = new(builder.Services, configRef);

if (AppUtils.IsDebug()) moduleManager.BuildDevModules();
moduleManager.LoadModuleAssemblies();
moduleManager.LoadModuleControllers();
moduleManager.RegisterModuleServices();
moduleManager.ValidateModules();

var servicesRef = builder.Services;
servicesRef.AddControllers();
servicesRef.AddEndpointsApiExplorer();
servicesRef.AddSwaggerGen();

servicesRef.AddServices();
servicesRef.RegisterCommandModules();
servicesRef.AddHttpClients(configRef);

var app = builder.Build();

_ = new AppConfigurator(app.Services)
    .ConfigureEvents();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseErrorHandler();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.UseSerilogRequestLogging();

if (AppUtils.IsDebug()) app.Map("/", () => Results.Redirect("/swagger"));

app.Run();
