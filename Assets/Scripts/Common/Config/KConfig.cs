using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.IO;

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

    private KConfig()
    {
        deckInfoIdListDic = new Dictionary<CardGroup, List<int>>();
        deckCardGroup = CardGroup.KumikoFirstYear;
        GetDeckConfig();
        GetCompetitionContextFromDisk();
    }

    public static KConfig Instance {
        get {
            return instance;
        }
    }

    public void DeleteDiskSave()
    {
        string deckConfigPath = GetDeckConfigPath();
        if (File.Exists(deckConfigPath)) {
            File.Delete(deckConfigPath);
            KLog.I(TAG, $"Deleted deck config file at: {deckConfigPath}");
        }
        deckInfoIdListDic = new Dictionary<CardGroup, List<int>>();
        deckCardGroup = CardGroup.KumikoFirstYear;
        string competitionConfigPath = GetCompetitionConfigPath();
        if (File.Exists(competitionConfigPath)) {
            File.Delete(competitionConfigPath);
            KLog.I(TAG, $"Deleted competition config file at: {competitionConfigPath}");
        }
        competitionContextRecord = null;
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
        string configPath = GetDeckConfigPath();
        if (!File.Exists(configPath)) {
            KLog.I(TAG, $"Deck config file not found at: {configPath}");
            return;
        }
        try {
            string configStr = File.ReadAllText(configPath);
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
        } catch (Exception ex) {
            KLog.E(TAG, $"Error reading deck config file: {ex.Message}");
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
        try {
            string configPath = GetDeckConfigPath();
            File.WriteAllText(configPath, updateDeckConfigReqStr);
            KLog.I(TAG, $"Deck config saved to: {configPath}");
        } catch (Exception ex) {
            KLog.E(TAG, $"Error saving deck config: {ex.Message}");
        }
    }

    private string GetDeckConfigPath()
    {
#if UNITY_EDITOR
        return Path.Combine(Application.persistentDataPath, "deck_config_test.json");
#else
        return Path.Combine(Application.persistentDataPath, "deck_config.json");
#endif
    }

    private string GetCompetitionConfigPath()
    {
#if UNITY_EDITOR
        return Path.Combine(Application.persistentDataPath, "competition_config_test.json");
#else
        return Path.Combine(Application.persistentDataPath, "competition_config.json");
#endif
    }

    private void SaveCompetitionContextToDisk()
    {
        // 格式：{"competition_config": "json_str"}
        KLog.I(TAG, "SaveCompetitionContextToDisk");
        string updateCompetitioConfigReqStr = JsonConvert.SerializeObject(competitionContextRecord);
        KLog.I(TAG, "SaveCompetitionContextToDisk: updateCompetitioConfigReqStr = " + updateCompetitioConfigReqStr);
        try {
            string configPath = GetCompetitionConfigPath();
            File.WriteAllText(configPath, updateCompetitioConfigReqStr);
            KLog.I(TAG, $"Competition config saved to: {configPath}");
        } catch (Exception ex) {
            KLog.E(TAG, $"Error saving Competition config: {ex.Message}");
        }
    }

    private void GetCompetitionContextFromDisk()
    {
        // 格式：{"competition_config": "json_str"}
        KLog.I(TAG, "GetCompetitionContextFromDisk");
        string configPath = GetCompetitionConfigPath();
        if (!File.Exists(configPath)) {
            KLog.I(TAG, $"Deck config file not found at: {configPath}");
            return;
        }
        try {
            string configStr = File.ReadAllText(configPath);
            competitionContextRecord = JsonConvert.DeserializeObject<CompetitionBase.ContextRecord>(configStr);
        } catch (Exception ex) {
            KLog.E(TAG, $"Error reading deck config file: {ex.Message}");
        }
    }
}