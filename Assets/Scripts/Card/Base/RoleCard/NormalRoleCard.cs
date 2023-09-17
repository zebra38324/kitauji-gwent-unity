// 普通角色牌，无特殊效果
abstract public class NormalCard : RoleCard
{
    // 所有buff影响，分为两种，一是针对基准点数进行调整，二是调整基准点数要乘的倍数
    // 无buff影响时，三个点数值相同
    private int basePower_; // 基准点数
    private int currentPower_; // 当前点数，计算各种buff影响之后得到

    public NormalCard(int originPower) : base(originPower)
    {
        basePower_ = originPower;
        currentPower_ = originPower;
    }

    public override int GetBasePower()
    {
        return basePower_;
    }

    public override int GetCurrentPower()
    {
        return currentPower_;
    }
}
