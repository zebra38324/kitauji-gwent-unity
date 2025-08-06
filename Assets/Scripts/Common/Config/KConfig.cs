using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
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

    public bool isTourist { get; private set; } = false;

    private static int[][] DEFAULT_DECK = new int[][] {
        new int[] {
            1002, 1003, 1004, 1007, 1008, 1009, 1013, 1051, 1052,
            1016, 1021, 1022, 1023, 1024, 1028, 1040, 1044,
            1041,
            5002, 5003, 5004,
            1080
        },
        new int[] {
            2005, 2006, 2007, 2008, 2011, 2012, 2013,
            2028, 2034, 2035,
            2042, 2047, 2048,
            5002, 5003, 5004,
            2080
        },
        new int[] {
            3001, 3006, 3007, 3021, 3040,
            3011, 3012, 3013, 3026, 3029, 3046, 3048, 3053, 3054, 3055,
            3032, 3056, 3057, 3058, 3059,
            3096, 5001, 5002, 5004,
            3095
        }
    };

    private CompetitionBase.ContextRecord competitionContextRecord = null;

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
    private class RegisterReq
    {
        public string username;
        public string password;
    }

    [Serializable]
    private class LoginReq
    {
        public bool isTourist;
        public string username;
        public string password;
    }

    /**
     * 注册
     * callback: 返回注册结果
     */
    public async void Register(Action<bool> callback, string username, string password)
    {
        RegisterReq registerReq = new RegisterReq();
        registerReq.username = username;
        registerReq.password = password;
        string registerReqStr = JsonUtility.ToJson(registerReq);
        KLog.I(TAG, "Register: registerReqStr = " + registerReqStr);
        int sessionId = KRPC.Instance.CreateSession();
        KRPC.Instance.Send(sessionId, KRPC.ApiType.register, registerReqStr);
        while (true) {
            string receiveStr = KRPC.Instance.Receive(sessionId);
            if (receiveStr != null) {
                JObject registerResJson = JObject.Parse(receiveStr);
                KLog.I(TAG, "Register: Receive: " + registerResJson);
                bool apiSuccess = registerResJson["status"]?.ToString() == KRPC.ApiRetStatus.success.ToString();
                if (callback != null) {
                    callback(apiSuccess);
                }
                break;
            }
            await UniTask.Delay(1);
        }
    }

    /**
     * 登录
     * isTourist: 以游客身份登录
     * callback: 返回登录结果
     * username: isTourist为false时设置
     * password: isTourist为false时设置
     */
    public async void Login(bool isTouristParam, Action<bool, string> callback, string username = null, string password = null)
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
                    callback(apiSuccess, loginResJson["message"]?.ToString());
                }
                if (apiSuccess) {
                    playerName = loginResJson["username"]?.ToString();
                    await GetDeckConfig();
                    if (!isTourist) {
                        await GetCompetitionContextFromServer();
                    }
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
        UpdateDeckConfig();
    }

    public List<int> GetDeckInfoIdList(CardGroup cardGroup)
    {
        if (!deckInfoIdListDic.ContainsKey(cardGroup)) {
            // 返回默认值，客户端指定
            deckInfoIdListDic[cardGroup] = new List<int>(DEFAULT_DECK[(int)cardGroup]);
        }
        return deckInfoIdListDic[cardGroup];
    }

    public void SaveCompetitionContext(CompetitionBase.ContextRecord contextRecord)
    {
        KLog.I(TAG, "SaveCompetitionContext");
        competitionContextRecord = contextRecord;
        SaveCompetitionContextToServer();
    }

    public CompetitionBase.ContextRecord GetCompetitionContext()
    {
        KLog.I(TAG, "GetCompetitionContext");
        return competitionContextRecord;
    }

    private async UniTask GetDeckConfig()
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

    [Serializable]
    private class UpdateDeckConfigReqDeck
    {
        public int group;
        public int[][] config;
    }

    [Serializable]
    private class UpdateDeckConfigReq
    {
        public UpdateDeckConfigReqDeck deck;
    }

    // 请求格式：{"deck": { "group": 0, "config": [[int数组], [int数组]]}}
    private async void UpdateDeckConfig()
    {
        if (isTourist) {
            KLog.I(TAG, "UpdateDeckConfig: isTourist");
            return;
        }
        UpdateDeckConfigReq updateDeckConfigReq = new UpdateDeckConfigReq();
        updateDeckConfigReq.deck = new UpdateDeckConfigReqDeck();
        updateDeckConfigReq.deck.group = (int)deckCardGroup;
        updateDeckConfigReq.deck.config = new int[][] {
            GetDeckInfoIdList(CardGroup.KumikoFirstYear).ToArray(),
            GetDeckInfoIdList(CardGroup.KumikoSecondYear).ToArray(),
            GetDeckInfoIdList(CardGroup.KumikoThirdYear).ToArray(),
        };
        string updateDeckConfigReqStr = JsonConvert.SerializeObject(updateDeckConfigReq);
        KLog.I(TAG, "UpdateDeckConfig: updateDeckConfigReqStr = " + updateDeckConfigReqStr);
        int sessionId = KRPC.Instance.CreateSession();
        KRPC.Instance.Send(sessionId, KRPC.ApiType.config_deck_update, updateDeckConfigReqStr);
        while (true) {
            string receiveStr = KRPC.Instance.Receive(sessionId);
            if (receiveStr != null) {
                JObject registerResJson = JObject.Parse(receiveStr);
                KLog.I(TAG, "UpdateDeckConfig: Receive: " + registerResJson);
                bool apiSuccess = registerResJson["status"]?.ToString() == KRPC.ApiRetStatus.success.ToString();
                break;
            }
            await UniTask.Delay(1);
        }
        KNetwork.Instance.CloseSession(sessionId);
    }

    [Serializable]
    private class UpdateCompetitionConfigReq
    {
        public string competition_config;
    }

    // 请求格式：{"competition_config": "json_str"}
    private async void SaveCompetitionContextToServer()
    {
        KLog.I(TAG, "SaveCompetitionContextToServer");
        if (isTourist) {
            KLog.I(TAG, "SaveCompetitionContextToServer: isTourist");
            return;
        }
        UpdateCompetitionConfigReq updateCompetitionConfigReq = new UpdateCompetitionConfigReq();
        updateCompetitionConfigReq.competition_config = JsonConvert.SerializeObject(competitionContextRecord);
        string updateCompetitioConfigReqStr = JsonConvert.SerializeObject(updateCompetitionConfigReq);
        KLog.I(TAG, "SaveCompetitionContextToServer: updateCompetitioConfigReqStr = " + updateCompetitioConfigReqStr);
        int sessionId = KRPC.Instance.CreateSession();
        KRPC.Instance.Send(sessionId, KRPC.ApiType.config_competition_update, updateCompetitioConfigReqStr);
        while (true) {
            string receiveStr = KRPC.Instance.Receive(sessionId);
            if (receiveStr != null) {
                JObject registerResJson = JObject.Parse(receiveStr);
                KLog.I(TAG, "SaveCompetitionContextToServer: Receive: " + registerResJson);
                bool apiSuccess = registerResJson["status"]?.ToString() == KRPC.ApiRetStatus.success.ToString();
                break;
            }
            await UniTask.Delay(1);
        }
        KNetwork.Instance.CloseSession(sessionId);
    }

    private async UniTask GetCompetitionContextFromServer()
    {
        KLog.I(TAG, "GetCompetitionContextFromServer");
        if (isTourist) {
            KLog.I(TAG, "GetCompetitionContextFromServer: isTourist");
            return;
        }
        int sessionId = KRPC.Instance.CreateSession();
        KRPC.Instance.Send(sessionId, KRPC.ApiType.config_competition_get, "{}");
        while (true) {
            // 返回格式：{"status": "success", "competition_config": "json_str"}
            string receiveStr = KRPC.Instance.Receive(sessionId);
            if (receiveStr != null) {
                KLog.I(TAG, "GetCompetitionContextFromServer: Receive: " + receiveStr);
                JObject resJson = JObject.Parse(receiveStr);
                bool apiSuccess = resJson["status"]?.ToString() == KRPC.ApiRetStatus.success.ToString();
                if (!apiSuccess) {
                    KLog.E(TAG, "GetCompetitionContextFromServer: fail");
                    return;
                }
                string configStr = resJson["competition_config"]?.ToString();
                competitionContextRecord = JsonConvert.DeserializeObject<CompetitionBase.ContextRecord>(configStr);
                break;
            }
            await UniTask.Delay(1);
        }
        KNetwork.Instance.CloseSession(sessionId);
    }
}