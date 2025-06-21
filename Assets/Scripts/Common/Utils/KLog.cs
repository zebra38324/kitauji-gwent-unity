using System.IO;
using UnityEngine;

// kitauji log
public class KLog
{
    public static bool redirectToFile = false; // 是否重定向到文件

    public static string filePath = null;

    public static void I(string TAG, string message)
    {
        string content = "[" + KTime.CurrentFormatMill() + "][Info][" + TAG + "]" + " " + message;
        if (redirectToFile) {
            WriteToFile(content + "\n");
        }
        Debug.Log(content);
    }

    public static void W(string TAG, string message)
    {
        string content = "[" + KTime.CurrentFormatMill() + "][Warning][" + TAG + "]" + " " + message;
        if (redirectToFile) {
            WriteToFile(content + "\n");
        }
        Debug.LogWarning(content);
    }

    public static void E(string TAG, string message)
    {
        string content = "[" + KTime.CurrentFormatMill() + "][Error][" + TAG + "]" + " " + message;
        if (redirectToFile) {
            WriteToFile(content + "\n");
        }
        Debug.LogError(content);
    }

    private static void WriteToFile(string content)
    {
        if (filePath == null) {
            filePath = Path.Combine(Application.persistentDataPath, "log/log.txt");
        }
        File.AppendAllText(filePath, content + "\n");
    }
}
