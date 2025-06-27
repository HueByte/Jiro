using Jiro.App;
using Jiro.App.Configurator;
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

var host = Host.CreateDefaultBuilder(args);

ConfigurationManager configManager = new();
configManager.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
	.AddEnvironmentVariables();

EnvironmentConfigurator environmentConfigurator = new EnvironmentConfigurator(configManager)
	.PrepareDefaultFolders()
	.PrepareConfigFiles()
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
foreach (var module in commandContainer.CommandModules.Keys) Log.Information("Module {Module} loaded", module);

var appConf = new AppConfigurator(app)
	  .Migrate();

await app.RunAsync();
