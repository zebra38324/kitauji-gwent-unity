using System;
using System.Collections;
using System.Collections.Generic;

// 一场比赛的信息
[Serializable]
public class CompetitionGameInfoModel
{
    public string selfName { get; private set; } // 本方名称

    public string enemyName { get; private set; } // 对手名称

    public int selfScore { get; private set; } // 本方得分

    public int enemyScore { get; private set; } // 对手得分

    public bool selfWin { get; private set; } // 本方是否获胜

    public bool hasFinished { get; private set; } = false; // 比赛是否结束

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
