using Jiro.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Jiro.Api.Configurator
{
    public class AppConfigurator
    {
        private readonly WebApplication _app;
        private readonly EventsConfigurator _eventsConfigurator;
        public AppConfigurator(WebApplication app)
        {
            _app = app;
            _eventsConfigurator = app.Services.GetRequiredService<EventsConfigurator>();
        }
        public AppConfigurator ConfigureSPA()
        {
            var cacheMaxAgeOneWeek = (60 * 60 * 24 * 7).ToString();
            _app.UseStaticFiles(new StaticFileOptions
            {
                OnPrepareResponse = ctx =>
                {
                    ctx.Context.Response.Headers.Append(
                         "Cache-Control", $"public, max-age={cacheMaxAgeOneWeek}");
                }
            });

            return this;
        }

        public AppConfigurator ConfigureEvents()
        {
            _eventsConfigurator?.ConfigureLoggingEvents();

            return this;
        }

        public AppConfigurator ConfigureCors()
        {
            // TODO:
            // temporary, will be replaced with a more secure solution once auth system is finished
            _app.UseCors(builder =>
            {
                builder.AllowAnyOrigin();
                builder.AllowAnyHeader();
                builder.AllowAnyMethod();
            });

            return this;
        }

        public AppConfigurator UseJiroSwagger()
        {
            _app.UseSwagger();
            _app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Jiro API V1");
                c.InjectStylesheet("/swagger-ui/SwaggerDark.css");
            });

            return this;
        }

        public AppConfigurator Migrate()
        {
            if (!Directory.Exists(Path.Join(AppContext.BaseDirectory, "save")))
                Directory.CreateDirectory(Path.Join(AppContext.BaseDirectory, "save"));

            using var scope = _app.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<JiroContext>();

            context.Database.Migrate();

            return this;
        }
    }
}