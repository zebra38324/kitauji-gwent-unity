using System;
using System.Collections.Immutable;
using System.Linq;
/*
 * 记录卡牌状态与卡牌点数计算逻辑（加减buff、变更属性等）
 * 角色牌点数计算：优先结算天气、而后翻倍、最后加减
 */
public record CardModel
{
    public CardInfo cardInfo { get; init; }
    public CardLocation cardLocation { get; init; } = CardLocation.None;
    public CardSelectType cardSelectType { get; init; } = CardSelectType.None;
    public bool isSelected { get; init; } = false; // 初始化选择重抽手牌时使用
    public int currentPower { get; init; } = 0;
    public bool hasScorch { get; init; } = false; // 是否已被退部
    private ImmutableList<int> buffRecord { get; init; } = ImmutableList.CreateRange(Enumerable.Repeat(0, (int)CardBuffType.Count));

    public CardModel(CardInfo info)
    {
        cardInfo = new CardInfo(info);
        currentPower = cardInfo.originPower;
    }

    // ========================= ui相关逻辑 ===========================
    public CardModel ChangeCardLocation(CardLocation location)
    {
        if (location == cardLocation) {
            return this;
        }
        return this with {
            cardLocation = location,
            cardSelectType = GetSelectType(location)
        };
    }

    public CardModel ChangeCardSelectType(CardSelectType type)
    {
        if (type == cardSelectType) {
            return this;
        }
        return this with {
            cardSelectType = type
        };
    }

    public CardModel ChangeSelectStatus(bool isSelected_)
    {
        if (isSelected_ == isSelected) {
            return this;
        }
        return this with {
            isSelected = isSelected_
        };
    }

    private CardSelectType GetSelectType(CardLocation location)
    {
        CardSelectType type = CardSelectType.None;
        if (location == CardLocation.HandArea || location == CardLocation.SelfLeaderCardArea || location == CardLocation.EnemyLeaderCardArea) {
            type = CardSelectType.PlayCard;
        } else if (location == CardLocation.InitHandArea) {
            type = CardSelectType.ReDrawHandCard;
        } else {
            type = CardSelectType.None;
        }
        return type;
    }

    // ========================= 点数逻辑 ===========================
    // 添加buff并指定添加buff的数量
    public CardModel AddBuff(CardBuffType buffType, int num)
    {
        if (!EnableModifyBuff()) {
            return this;
        }
        int oldValue = buffRecord[(int)buffType];
        var newBuffRecord = buffRecord.SetItem((int)buffType, oldValue + num);
        return this with {
            buffRecord = newBuffRecord,
            currentPower = GetPower(newBuffRecord)
        };
    }

    // 移除buff并指定移除buff的数量，为0时表示移除所有
    public CardModel RemoveBuff(CardBuffType buffType, int num = 0)
    {
        if (!EnableModifyBuff()) {
            return this;
        }
        int oldValue = buffRecord[(int)buffType];
        int newValue = 0;
        if (num > 0) {
            newValue = Math.Max(oldValue - num, 0);
        } else {
            newValue = 0;
        }
        var newBuffRecord = buffRecord.SetItem((int)buffType, newValue);
        return this with {
            buffRecord = newBuffRecord,
            currentPower = GetPower(newBuffRecord)
        };
    }

    // 清除所有除了天气的debuff
    public CardModel RemoveNormalDebuff()
    {
        var newBuffRecord = buffRecord.SetItem((int)CardBuffType.Attack2, 0);
        newBuffRecord = newBuffRecord.SetItem((int)CardBuffType.Attack4, 0);
        return this with {
            buffRecord = newBuffRecord,
            currentPower = GetPower(newBuffRecord)
        };
    }

    public CardModel RemoveAllBuff()
    {
        var newBuffRecord = buffRecord;
        for (int i = 0; i < (int)CardBuffType.Count; i++) {
            newBuffRecord = newBuffRecord.SetItem(i, 0);
        }
        return this with {
            buffRecord = newBuffRecord,
            currentPower = GetPower(newBuffRecord),
            hasScorch = false
        };
    }

    // 设置buff为指定数量
    public CardModel SetBuff(CardBuffType buffType, int num)
    {
        return RemoveBuff(buffType)
            .AddBuff(buffType, num);
    }

    public bool IsDead()
    {
        return hasScorch || (currentPower <= 0 && currentPower < cardInfo.originPower);
    }

    public CardModel SetScorch()
    {
        return this with {
            hasScorch = true
        };
    }

    private int GetPower(ImmutableList<int> newBuffRecord)
    {
        int power = cardInfo.originPower;
        if (newBuffRecord[(int)CardBuffType.Weather] > 0) {
            power = Math.Min(cardInfo.originPower, 1);
        }
        // Bond与Horn的倍数计算需要+1
        int times = newBuffRecord[(int)CardBuffType.Bond] + newBuffRecord[(int)CardBuffType.Horn] + 1;
        if (times > 0) {
            power *= times;
        }
        int diff = newBuffRecord[(int)CardBuffType.Morale] +
            5 * newBuffRecord[(int)CardBuffType.Kasa] +
            2 * newBuffRecord[(int)CardBuffType.Monaka] -
            2 * newBuffRecord[(int)CardBuffType.Attack2] -
            4 * newBuffRecord[(int)CardBuffType.Attack4];
        power += diff;
        return power;
    }

    // 是否支持变更buff
    private bool EnableModifyBuff()
    {
        return cardInfo.cardType == CardType.Normal;
    }
}

