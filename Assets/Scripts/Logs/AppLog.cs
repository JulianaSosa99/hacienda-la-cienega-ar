using UnityEngine;

public static class AppLog
{
    private const string APP_TAG = "app.hacienda.cienega";

    public static void Info(string tag, string message)
    {
        UnityEngine.Debug.Log($"{APP_TAG} [{tag}] {message}");
    }

    public static void Warning(string tag, string message)
    {
        UnityEngine.Debug.LogWarning($"{APP_TAG} [{tag}] {message}");
    }

    public static void Error(string tag, string message)
    {
        UnityEngine.Debug.LogError($"{APP_TAG} [{tag}] {message}");
    }
}