using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Security.Cryptography;
using System.Text;
using System;

public class LoginScene : MonoBehaviour
{
    private static string TAG = "LoginScene";

    public TMP_InputField account;

    public TMP_InputField password;

    public GameObject toastView;

    private bool disableSendReq = false;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnClickRegister()
    {
        KLog.I(TAG, "OnClickRegister");
        if (disableSendReq) {
            KLog.I(TAG, "OnClickRegister: disableSendReq");
            return;
        }
        if (!CheckInput()) {
            return;
        }
        StartCoroutine(SendRegisterReq());
    }

    public void OnClickLogin()
    {
        KLog.I(TAG, "OnClickLogin");
        if (disableSendReq) {
            KLog.I(TAG, "OnClickLogin: disableSendReq");
            return;
        }
        if (!CheckInput()) {
            return;
        }
        StartCoroutine(SendLoginReq(false));
    }

    public void OnClickTourist()
    {
        KLog.I(TAG, "OnClickTourist");
        if (disableSendReq) {
            KLog.I(TAG, "OnClickTourist: disableSendReq");
            return;
        }
        StartCoroutine(SendLoginReq(true));
    }

    private IEnumerator SendRegisterReq()
    {
        disableSendReq = true;

        bool registerRet = false;
        bool registerFinish = false;
        KConfig.Instance.Register((bool ret) => {
            registerRet = ret;
            registerFinish = true;
        }, account.text, HashPassword());
        while (!registerFinish) {
            yield return null;
        }
        KLog.I(TAG, "OnClickRegister: registerRet = " + registerRet);
        if (registerRet) {
            toastView.GetComponent<ToastView>().ShowToast("注册成功");
        } else {
            toastView.GetComponent<ToastView>().ShowToast("此用户名已被注册");
        }

        disableSendReq = false;
    }

    private IEnumerator SendLoginReq(bool isTourist)
    {
        disableSendReq = true;

        bool loginRet = false;
        string loginMessage = null;
        bool loginFinish = false;
        if (isTourist) {
            KConfig.Instance.Login(true, (bool ret, string message) => {
                loginRet = ret;
                loginMessage = message;
                loginFinish = true;
            });
        } else {
            KConfig.Instance.Login(false, (bool ret, string message) => {
                loginRet = ret;
                loginMessage = message;
                loginFinish = true;
            }, account.text, HashPassword());
        }
        while (!loginFinish) {
            yield return null;
        }
        KLog.I(TAG, "SendLoginReq: logRet = " + loginRet);
        if (loginRet) {
            KHeartbeat.Instance.Init();
            SceneManager.LoadScene("MainMenuScene");
            toastView.GetComponent<ToastView>().ShowToast("登录成功");
            yield break;
        } else if (loginMessage != null) {
            toastView.GetComponent<ToastView>().ShowToast(loginMessage);
        } else {
            toastView.GetComponent<ToastView>().ShowToast("登陆异常");
        }

        disableSendReq = false;
    }

    private bool CheckInput()
    {
        if (account.text.StartsWith("Tourist")) {
            KLog.W(TAG, "CheckInput: account start with Tourist");
            toastView.GetComponent<ToastView>().ShowToast("用户名不能以Tourist开头");
            return false;
        }
        if (account.text.Length < 1 || account.text.Length > 10 ||
            password.text.Length < 4 || password.text.Length > 10) {
            KLog.W(TAG, "CheckInput: len invalid, account len = " + account.text.Length + ", password len = " + password.text.Length);
            toastView.GetComponent<ToastView>().ShowToast("账号长度为1-10，密码长度为4-10");
            return false;
        }
        if (!CheckPassword()) {
            KLog.W(TAG, "CheckInput: password contains chinese");
            toastView.GetComponent<ToastView>().ShowToast("密码不能包含中文");
            return false;
        }
        return true;
    }

    private bool CheckPassword()
    {
        string pattern = @"[\u4e00-\u9fa5\u3400-\u4dbf\uf900-\ufaff\u2e80-\u2eff\u3000-\u303f\uff00-\uffef]";
        return !Regex.IsMatch(password.text, pattern);
    }

    private string HashPassword()
    {
        SHA256 sha256 = SHA256.Create();
        string combined = account.text + password.text;
        byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(combined));
        return Convert.ToBase64String(hashBytes);
    }
}
