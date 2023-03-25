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
            var IJiroInstanceService = scope.ServiceProvider.GetRequiredService<IJiroInstanceService>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

            await SeedMainInstanceAsync(logger, IJiroInstanceService);
            await SeedRolesAsync(logger, roleManager);
            await SeedManagementAsync(logger, userManager, whitelistService);
        }

        private async static Task SeedMainInstanceAsync(ILogger logger, IJiroInstanceService jiroInstanceService)
        {
            if (await jiroInstanceService.GetJiroInstanceAsync() is not null)
            {
                return;
            }

            logger.LogInformation("Seeding main Jiro instance...");

            var jiroInstance = new JiroInstance()
            {
                InstanceName = "Default Jiro Instance",
                IsActive = true,
                IsConfigured = false
            };

            await jiroInstanceService.CreateJiroInstanceAsync(jiroInstance);
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
            Tuple<AppUser, string[]>[] managementUsers = {
                new Tuple<AppUser, string[]>(new AppUser()
                {
                    UserName = "admin",
                    Email = "admin@heaven.org"
                }, new string[] { Roles.ADMIN }),
                new Tuple<AppUser, string[]>(new AppUser()
                {
                    UserName = "server",
                    Email = "server@heaven.org"
                }, new string[] { Roles.SERVER, Roles.ADMIN })
            };

            foreach ((var user, var roles) in managementUsers)
            {
                if (await userManager.FindByNameAsync(user.UserName!) is null)
                {
                    logger.LogInformation("Seeding {user} with roles {role}", user.UserName, string.Join(';', roles));

                    var result = await userManager.CreateAsync(user, "TempPassword12");
                    if (result.Succeeded)
                    {
                        foreach (var role in roles)
                            await userManager.AddToRoleAsync(user, role);
                        await whitelistService.AddUserToWhitelist(user);
                    }
                }
            }
        }
    }
}