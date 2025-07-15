using System.IO;
using UnityEngine;

// kitauji log
public class KLog
{
    public static bool blockLog = false; // webgl下才可以简单依赖这个，其他场景建议改用AsyncLocal

    public static bool redirectToFile = false; // 是否重定向到文件

    public static string filePath = null;

    private enum LogLevel
    {
        Info = 0,
        Warning,
        Error,
    }

    public static void I(string TAG, string message)
    {
        LogInternal(LogLevel.Info, TAG, message);
    }

    public static void W(string TAG, string message)
    {
        LogInternal(LogLevel.Warning, TAG, message);
    }

    public static void E(string TAG, string message)
    {
        LogInternal(LogLevel.Error, TAG, message);
    }

    private static void LogInternal(LogLevel level, string TAG, string message)
    {
        string content = $"[{KTime.CurrentFormatMill()}][{level.ToString()}][{TAG}] {message}";
        if (blockLog) {
            return;
        }
        if (redirectToFile) {
            TryWriteToFile(content);
        }
        switch (level) {
            case LogLevel.Info: {
                Debug.Log(content);
                break;
            }
            case LogLevel.Warning: {
                Debug.LogWarning(content);
                break;
            }
            case LogLevel.Error: {
                Debug.LogError(content);
                break;
            }
        }
    }

    private static void TryWriteToFile(string content)
    {
        if (!redirectToFile) {
            return;
        }
        if (filePath == null) {
            filePath = Path.Combine(Application.persistentDataPath, $"log/log_{KTime.CurrentMill()}.txt");
            Debug.Log($"TryWriteToFile: {filePath}");
        }
        File.AppendAllText(filePath, content + "\n");
    }
}
