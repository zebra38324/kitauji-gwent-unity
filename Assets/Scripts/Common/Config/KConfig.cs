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

    public string playerName = "北宇治高中";

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

    private static string DECK_CONFIG_KEY = "deck_config";

    private static string COMPETITION_CONFIG_KEY = "competition_config";

    private KConfig()
    {
        deckInfoIdListDic = new Dictionary<CardGroup, List<int>>();
        GetDeckConfig();
        GetCompetitionContextFromDisk();
    }

    public static KConfig Instance {
        get {
            return instance;
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
        SaveCompetitionContextToDisk();
    }

    public CompetitionBase.ContextRecord GetCompetitionContext()
    {
        KLog.I(TAG, "GetCompetitionContext");
        return competitionContextRecord;
    }

    private void GetDeckConfig()
    {
        // 格式：{"deck_config": { "group": 0, "config": [[int数组], [int数组]]}}
        if (!PlayerPrefs.HasKey(DECK_CONFIG_KEY)) {
            return;
        }
        string configStr = PlayerPrefs.GetString(DECK_CONFIG_KEY);
        KLog.I(TAG, "GetDeckConfig: Receive: " + configStr);
        JObject resJson = JObject.Parse(configStr);
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

    private void UpdateDeckConfig()
    {
        // 格式：{"deck_config": { "group": 0, "config": [[int数组], [int数组]]}}
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
        PlayerPrefs.SetString(DECK_CONFIG_KEY, updateDeckConfigReqStr);
        PlayerPrefs.Save();
    }

    [Serializable]
    private class UpdateCompetitionConfigReq
    {
        public string competition_config;
    }

    private void SaveCompetitionContextToDisk()
    {
        // 格式：{"competition_config": "json_str"}
        KLog.I(TAG, "SaveCompetitionContextToDisk");
        UpdateCompetitionConfigReq updateCompetitionConfigReq = new UpdateCompetitionConfigReq();
        updateCompetitionConfigReq.competition_config = JsonConvert.SerializeObject(competitionContextRecord);
        string updateCompetitioConfigReqStr = JsonConvert.SerializeObject(updateCompetitionConfigReq);
        KLog.I(TAG, "SaveCompetitionContextToDisk: updateCompetitioConfigReqStr = " + updateCompetitioConfigReqStr);
        PlayerPrefs.SetString(COMPETITION_CONFIG_KEY, updateCompetitioConfigReqStr);
        PlayerPrefs.Save();
    }

    private void GetCompetitionContextFromDisk()
    {
        // 格式：{"competition_config": "json_str"}
        KLog.I(TAG, "GetCompetitionContextFromDisk");
        if (!PlayerPrefs.HasKey(COMPETITION_CONFIG_KEY)) {
            return;
        }
        string configStr = PlayerPrefs.GetString(COMPETITION_CONFIG_KEY);
        competitionContextRecord = JsonConvert.DeserializeObject<CompetitionBase.ContextRecord>(configStr);
    }
}