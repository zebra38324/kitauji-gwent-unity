using UnityEngine;
using System;
using System.Collections.Generic;

class StatisticJsonParse
{
    
    [Serializable]
    private struct JsonCardInfo
    {
        public string imageName; // 资源名，用于在文件系统中搜索，“64.satsuki.suzuki-portrait.jpg”格式
        public string chineseName; // 中文角色名，“铃木皋月”格式
        public string englishName; // 英文角色名，“satsuki suzuki”格式
        public string group; // 牌组
        public int grade; // 年级，1-3
        public int originPower; // 初始点数
        public string badgeType; // 类型，位于场上哪一排
        public string ability; // 特殊能力
        public string bondType; // bond类型，用于匹配bond的组合
        public string musterType; // 抱团类型，用于匹配muster组合
        public string relatedCard; // 相关卡牌，bond与muster使用
        public int attackNum; // 攻击牌的攻击数值
        public string cardType; // 是否为英雄牌
        public string quote; // 卡牌最下方的台词引用
    }

    private class CardsInfo
    {
        public List<JsonCardInfo> Cards;
    }

    public static List<CardInfo> GetCardInfo(string statisticStr)
    {
        CardsInfo cardsInfo = JsonUtility.FromJson<CardsInfo>(statisticStr);
        List<CardInfo> result = new List<CardInfo>();
        foreach (JsonCardInfo jsonInfo in cardsInfo.Cards) {
            CardInfo info = new CardInfo();
            info.imageName = jsonInfo.imageName;
            info.chineseName = jsonInfo.chineseName;
            info.englishName = jsonInfo.englishName;
            info.group = (CardGroup)Enum.Parse(typeof(CardGroup), jsonInfo.group, true);
            info.grade = jsonInfo.grade;
            info.originPower = jsonInfo.originPower;
            info.badgeType = (CardBadgeType)Enum.Parse(typeof(CardBadgeType), jsonInfo.badgeType, true);
            info.ability = (CardAbility)Enum.Parse(typeof(CardAbility), jsonInfo.ability, true);
            info.bondType = jsonInfo.bondType;
            info.musterType = jsonInfo.musterType;
            info.relatedCard = jsonInfo.relatedCard;
            info.attackNum = jsonInfo.attackNum;
            info.cardType = (CardType)Enum.Parse(typeof(CardType), jsonInfo.cardType, true);
            info.quote = jsonInfo.quote;
            result.Add(info);
        }
        return result;
    }
}
