using Jiro.Api;
using Jiro.Api.Configurator;
using Jiro.Core.Utils;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .CreateBootstrapLogger();

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services));

ModuleManager moduleManager = new(builder.Services, builder.Configuration);

if (AppUtils.IsDebug()) moduleManager.BuildDevModules();
moduleManager.LoadModuleAssemblies();
moduleManager.LoadModuleControllers();
moduleManager.RegisterModuleServices();
moduleManager.ValidateModules();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddServices();
builder.Services.RegisterCommandModules();
builder.Services.AddHttpClients(builder.Configuration);

var app = builder.Build();

AppConfigurator appConfigurator = new(app.Services);
appConfigurator.ConfigureEvents();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

if (AppUtils.IsDebug()) app.Map("/", () => Results.Redirect("/swagger"));

app.Run();
