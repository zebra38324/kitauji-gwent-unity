using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Cysharp.Threading.Tasks;

public class LoginSceneTouristButton : MonoBehaviour
{
    private static string TAG = "LoginSceneTouristButton";

    private bool isLogging = false;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public async void Click()
    {
        if (isLogging) {
            KLog.I(TAG, "Click: is already logging");
            return;
        }
        isLogging = true;
        KLog.I(TAG, "Click");
        bool loginRet = false;
        bool loginFinish = false;
        KConfig.Instance.Login(true, (bool ret) => {
            loginRet = ret;
            loginFinish = true;
        });
        while (!loginFinish) {
            await UniTask.Delay(1);
        }
        KLog.I(TAG, "logRet = " + loginRet);
        if (!loginRet) {
            isLogging = false;
            return;
        }
        SceneManager.LoadScene("MainMenuScene");
        isLogging = false;
    }
}
