using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Cysharp.Threading.Tasks;


// kitauji config
// 全局配置读取
public class KConfig
{
    private string TAG = "KConfig";
    private static readonly KConfig instance = new KConfig();

    public List<int> deckInfoIdList { get; private set; }

    public CardGroup deckCardGroup { get; private set; }

    public string playerName = "";

    private bool isTourist = false;

    private static string[] touristNameList = { "黄前久美子", "高坂丽奈" };

    static KConfig() { }

    private KConfig() { }

    public static KConfig Instance {
        get {
            return instance;
        }
    }

    [Serializable]
    private class LoginReq
    {
        public bool isTourist;
        public string username;
        public string password;
    }

    /**
     * 登录
     * isTourist: 以游客身份登录
     * callback: 返回登录结果
     * username: isTourist为false时设置
     * password: isTourist为false时设置
     */
    public async void Login(bool isTouristParam, Action<bool> callback, string username = null, string password = null)
    {
        isTourist = isTouristParam;
        LoginReq loginReq = new LoginReq();
        loginReq.isTourist = isTourist;
        loginReq.username = username;
        loginReq.password = password;
        string loginReqStr = JsonUtility.ToJson(loginReq);
        KLog.I(TAG, "Login: loginReqStr = " + loginReqStr);
        int sessionId = KRPC.Instance.CreateSession();
        KRPC.Instance.Send(sessionId, KRPC.ApiType.auth_login, loginReqStr);
        while (true) {
            string receiveStr = KRPC.Instance.Receive(sessionId);
            if (receiveStr != null) {
                JObject loginResJson = JObject.Parse(receiveStr);
                KLog.I(TAG, "Login: Receive: " + receiveStr);
                bool apiSuccess = loginResJson["status"]?.ToString() == KRPC.ApiRetStatus.success.ToString();
                if (callback != null) {
                    callback(apiSuccess);
                }
                if (apiSuccess) {
                    playerName = loginResJson["username"]?.ToString();
                    GetDeckInfoIdList();
                }
                break;
            }
            await UniTask.Delay(1);
        }
    }

    public void UpdateDeckInfoIdList(List<int> infoIdList, CardGroup cardGroup)
    {
        KLog.I(TAG, "UpdateDeckInfoIdList");
        deckInfoIdList = infoIdList;
        deckCardGroup = cardGroup;
    }

    private async void GetDeckInfoIdList()
    {
        string deckConfigReqStr = "{}";
        int sessionId = KRPC.Instance.CreateSession();
        KRPC.Instance.Send(sessionId, KRPC.ApiType.config_deck_get, deckConfigReqStr);
        while (true) {
            string receiveStr = KRPC.Instance.Receive(sessionId);
            if (receiveStr != null) {
                KLog.I(TAG, "GetDeckInfoIdList: Receive: " + receiveStr);
                JObject deckConfigJson = JObject.Parse(receiveStr);
                bool apiSuccess = deckConfigJson["status"]?.ToString() == KRPC.ApiRetStatus.success.ToString();
                deckInfoIdList = ((JArray)deckConfigJson["deck"])?.ToObject<List<int>>();
                JudgeDeckGroup();
                KLog.I(TAG, "GetDeckInfoIdList: deckInfoIdList: " + deckInfoIdList.Count);
                break;
            }
            await UniTask.Delay(1);
        }
        KNetwork.Instance.CloseSession(sessionId);
    }

    private void JudgeDeckGroup()
    {
        deckCardGroup = CardGroup.KumikoSecondYear;
    }
}