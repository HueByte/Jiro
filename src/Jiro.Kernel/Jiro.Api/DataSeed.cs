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
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var whitelistService = scope.ServiceProvider.GetRequiredService<IWhitelistService>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

            await SeedRolesAsync(logger, roleManager);
            await SeedManagementAsync(logger, userManager, whitelistService);
        }

        private async static Task SeedRolesAsync(ILogger logger, RoleManager<AppRole> roleManager)
        {
            string[] roles = { Roles.USER, Roles.ADMIN, Roles.SERVER };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new AppRole() { Name = role });
                    logger.LogInformation("Seeding {role}", role);
                }
            }
        }

        private async static Task SeedManagementAsync(ILogger logger, UserManager<AppUser> userManager, IWhitelistService whitelistService)
        {
            Tuple<AppUser, string>[] managementUsers = {
                new Tuple<AppUser, string>(new AppUser()
                {
                    UserName = "admin",
                    Email = "admin@heaven.org"
                }, Roles.ADMIN),
                new Tuple<AppUser, string>(new AppUser()
                {
                    UserName = "server",
                    Email = "server@heaven.org"
                }, Roles.SERVER)
            };

            foreach ((var user, var role) in managementUsers)
            {
                if (await userManager.FindByNameAsync(user.UserName!) is null)
                {
                    logger.LogInformation("Seeding {user} with role {role}", user.UserName, role);

                    var result = await userManager.CreateAsync(user, "TempPassword12");
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(user, role);
                        await whitelistService.AddUserToWhitelist(user);
                    }
                }
            }
        }
    }
}