using System.Security.Claims;
using Jiro.Api.Authorization.Requirements;
using Microsoft.AspNetCore.Authorization;

namespace Jiro.Api.Authorization;

public class WhitelistAuthorizationHandler : AuthorizationHandler<WhitelistRequirement>, IAuthorizationRequirement
{
    private readonly ILogger _logger;
    private readonly IWhitelistService _whitelistService;
    public WhitelistAuthorizationHandler(IWhitelistService whitelistService, ILogger<WhitelistAuthorizationHandler> logger)
    {
        _whitelistService = whitelistService;
        _logger = logger;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, WhitelistRequirement requirement)
    {
        if (!requirement.IsEnabled)
        {
            context.Succeed(requirement);
            return;
        }

        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        // todo implement caching for whitelist
        var isWhitelisted = await _whitelistService.IsWhitelistedAsync(userId);

        if (isWhitelisted)
        {
            context.Succeed(requirement);
        }
        else
        {
            var username = context.User.FindFirstValue(ClaimTypes.Name)!;
            _logger.LogWarning("User {userame} is not whitelisted", username);
            context.Fail();
        }
    }
}