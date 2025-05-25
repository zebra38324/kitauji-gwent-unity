using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using LanguageExt;
using static LanguageExt.Prelude;

/**
 * 记录游戏中的各种信息
 */
public record PlayTracker
{
    private string TAG = "PlayTracker";

    public record PlayerInfo
    {
        public string name { get; init; }
        public CardGroup cardGroup { get; init; } = CardGroup.KumikoFirstYear;
        public int setScore { get; init; } = 0; // 局分

        public static readonly Lens<PlayerInfo, CardGroup> Lens_CardGroup = Lens<PlayerInfo, CardGroup>.New(
            p => p.cardGroup,
            cardGroup => p => p with { cardGroup = cardGroup }
        );

        public static readonly Lens<PlayerInfo, int> Lens_SetScore = Lens<PlayerInfo, int>.New(
            p => p.setScore,
            setScore => p => p with { setScore = setScore }
        );

        public PlayerInfo(string name,
            CardGroup cardGroup = CardGroup.KumikoFirstYear,
            int setScore = 0)
        {
            this.name = name;
            this.cardGroup = cardGroup;
            this.setScore = setScore;
        }
    }

    public static readonly Lens<PlayTracker, PlayerInfo> Lens_SelfPlayerInfo = Lens<PlayTracker, PlayerInfo>.New(
        p => p.selfPlayerInfo,
        selfPlayerInfo => p => p with { selfPlayerInfo = selfPlayerInfo }
    );

    public static readonly Lens<PlayTracker, PlayerInfo> Lens_EnemyPlayerInfo = Lens<PlayTracker, PlayerInfo>.New(
        p => p.enemyPlayerInfo,
        enemyPlayerInfo => p => p with { enemyPlayerInfo = enemyPlayerInfo }
    );

    public static readonly Lens<PlayTracker, int> Lens_SelfPlayerInfo_SetScore = lens(Lens_SelfPlayerInfo, PlayerInfo.Lens_SetScore);

    public static readonly Lens<PlayTracker, int> Lens_EnemyPlayerInfo_SetScore = lens(Lens_EnemyPlayerInfo, PlayerInfo.Lens_SetScore);

    public static readonly Lens<PlayTracker, CardGroup> Lens_EnemyPlayerInfo_CardGroup = lens(Lens_EnemyPlayerInfo, PlayerInfo.Lens_CardGroup);

    // 记录一局的先后手、结果等
    public record SetRecord
    {
        public bool selfFirst { get; init; } // self先手
        public int selfScore { get; init; }
        public int enemyScore { get; init; }
        public int result { get; init; } // 每局结果，-1：self负，0：平局，1：self胜

        public SetRecord(bool selfFirst,
            int selfScore = 0,
            int enemyScore = 0,
            int result = 0)
        {
            this.selfFirst = selfFirst;
            this.selfScore = selfScore;
            this.enemyScore = enemyScore;
            this.result = result;
        }
    }

    // 玩家信息
    public PlayerInfo selfPlayerInfo { get; init; }

    public PlayerInfo enemyPlayerInfo { get; init; }

    // 游戏比分信息等
    public static int SET_NUM = 3; // 每场比赛最多三局

    public ImmutableList<SetRecord> setRecordList { get; init; } = ImmutableList<SetRecord>.Empty; // 每局比赛的结果记录

    public int curSet { get; init; } = 0;// 当前是第几局

    // 开局先手判断
    public bool isHost { get; init; } = false;

    public bool isGameFinish { get; init; } = false;

    public bool isSelfWinner { get; init; } = false;

    private static int HOST_FIRST_RANDOM_MAX = 100;

    private static int HOST_FIRST_RANDOM_MIN = 0;

    private static int HOST_FIRST_RANDOM_THRESHOLD = 50;

    public PlayTracker(bool isHost_,
        string selfName,
        string enemyName,
        CardGroup selfGroup) // enemy group开局后由battle model获取
    {
        if (TAG == "PlayTracker") {
            TAG += isHost_ ? "-Host" : "-Player";
        }
        isHost = isHost_;
        selfPlayerInfo = new PlayerInfo(selfName, selfGroup);
        enemyPlayerInfo = new PlayerInfo(enemyName);
    }

    // 设置第一局信息
    public PlayTracker ConfigFirstSet(bool selfFirst)
    {
        var newRecord = this;
        if (curSet > 0) {
            KLog.E(TAG, "StartGame: curSet invalid: " + curSet);
            return newRecord;
        }
        newRecord = newRecord with {
            setRecordList = newRecord.setRecordList.Add(new SetRecord(selfFirst))
        };
        return newRecord;
    }

    // 记录单局游戏的结束信息
    public PlayTracker SetFinish(int selfScore, int enemyScore)
    {
        var newRecord = this;
        KLog.I(TAG, "SetFinish: curSet = " + curSet + ", selfScore = " + selfScore + ", enemyScore = " + enemyScore);
        
        // 局分：赢一小局加一分，平局双方各加一分
        int result = 0;
        if (selfScore > enemyScore) {
            result = 1;
        } else if (selfScore < enemyScore) {
            result = -1;
        } else {
            // 久二年牌组，平局算赢
            if (newRecord.selfPlayerInfo.cardGroup == CardGroup.KumikoSecondYear && newRecord.enemyPlayerInfo.cardGroup != CardGroup.KumikoSecondYear) {
                result = 1;
            } else if (newRecord.selfPlayerInfo.cardGroup != CardGroup.KumikoSecondYear && newRecord.enemyPlayerInfo.cardGroup == CardGroup.KumikoSecondYear) {
                result = -1;
            } else {
                // 两边都是久二年，还是平局
                result = 0;
            }
        }
        var newSetRecordList = newRecord.setRecordList;
        var updatedSetRecord = newRecord.setRecordList[newRecord.curSet] with {
            selfScore = selfScore,
            enemyScore = enemyScore,
            result = result
        };
        newRecord = newRecord with {
            setRecordList = newSetRecordList.SetItem(newRecord.curSet, updatedSetRecord)
        };
        if (result >= 0) {
            newRecord = Lens_SelfPlayerInfo_SetScore.Set(newRecord.selfPlayerInfo.setScore + 1, newRecord);
        }
        if (result <= 0) {
            newRecord = Lens_EnemyPlayerInfo_SetScore.Set(newRecord.enemyPlayerInfo.setScore + 1, newRecord);
        }

        newRecord = newRecord.TryFinishGame();
        if (newRecord.isGameFinish) {
            return newRecord;
        }
        // 更新新一局的信息
        int lastSet = newRecord.curSet;
        bool nextSelfFirst = false;
        if (newRecord.setRecordList[lastSet].result == 1) {
            nextSelfFirst = true;
        } else if (newRecord.setRecordList[lastSet].result == -1) {
            nextSelfFirst = false;
        } else {
            nextSelfFirst = !newRecord.setRecordList[lastSet].selfFirst;
        }
        var newSetRecord = new SetRecord(nextSelfFirst);
        KLog.I(TAG, "SetFinish: next set self first = " + nextSelfFirst);
        newRecord = newRecord with {
            curSet = lastSet + 1,
            setRecordList = newRecord.setRecordList.Add(newSetRecord)
        };
        return newRecord;
    }

    // 返回带颜色的玩家名，用于文本ui显示
    public string GetNameText(bool isSelf)
    {
        string name = isSelf ? selfPlayerInfo.name : enemyPlayerInfo.name;
        string color = isSelf ? "green" : "red";
        return string.Format("<color={0}>{1}</color>", color, name);
    }

    private PlayTracker TryFinishGame()
    {
        var newRecord = this;
        if (!newRecord.IsGameFinish()) {
            return newRecord;
        }
        newRecord = newRecord with {
            isGameFinish = true,
            isSelfWinner = newRecord.IsSelfWinner(),
        };
        return newRecord;
    }

    private bool IsGameFinish()
    {
        int selfCount = selfPlayerInfo.setScore;
        int enemyCount = enemyPlayerInfo.setScore;
        bool isGameFinish = false;
        if (selfCount != enemyCount) {
            // 双方局分不同，有达到2及以上的，游戏结束
            isGameFinish = selfCount >= 2 || enemyCount >= 2;
        } else {
            // 双方局分相同，打完三局后结束
            isGameFinish = curSet == 2;
        }
        KLog.I(TAG, "IsGameFinish: ret = " + isGameFinish + ", selfCount = " + selfCount + ", enemyCount = " + enemyCount);
        return isGameFinish;
    }

    private bool IsSelfWinner()
    {
        int selfCount = selfPlayerInfo.setScore;
        int enemyCount = enemyPlayerInfo.setScore;
        int selfFirstCount = 0;
        bool isSelfWinner = false;
        for (int i = 0; i <= curSet; i++) {
            if (setRecordList[i].selfFirst) {
                selfFirstCount += 1;
            }
        }
        if (selfCount != enemyCount) {
            // 双方局分不同，局分高的胜利
            isSelfWinner = selfCount > enemyCount;
        } else {
            // 三局后双方局分相同，先手局数多的一方获胜（先手劣势）
            isSelfWinner = selfFirstCount >= 2;
        }
        KLog.I(TAG, "IsSelfWinner: " + isSelfWinner);
        return isSelfWinner;
    }

    // 返回带颜色的技能名，用于文本ui显示
    public static string GetAbilityNameText(CardAbility cardAbility)
    {
        return string.Format("<b>{0}</b>", CardText.cardAbilityText[(int)cardAbility].Split("：")[0]);
    }

    // 返回带颜色的卡牌名，用于文本ui显示
    public static string GetCardNameText(CardModel card)
    {
        return string.Format("<b>{0}</b>", card.cardInfo.chineseName);
    }

    // 开局本方为host，计算是否先手
    public static bool IsHostSelfFirst()
    {
        System.Random ran = new System.Random();
        int randomNum = ran.Next(HOST_FIRST_RANDOM_MIN, HOST_FIRST_RANDOM_MAX); // 范围0-99，大于等于50为host先手
        return randomNum >= HOST_FIRST_RANDOM_THRESHOLD;
    }
}
