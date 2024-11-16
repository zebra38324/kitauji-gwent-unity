using UnityEngine;

// kitauji log
public class KLog
{
    public static void I(string TAG, string message)
    {
        Debug.Log("[Info][" + TAG + "]" + " " + message);
    }

    public static void W(string TAG, string message)
    {
        Debug.LogWarning("[Warning][" + TAG + "]" + " " + message);
    }

    public static void E(string TAG, string message)
    {
        Debug.LogError("[Error][" + TAG + "]" + " " + message);
    }
}
