
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
    Tunning
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
    public string englishName; // 英文角色名，“suzuki satsuki”格式
    public CardGroup group; // 牌组
    public int grade; // 年级，1-3
    public int originPower; // 初始点数
    public CardBadgeType badgeType; // 类型，位于场上哪一排
    public CardAbility ability; // 特殊能力
    public CardType cardType; // 是否为英雄牌
    public string quote; // 卡牌最下方的台词引用
}
