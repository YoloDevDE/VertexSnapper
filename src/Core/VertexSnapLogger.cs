using System;
using UnityEngine;
using ZeepSDK.Messaging;

namespace VertexSnapper.Core;

public class VertexSnapLogger
{
    private readonly string prefix = "[VERTEX]";

    public void LogStateTransition(VertexSnapMode from, VertexSnapMode to, string reason)
    {
        LogInfo($"{from} -> {to} ({reason})");
    }

    public void LogMethodEntry(string methodName, string details = "")
    {
        string message = string.IsNullOrEmpty(details) ? $"→ {methodName}" : $"→ {methodName}: {details}";
        LogDebug(message);
    }

    public void LogMethodExit(string methodName, string result = "")
    {
        string message = string.IsNullOrEmpty(result) ? $"← {methodName}" : $"← {methodName}: {result}";
        LogDebug(message);
    }

    public void LogObjectCount(string objectType, int count)
    {
        LogDebug($"{objectType} count: {count}");
    }

    public void LogVariableValue(string variableName, object value)
    {
        LogDebug($"{variableName} = {value}");
    }

    public void LogInfo(string message)
    {
        MessengerApi.Log($"{prefix} {message}");
    }

    public void LogDebug(string message)
    {
        Debug.Log($"{prefix} {message}");
    }

    public void LogWarning(string message)
    {
        Debug.LogWarning($"{prefix} WARNING: {message}");
    }

    public void LogError(string message)
    {
        Debug.LogError($"{prefix} ERROR: {message}");
    }

    public void LogError(string message, Exception ex)
    {
        Debug.LogError($"{prefix} ERROR: {message}\n{ex}");
    }
}