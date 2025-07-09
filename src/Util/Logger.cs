using System;
using BepInEx.Logging;

namespace VertexSnapper.Util;

public static class Logger
{
    private static ManualLogSource _logSource;

    public static void Initialize(ManualLogSource logSource)
    {
        _logSource = logSource;
    }

    public static void LogDebug(string message)
    {
        _logSource?.LogDebug(message);
    }

    public static void LogInfo(string message)
    {
        _logSource?.LogInfo(message);
    }

    public static void LogWarning(string message)
    {
        _logSource?.LogWarning(message);
    }

    public static void LogError(string message)
    {
        _logSource?.LogError(message);
    }

    public static void LogError(Exception ex, string context = "")
    {
        if (string.IsNullOrEmpty(context))
        {
            _logSource?.LogError($"Exception: {ex.Message}\n{ex.StackTrace}");
        }
        else
        {
            _logSource?.LogError($"Exception in {context}: {ex.Message}\n{ex.StackTrace}");
        }
    }
}