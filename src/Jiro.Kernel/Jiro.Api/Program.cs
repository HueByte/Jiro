using Jiro.Api;
using Jiro.Api.Configurator;
using Jiro.Api.Middlewares;
using Jiro.Commands.Base;
using Jiro.Core.Commands.GPT;
using Jiro.Core.Options;
using Jiro.Core.Utils;
using Jiro.Infrastructure;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;
using Serilog.Sinks.SystemConsole.Themes;

while (true)
{
    var builder = WebApplication.CreateBuilder(args);

    var configRef = builder.Configuration;
    configRef.SetBasePath(AppContext.BaseDirectory)
        .AddEnvironmentVariables();

    EnvironmentConfigurator environmentConfigurator = new(configRef);
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
        .WriteTo.Async(e => e.File(configRef.GetValue<string>("API_LOGS_PATH") ?? Path.Combine(AppContext.BaseDirectory, "logs/jiro.log"), rollingInterval: logInterval))
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
    servicesRef.AddHttpContextAccessor();

    string? connString = configRef.GetValue<string>("JIRO_DB_CONN");
    if (string.IsNullOrEmpty(connString))
        connString = configRef.GetConnectionString("JiroContext");

    servicesRef.AddJiroSQLiteContext(string.IsNullOrEmpty(connString) ? Path.Join(AppContext.BaseDirectory, "save", "jiro.db") : connString);
    servicesRef.AddServices(configRef);
    servicesRef.RegisterCommands(nameof(GPTCommand.Chat));
    servicesRef.AddHttpClients(configRef);
    servicesRef.AddOptions(configRef);
    servicesRef.AddSecurity(configRef);

    var app = builder.Build();

    // Log loaded modules
    var commandContainer = app.Services.GetRequiredService<CommandsContext>();
    foreach (var module in commandContainer.CommandModules.Keys) Serilog.Log.Information("Module {Module} loaded", module);

    var appConf = new AppConfigurator(app)
        .ConfigureEvents()
        .ConfigureCors()
        .Migrate();

    await DataSeed.SeedAsync(app);

    var instance = app.Services.GetRequiredService<ICurrentInstanceService>();
    await instance.SetCurrentInstance();

    if (app.Environment.IsDevelopment())
    {
        appConf.UseJiroSwagger();
    }

    pluginManager.RegisterAppExtensions(app);

    app.UseStaticFiles();
    app.UseErrorHandler();
    app.UseCurrentUser();
    app.UseHttpsRedirection();
    app.UseAuthorization();
    app.MapControllers();
    app.UseSerilogRequestLogging();
    app.UseCookiePolicy();
    app.MapFallbackToFile("index.html");

    if (AppUtils.IsDebug()) app.Map("/", () => Results.Redirect("/swagger"));

    app.Map("/api", () => "Hello World");

    try
    {
        await app.RunAsync(_cts.Token);

        if (_cts.IsCancellationRequested) _cts = new CancellationTokenSource();
        else break;
    }
    catch (Exception ex)
    {
        Serilog.Log.Logger.Error(ex, "An error occurred while running the application");
        break;
    }
}

public partial class Program
{
    private static CancellationTokenSource _cts = new();
    public static void Restart() => _cts.Cancel();
}