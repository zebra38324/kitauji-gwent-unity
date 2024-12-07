using System;
using UnityEngine;
/*
 * 记录卡牌状态与卡牌点数计算逻辑（加减buff、变更属性等）
 * 角色牌点数计算：优先结算天气、而后翻倍、最后加减
 */
public class CardModel
{
    private static int buffTypeCount = Enum.GetValues(typeof(CardBuffType)).Length;

    private int[] buffRecord; // 记录每种buff的数量

    public int currentPower { get; private set; }

    public CardInfo cardInfo {  get; private set; }

    private CardLocation cardLocation_ = CardLocation.None;

    public CardLocation cardLocation {
        get {
            return cardLocation_;
        }
        set {
            cardLocation_ = value;
            if (cardLocation_ == CardLocation.HandArea) {
                selectType = CardSelectType.PlayCard;
            } else if (cardLocation_ == CardLocation.InitHandArea) {
                selectType = CardSelectType.ReDrawHandCard;
            } else {
                selectType = CardSelectType.None;
            }
        }
    }

    private CardSelectType selectType_ = CardSelectType.None;
    public CardSelectType selectType {
        get {
            return selectType_;
        }
        set {
            selectType_ = value;
        }
    }
    public bool isSelected = false; // 初始化选择重抽手牌时使用

    public CardModel(CardInfo info)
    {
        buffRecord = new int[buffTypeCount];
        this.cardInfo = new CardInfo(info);
        currentPower = info.originPower;
    }

    // ========================= 点数逻辑 ===========================
    // 添加buff并指定添加buff的数量
    public void AddBuff(CardBuffType buffType, int num)
    {
        if (!EnableModifyBuff()) {
            return;
        }
        buffRecord[(int)buffType] += num;
        UpdateCurrentPower();
    }

    // 移除buff并指定移除buff的数量，为0时表示移除所有
    public void RemoveBuff(CardBuffType buffType, int num = 0)
    {
        if (!EnableModifyBuff()) {
            return;
        }
        if (num > 0) {
            buffRecord[(int)buffType] -= Math.Min(num, buffRecord[(int)buffType]);
        } else {
            buffRecord[(int)buffType] = 0;
        }
        UpdateCurrentPower();
    }

    // 清除所有除了天气的debuff
    public void RemoveNormalDebuff()
    {
        buffRecord[(int)CardBuffType.Attack2] = 0;
        buffRecord[(int)CardBuffType.Attack4] = 0;
        UpdateCurrentPower();
    }

    public void RemoveAllBuff()
    {
        for (int i = 0; i < buffTypeCount; i++) {
            buffRecord[i] = 0;
        }
        UpdateCurrentPower();
    }

    // 设置buff为指定数量
    public void SetBuff(CardBuffType buffType, int num)
    {
        RemoveBuff(buffType);
        AddBuff(buffType, num);
        UpdateCurrentPower();
    }

    public bool IsDead()
    {
        return currentPower <= 0 && currentPower < cardInfo.originPower;
    }

    private void UpdateCurrentPower()
    {
        int power = cardInfo.originPower;
        if (buffRecord[(int)CardBuffType.Weather] > 0) {
            power = Math.Min(cardInfo.originPower, 1);
        }
        // Bond与Horn的倍数计算需要+1
        int times = buffRecord[(int)CardBuffType.Bond] + buffRecord[(int)CardBuffType.Horn] + 1;
        if (times > 0) {
            power *= times;
        }
        int diff = buffRecord[(int)CardBuffType.Morale] -
            2 * buffRecord[(int)CardBuffType.Attack2] -
            4 * buffRecord[(int)CardBuffType.Attack4];
        power += diff;
        currentPower = power;
    }

    // 是否支持变更buff
    private bool EnableModifyBuff()
    {
        // TODO: 考虑技能牌（包括天气）
        return cardInfo.cardType == CardType.Normal;
    }
}
