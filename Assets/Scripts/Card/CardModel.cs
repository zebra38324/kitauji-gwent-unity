using System;
/*
 * 卡牌模型，存储数据与逻辑（加减buff、变更属性等）
 * 包括角色牌、技能牌
 * 角色牌点数计算：优先结算天气、而后翻倍、最后加减
 */
public class CardModel
{
    // buff类型，明确记录增减益效果来源
    public enum BuffType
    {
        // 增益buff
        Bond = 0, // 翻倍
        Morale, // +1
        Horn, // 翻倍
        // 减益buff
        Attack2, // -2
        Attack4, // -4
        Weather, // 基础数值降为1
    }

    private static int buffTypeCount = Enum.GetValues(typeof(BuffType)).Length;

    private int[] buffRecord; // 记录每种buff的数量

    private CardInfo cardInfo;

    private int currentPower;


    public CardModel(CardInfo info)
    {
        buffRecord = new int[buffTypeCount];
        this.cardInfo = info;
        currentPower = info.originPower;
    }

    public int GetCurrentPower()
    {
        return currentPower;
    }

    public int GetOriginPower()
    {
        return cardInfo.originPower;
    }

    // 添加buff并指定添加buff的数量
    public void AddBuff(BuffType buffType, int num)
    {
        buffRecord[(int)buffType] += num;
    }

    // 移除buff并指定移除buff的数量，为0时表示移除所有
    public void RemoveBuff(BuffType buffType, int num = 0)
    {
        if (num > 0) {
            buffRecord[(int)buffType] -= Math.Min(num, buffRecord[(int)buffType]);
        } else {
            buffRecord[(int)buffType] = 0;
        }
    }
}