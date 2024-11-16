using System.Collections.Generic;
using UnityEngine;
/**
 * 对战区行区域逻辑
 */
public class BattleRowAreaModel : RowAreaModel
{
    CardBadgeType rowType = CardBadgeType.None;

    public BattleRowAreaModel(CardBadgeType rowType)
    {
        this.rowType = rowType;
    }

    public override void AddCard(CardModel card)
    {
        card.cardLocation = CardLocation.BattleArea;
        base.AddCard(card);
        UpdateBuff();
    }

    public override void RemoveCard(CardModel card)
    {
        card.RemoveAllBuff();
        base.RemoveCard(card);
        UpdateBuff();
    }

    public void RemoveAllCard()
    {
        foreach (CardModel card in cardList) {
            card.RemoveAllBuff();
            card.cardLocation = CardLocation.None;
        }
        cardList.Clear();
        UpdateBuff();
    }

    // 伞击技能，木管行总点数大于10，移除点数最高的若干卡牌，并返回移除的卡牌列表
    public List<CardModel> ApplyScorchWood()
    {
        List <CardModel> targetCardList = new List<CardModel>();
        if (rowType != CardBadgeType.Wood || GetCurrentPower() <= 10) {
            return targetCardList;
        }
        int maxPower = -1;
        foreach (CardModel card in cardList) {
            if (card.GetCardInfo().cardType != CardType.Normal) {
                continue;
            }
            int cardPower = card.GetCurrentPower();
            if (cardPower > maxPower) {
                targetCardList.Clear();
                targetCardList.Add(card);
                maxPower = cardPower;
            } else if (cardPower == maxPower) {
                targetCardList.Add(card);
            }
        }
        foreach (CardModel card in targetCardList) {
            RemoveCard(card);
        }
        return targetCardList;
    }

    public int GetCurrentPower()
    {
        int sum = 0;
        foreach (CardModel card in cardList) {
            sum += card.GetCurrentPower();
        }
        return sum;
    }

    // 更新天气、horn、morale等行生效的buff
    private void UpdateBuff()
    {
        UpdateMoraleBuff();
        UpdateHornBuff();
    }

    private void UpdateMoraleBuff()
    {
        int moraleCount = 0;
        foreach (CardModel card in cardList) {
            if (card.GetCardInfo().ability == CardAbility.Morale) {
                moraleCount++;
            }
        }
        foreach (CardModel card in cardList) {
            if (card.GetCardInfo().ability != CardAbility.Morale) {
                card.SetBuff(CardBuffType.Morale, moraleCount);
            } else if (moraleCount > 0) {
                card.SetBuff(CardBuffType.Morale, moraleCount - 1);
            }
        }
    }

    private void UpdateHornBuff()
    {
        int hornCount = 0;
        foreach (CardModel card in cardList) {
            if (card.GetCardInfo().ability == CardAbility.Horn) {
                hornCount++;
            }
        }
        foreach (CardModel card in cardList) {
            if (card.GetCardInfo().ability != CardAbility.Horn) {
                card.SetBuff(CardBuffType.Horn, hornCount);
            } else if (hornCount > 0) {
                card.SetBuff(CardBuffType.Horn, hornCount - 1);
            }
        }
    }
}
