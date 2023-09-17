// 角色卡

public enum RoleCardGroup
{
    KumikoFirstYearS1 = 0,
    KumikoFirstYearS2,
    KumikoSecondYear
}

public enum RoleCardBadgeType
{
    Wood = 0,
    Brass,
    Percussion
}

public enum RoleCardAbility // TODO
{
    None = 0,
    Spy,
    Attack
}

public enum RoleCardType
{
    Normal = 0,
    Hero
}

public struct RoleCardInfo
{
    string roleName; // 人物姓名，与Card::name_相同，通常作为key值索引
    RoleCardGroup group; // 牌组
    int grade; // 年级
    int originPower; // 初始点数
    RoleCardBadgeType badgeType; // 类型，位于场上哪一排
    RoleCardAbility ability; // 特殊能力
    RoleCardType roleType; // 是否为英雄牌
    string quote; // 卡牌最下方的台词引用
}

abstract public class RoleCard : Card
{
    protected readonly int originPower_; // 该角色卡的初始点数，不可修改

    protected RoleCardInfo roleInfo_; // 从配置文件中获取的角色信息

    public RoleCard(int originPower)
    {
        originPower_ = originPower;
    }

    public int GetOriginPower()
    {
        return originPower_;
    }

    public abstract int GetBasePower();

    public abstract int GetCurrentPower();

}
