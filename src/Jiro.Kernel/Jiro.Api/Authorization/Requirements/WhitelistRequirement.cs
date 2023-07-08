using Microsoft.AspNetCore.Authorization;

namespace Jiro.Api.Authorization.Requirements;

public class WhitelistRequirement : IAuthorizationRequirement
{
    public bool IsEnabled { get; set; }

    public WhitelistRequirement(bool isEnabled)
    {
        IsEnabled = isEnabled;
    }
}