using OpenAI;

namespace Jiro.Core.Utils;

public static class AppUtils
{
    public static bool IsDebug()
    {
#if DEBUG
        return true;
#else
        return false;
#endif
    }

    public static Role GetRole(string role) => role switch
    {
        "User" => Role.User,
        "System" => Role.System,
        "AI" => Role.Assistant,
        _ => throw new ArgumentException($"Invalid value {role}", nameof(role))
    };
}
