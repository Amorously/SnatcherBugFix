using BepInEx.Core.Logging.Interpolation;
using BepInEx.Logging;

namespace SnatcherBugFix;

internal static class Logger
{
    private static readonly ManualLogSource MLS;

    static Logger()
    {
        MLS = new ManualLogSource("SnatcherBugFix");
        BepInEx.Logging.Logger.Sources.Add(MLS);
    }

    public static void Info(BepInExInfoLogInterpolatedStringHandler handler) => MLS.LogInfo(handler);
    public static void Info(string str) => MLS.LogMessage(str);
    public static void Debug(BepInExDebugLogInterpolatedStringHandler handler) => MLS.LogDebug(handler);
    public static void Debug(string str) => MLS.LogDebug(str);
    public static void Error(BepInExErrorLogInterpolatedStringHandler handler) => MLS.LogError(handler);
    public static void Error(string str) => MLS.LogError(str);
    public static void Warn(BepInExWarningLogInterpolatedStringHandler handler) => MLS.LogWarning(handler);
    public static void Warn(string str) => MLS.LogWarning(str);
}
