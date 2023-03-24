using Jiro.Core.Constants;
using Jiro.Core.Models;
using Jiro.Infrastructure;
using Microsoft.AspNetCore.Identity;

namespace Jiro.Api
{
    public static class DataSeed
    {
        public static async Task SeedAsync(WebApplication app)
        {
            await using var scope = app.Services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<JiroContext>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<AppRole>>();

            string[] roles = { Roles.USER, Roles.ADMIN, Roles.SERVER };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new AppRole() { Name = role });
            }
        }
    }
}