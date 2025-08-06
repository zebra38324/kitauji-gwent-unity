using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;

// 一场比赛的信息
[Serializable]
public class CompetitionGameInfoModel
{
    [JsonProperty]
    public string selfName { get; private set; } // 本方名称

    [JsonProperty]
    public string enemyName { get; private set; } // 对手名称

    [JsonProperty]
    public int selfScore { get; private set; } // 本方得分

    [JsonProperty]
    public int enemyScore { get; private set; } // 对手得分

    [JsonProperty]
    public bool selfWin { get; private set; } // 本方是否获胜

    [JsonProperty]
    public bool hasFinished { get; private set; } = false; // 比赛是否结束

    // 添加无参构造函数（反序列化必需）
    public CompetitionGameInfoModel() { }

    public CompetitionGameInfoModel(string selfName, string enemyName)
    {
        this.selfName = selfName;
        this.enemyName = enemyName;
    }

    public void FinishGame(int selfScore, int enemyScore, bool selfWin)
    {
        this.selfScore = selfScore;
        this.enemyScore = enemyScore;
        this.selfWin = selfWin;
        this.hasFinished = true;
    }
}
