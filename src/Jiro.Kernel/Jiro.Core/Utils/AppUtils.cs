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
}