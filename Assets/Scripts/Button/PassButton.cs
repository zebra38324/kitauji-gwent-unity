using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PassButton : MonoBehaviour
{
    private static string TAG = "PassButton";

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ClickPass()
    {
        KLog.I(TAG, "ClickPass");
        PlaySceneManager.Instance.HandleMessage(SceneMsg.ClickPass);
    }
}
