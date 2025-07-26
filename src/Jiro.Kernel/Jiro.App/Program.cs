using Jiro.App;
using Jiro.App.Configurator;
using Jiro.App.Extensions;
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
using Serilog.Extensions.Logging;

AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

// Ensure working directory matches the application's base directory
Directory.SetCurrentDirectory(AppContext.BaseDirectory);

// Check for test mode
bool isTestMode = args.Contains("--test-mode") || Environment.GetEnvironmentVariable("JIRO_TEST_MODE") == "true";

var host = Host.CreateDefaultBuilder(args)
	.UseContentRoot(AppContext.BaseDirectory)
	.ConfigureHostConfiguration(config =>
	{
		config.SetBasePath(AppContext.BaseDirectory);
		config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
		config.AddEnvironmentVariables();
		config.AddEnvironmentVariables("JIRO_");
		config.AddCommandLine(args);
	});

ConfigurationManager configManager = new();


configManager.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
	.AddEnvironmentVariables()
	.AddEnvironmentVariables("JIRO_");

EnvironmentConfigurator environmentConfigurator = new EnvironmentConfigurator(configManager)
	.PrepareDefaultFolders()
	.PrepareLogsFolder();

Log.Logger = new LoggerConfiguration()
	.ReadFrom.Configuration(configManager)
	.CreateBootstrapLogger();

var logger = new SerilogLoggerProvider(Log.Logger)
	.CreateLogger(nameof(Program));

host.UseSerilog((context, services, configuration) => configuration
		.ReadFrom.Configuration(context.Configuration)
		.ReadFrom.Services(services));

var modulePaths = configManager.GetSection("Modules").Get<string[]>();
PluginManager? pluginManager = null;

host.ConfigureAppConfiguration(options =>
{
	options.AddConfiguration(configManager);
});

host.ConfigureServices(services =>
{
	// Configure options first so they can be used by other services
	services.AddOptions(configManager);

	// Get application options to configure services
	var appOptions = new ApplicationOptions();
	configManager.Bind(appOptions);

	// In test mode, use dummy values if not provided
	if (isTestMode)
	{
		appOptions.ApiKey = string.IsNullOrWhiteSpace(appOptions.ApiKey) ? "test-api-key" : appOptions.ApiKey;
		appOptions.JiroApi = string.IsNullOrWhiteSpace(appOptions.JiroApi) ? "https://localhost:18092" : appOptions.JiroApi;
	}

	// Validate configuration
	if (!appOptions.IsValid())
	{
		var errors = string.Join(Environment.NewLine, appOptions.GetValidationErrors());
		throw new Exception($"Configuration validation failed:{Environment.NewLine}{errors}");
	}

	services.AddGrpcClient<JiroHubProto.JiroHubProtoClient>("JiroClient", options =>
		{
			var grpcServerUrl = configManager.GetSection("Grpc:ServerUrl").Value;
			if (string.IsNullOrEmpty(grpcServerUrl))
			{
				throw new InvalidOperationException("Grpc:ServerUrl is required in configuration");
			}
			options.Address = new Uri(grpcServerUrl);
		})
		.AddCallCredentials((context, metadata) =>
		{
			metadata.Add("X-Api-Key", appOptions.ApiKey);

			return Task.CompletedTask;
		})
		.AddInterceptor<Jiro.App.Services.GrpcExceptionInterceptor>()
		.AddInterceptor<Jiro.App.Interceptors.InstanceContextInterceptor>()
		.ConfigureChannel(options =>
		{
			var socketsHandler = new SocketsHttpHandler
			{
				PooledConnectionIdleTimeout = Timeout.InfiniteTimeSpan,
				KeepAlivePingDelay = TimeSpan.FromSeconds(60),
				KeepAlivePingTimeout = TimeSpan.FromSeconds(30),
				EnableMultipleHttp2Connections = true
			};
			
			// For localhost development, skip TLS certificate validation
			var grpcServerUrl = configManager.GetSection("Grpc:ServerUrl").Value;
			if (!string.IsNullOrEmpty(grpcServerUrl))
			{
				var uri = new Uri(grpcServerUrl);
				if (uri.Host == "localhost" || uri.Host == "127.0.0.1")
				{
					socketsHandler.SslOptions.RemoteCertificateValidationCallback = 
						(sender, certificate, chain, sslPolicyErrors) => true;
				}
			}
			
			options.HttpHandler = socketsHandler;
		});

	pluginManager = new(services, configManager, logger);

	// Get data paths from options and configure database
	var dataPathsOptions = new DataPathsOptions();
	configManager.GetSection(DataPathsOptions.DataPaths).Bind(dataPathsOptions);

	var connString = configManager.GetConnectionString("JiroContext");
	var dbPath = string.IsNullOrEmpty(connString) ? dataPathsOptions.AbsoluteDatabasePath : connString;
	services.AddJiroSQLiteContext(dbPath);
	services.AddMemoryCache();
	services.AddServices(configManager);
	services.RegisterCommands(nameof(ChatCommand.Chat));
	services.AddHttpClients(configManager);

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

// Initialize instance ID from API
using (var scope = app.Services.CreateScope())
{
	var instanceMetadataAccessor = scope.ServiceProvider.GetRequiredService<Jiro.Core.Services.Context.IInstanceMetadataAccessor>();
	var appOptions = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<Jiro.Core.Options.ApplicationOptions>>().Value;
	
	Log.Information("Initializing instance ID from Jiro API...");
	var instanceId = await instanceMetadataAccessor.InitializeInstanceIdAsync(appOptions.ApiKey);
	
	if (!string.IsNullOrWhiteSpace(instanceId))
	{
		Log.Information("✅ Instance ID initialized successfully: {InstanceId}", instanceId);
	}
	else
	{
		Log.Fatal("❌ Failed to initialize instance ID from API - application cannot start without valid instance metadata");
		Environment.Exit(1);
	}
}

var appConf = new AppConfigurator(app)
	  .Migrate();

await app.RunAsync();
