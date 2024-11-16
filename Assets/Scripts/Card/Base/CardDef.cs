
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
    Spy, // 间谍: 加入对面吹奏部，让己方新增两名部员。
    Attack, // 投掷: 投掷号嘴（或洗手液等），使指定对方一名部员吹奏能力降低。
    Tunning, // 调音: 消除己方除天气外的所有不良影响。
    Bond, // 同袍之情: 当CP（或组合）同时登场时，吹奏能力翻倍。
    ScorchWood, // 伞击：令对方实力最强的木管成员退部（仅当对方木管总吹奏实力大于10）
    Muster, // 抱团，令牌组中自己的闺蜜/基友立即上场比赛。
    Morale, // 士气: 高喊“北宇治Fight！”，使同一行内除自己以外的部员吹奏能力+1。
    Medic, // 复活: 令一名退部/毕业的部员（天王除外）回归吹奏部，并立即加入演奏。
    Horn, // 支援: 使同一行内除自己之外的部员吹奏实力翻倍。
}

public enum CardType
{
    Normal = 0,
    Hero
}

public struct CardInfo
{
    public int id; // 卡牌全局唯一id，存在多张相同卡牌时，以其id作为区分
    public int infoId; // 卡牌信息id
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
    public string relatedCard; // 相关卡牌，bond与muster使用
    public int attackNum; // 攻击牌的攻击数值
    public CardType cardType; // 是否为英雄牌
    public string quote; // 卡牌最下方的台词引用

    public CardInfo(CardInfo other)
    {
        id = other.id;
        infoId = other.infoId;
        imageName = other.imageName;
        chineseName = other.chineseName;
        englishName = other.englishName;
        group = other.group;
        grade = other.grade;
        originPower = other.originPower;
        badgeType = other.badgeType;
        ability = other.ability;
        bondType = other.bondType;
        musterType = other.musterType;
        relatedCard = other.relatedCard;
        attackNum = other.attackNum;
        cardType = other.cardType;
        quote = other.quote;
    }
}

// 卡牌可选择的类型
public enum CardSelectType
{
    None = 0, // 点击无效果
    HandCard, // 手牌
    MedicDiscardCard, // 复活弃牌区
    WithstandAttack, // 准备被攻击
}

// 卡牌当前所在区域
public enum CardLocation
{
    None = 0, // 无ui显示，例如备选卡牌堆中
    HandArea, // 手牌区
    DiscardArea, // 弃牌区
    BattleArea, // 对战区
    SelfBattleArea, // 本方对战区
    EnemyBattleArea, // 对方对战区
}

// buff类型，明确记录增减益效果来源
public enum CardBuffType
{
    // 增益buff
    Bond = 0, // 点数加倍
    Morale, // +1
    Horn, // 点数加倍
    // 减益buff
    Attack2, // -2
    Attack4, // -4
    Weather, // 基础数值降为1
}
