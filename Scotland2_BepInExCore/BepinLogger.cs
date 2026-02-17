using BepInEx.Logging;
using Scotland2_Mono;

namespace Scotland2_BepInExCore;

public class BepinLogger : IScotlandLog
{
    private static ManualLogSource Log => Plugin.Log;
    
    public void Info(string message)
    {
        Log.LogInfo(message);
    }

    public void Warn(string message)
    {
        Log.LogWarning(message);
    }

    public void Error(string message)
    {
        Log.LogError(message);
    }

    public void Debug(string message)
    {
        Log.LogDebug(message);
    }
}