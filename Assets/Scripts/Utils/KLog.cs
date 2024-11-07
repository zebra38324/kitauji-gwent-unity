using UnityEngine;

// kitauji log
public class KLog
{
    public static void I(string TAG, string message)
    {
        Debug.Log("[" + TAG + "]" + " " + message);
    }

    public static void E(string TAG, string message)
    {
        Debug.LogError("[" + TAG + "]" + " " + message);
    }
}
