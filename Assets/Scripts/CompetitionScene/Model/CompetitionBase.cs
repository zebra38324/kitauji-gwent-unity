using System;
using System.Collections.Generic;

public class CompetitionBase
{
    // 当前比赛级别
    public enum Level
    {
        KyotoPrefecture = 0, // 京都府赛
        Kansai, // 关西赛
        National, // 全国赛
    }

    public static readonly string[] LEVEL_TEXT = {
        "京都府赛",
        "关西赛",
        "全国赛"
    };

    // 所获奖项
    public enum Prize
    {
        Bronze = 0, // 铜奖
        Silver, // 银奖
        Gold, // 金奖
        GoldPromote, // 金奖（晋级）
    }

    public static readonly string[] PRIZE_TEXT = {
        "铜奖",
        "银奖",
        "金奖",
        "金奖（晋级）"
    };

    public static Prize GetPrize(int rank, Level level)
    {
        if (rank <= 1 && level != Level.National)
            return Prize.GoldPromote; // 前两名晋级
        else if (rank <= 2)
            return Prize.Gold; // 金奖
        else if (rank <= 5)
            return Prize.Silver; // 银奖
        else
            return Prize.Bronze; // 铜奖
    }

    [Serializable]
    public class TeamInfoRecord
    {
        public string name;
        public int currentRound;
        public CompetitionGameInfoModel[] gameList;
        public Prize prize;
    }

    [Serializable]
    public class ContextRecord
    {
        public string playerName;
        public Level currnetLevel;
        public TeamInfoRecord[] teamRecords;
    }
}