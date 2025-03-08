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

    public Dictionary<CardGroup, List<int>> deckInfoIdListDic { get; private set; }

    public CardGroup deckCardGroup { get; private set; }

    public string playerName = "";

    private bool isTourist = false;

    private KConfig()
    {
        deckInfoIdListDic = new Dictionary<CardGroup, List<int>>();
    }

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
                    GetDeckConfig();
                }
                break;
            }
            await UniTask.Delay(1);
        }
    }

    public void UpdateDeckInfoIdList(List<int> infoIdList, CardGroup cardGroup)
    {
        KLog.I(TAG, "UpdateDeckInfoIdList");
        deckInfoIdListDic[cardGroup] = infoIdList;
        deckCardGroup = cardGroup;
    }

    public List<int> GetDeckInfoIdList(CardGroup cardGroup)
    {
        if (!deckInfoIdListDic.ContainsKey(cardGroup)) {
            return null;
        }
        return deckInfoIdListDic[cardGroup];
    }

    private async void GetDeckConfig()
    {
        string deckConfigReqStr = "{}";
        int sessionId = KRPC.Instance.CreateSession();
        KRPC.Instance.Send(sessionId, KRPC.ApiType.config_deck_get, deckConfigReqStr);
        while (true) {
            // 返回格式：{"status": "success", "deck": { "group": 0, "config": [[int数组], [int数组]]}}
            string receiveStr = KRPC.Instance.Receive(sessionId);
            if (receiveStr != null) {
                KLog.I(TAG, "GetDeckInfoIdList: Receive: " + receiveStr);
                JObject resJson = JObject.Parse(receiveStr);
                bool apiSuccess = resJson["status"]?.ToString() == KRPC.ApiRetStatus.success.ToString();
                if (!apiSuccess) {
                    KLog.E(TAG, "GetDeckInfoIdList: fail");
                    return;
                }
                JObject deckConfig = (JObject)resJson["deck"];
                deckCardGroup = (CardGroup)(int)deckConfig["group"];
                KLog.I(TAG, "GetDeckInfoIdList: deckCardGroup: " + deckCardGroup);
                JArray configArray = (JArray)deckConfig["config"];
                for (int i = 0; i < configArray.Count; i++) {
                    CardGroup group = (CardGroup)i;
                    JToken item = configArray[i];
                    List<int> infoIdList = ((JArray)item).ToObject<List<int>>();
                    deckInfoIdListDic[group] = infoIdList;
                    KLog.I(TAG, "GetDeckInfoIdList: group: " + group + ", infoIdList: " + infoIdList.Count);
                }
                break;
            }
            await UniTask.Delay(1);
        }
        KNetwork.Instance.CloseSession(sessionId);
    }
}