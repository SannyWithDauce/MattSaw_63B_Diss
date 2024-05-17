using UnityEngine;

public class GlobalExceptionHandler : MonoBehaviour
{
    void Awake()
    {
        Application.logMessageReceived += HandleLog;
    }

    void HandleLog(string logString, string stackTrace, LogType type)
    {
        if (type == LogType.Exception)
        {
            // Log or handle the exception as needed
            Debug.LogError($"Unhandled Exception: {logString}\n{stackTrace}");
        }
    }

    void OnDestroy()
    {
        Application.logMessageReceived -= HandleLog;
    }
}

