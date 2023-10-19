
public enum CardGroup
{
    KumikoFirstYearS1 = 0,
    KumikoFirstYearS2,
    KumikoSecondYear,
}

public enum CardBadgeType
{
    None = 0,
    Wood,
    Brass,
    Percussion,
}

public enum CardAbility // TODO: 完善
{
    None = 0,
    Spy,
    Attack,
    Tunning,
    Bond, // 同袍之情
    ScorchWood, // 伞击：令对方实力最强的木管成员退部（仅当对方木管总吹奏实力大于10）
    Muster, // 抱团，令牌组中自己的闺蜜/基友立即上场比赛。
}

public enum CardType
{
    Normal = 0,
    Hero
}

public struct CardInfo
{
    public string imageName; // 资源名，用于在文件系统中搜索，“64.satsuki.suzuki-portrait.jpg”格式
    public string chineseName; // 中文角色名，“铃木皋月”格式
    public string englishName; // 英文角色名，“satsuki suzuki”格式
    public CardGroup group; // 牌组
    public int grade; // 年级，1-3
    public int originPower; // 初始点数
    public CardBadgeType badgeType; // 类型，位于场上哪一排
    public CardAbility ability; // 特殊能力
    public string bondType; // bond类型，用于匹配bond的组合
    public string musterType; // 抱团类型，用于匹配muster组合
    public CardType cardType; // 是否为英雄牌
    public string quote; // 卡牌最下方的台词引用
}

// 最终分数=(basePower + add) * times
// TODO 区分天气buff
public struct CardPowerBuff
{
    public int basePower;
    public int add;
    public int minus;
    public int times;
}
