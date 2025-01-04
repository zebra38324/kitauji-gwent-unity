using UnityEngine;

// kitauji log
public class KLog
{
    public static void I(string TAG, string message)
    {
        Debug.Log("[" + KTime.CurrentFormatMill() + "][Info][" + TAG + "]" + " " + message);
    }

    public static void W(string TAG, string message)
    {
        Debug.LogWarning("[" + KTime.CurrentFormatMill() + "][Warning][" + TAG + "]" + " " + message);
    }

    public static void E(string TAG, string message)
    {
        Debug.LogError("[" + KTime.CurrentFormatMill() + "][Error][" + TAG + "]" + " " + message);
    }
}
