using Jiro.Api;
using Jiro.Api.Configurator;
using Jiro.Core.Utils;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Map("/", () => Results.Redirect("/swagger"));

app.Run();
