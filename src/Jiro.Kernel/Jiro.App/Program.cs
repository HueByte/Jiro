using Jiro.App;
using Jiro.App.Configurator;
using Jiro.App.Extensions;
using Jiro.Commands.Base;
using Jiro.Commands.Models;
using Jiro.Core.Commands.Chat;
using Jiro.Core.Utils;
using Jiro.Infrastructure;

using JiroCloud.Api.Proto;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Serilog;
using Serilog.Extensions.Logging;

AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

// Check for test mode
bool isTestMode = args.Contains("--test-mode") || Environment.GetEnvironmentVariable("JIRO_TEST_MODE") == "true";

var host = Host.CreateDefaultBuilder(args);

ConfigurationManager configManager = new();

// Ensure appsettings.json exists, create from example if needed
var appSettingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
var exampleSettingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.example.json");

if (!File.Exists(appSettingsPath) && File.Exists(exampleSettingsPath))
{
	File.Copy(exampleSettingsPath, appSettingsPath);
}

configManager.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
	.AddEnvironmentVariables();

EnvironmentConfigurator environmentConfigurator = new EnvironmentConfigurator(configManager)
	.PrepareDefaultFolders()
	.PrepareLogsFolder();

Log.Logger = new LoggerConfiguration()
	.ReadFrom.Configuration(configManager)
	.CreateBootstrapLogger();

var logger = new SerilogLoggerProvider(Log.Logger)
	.CreateLogger(nameof(Program));

host.UseSerilog((context, services, configuration) => configuration
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
	string? apiKey = configManager.GetValue<string>("ApiKey");
	string? apiUrl = configManager.GetValue<string>("JiroApi");

	// In test mode, use dummy values if not provided
	if (isTestMode)
	{
		apiKey ??= "test-api-key";
		apiUrl ??= "https://localhost:18092";
	}

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
		.AddInterceptor<Jiro.App.Services.GrpcExceptionInterceptor>()
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

	// Add the new communication services (WebSocket for receiving, gRPC for sending)
	services.AddJiroCommunication(configManager);

	services.AddHostedService<JiroClientService>();
	services.AddHttpContextAccessor();
});

if (AppUtils.IsDebug()) pluginManager?.BuildDevModules(modulePaths);
pluginManager?.LoadModuleAssemblies();
pluginManager?.LoadModuleControllers();
pluginManager?.RegisterModuleServices();

//pluginManager.RegisterAppExtensions(host);

var app = host.Build();

// In test mode, perform quick validation and exit
if (isTestMode)
{
	Log.Information("🧪 Running in test mode - performing startup validation");

	// Test basic service resolution
	try
	{
		var testCommandContainer = app.Services.GetRequiredService<CommandsContext>();
		Log.Information("✅ Command container resolved successfully");

		var testAppConf = new AppConfigurator(app);
		Log.Information("✅ App configurator created successfully");

		Log.Information("✅ Test mode validation completed successfully");
		Environment.Exit(0);
	}
	catch (Exception ex)
	{
		Log.Error(ex, "❌ Test mode validation failed");
		Environment.Exit(1);
	}
}

// Log loaded modules
var commandContainer = app.Services.GetRequiredService<CommandsContext>();
foreach (var module in commandContainer.CommandModules.Keys) Log.Information("Module {Module} loaded", module);

var appConf = new AppConfigurator(app)
	  .Migrate();

await app.RunAsync();
