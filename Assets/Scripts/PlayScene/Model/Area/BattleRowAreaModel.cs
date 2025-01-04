using System.Collections.Generic;
using UnityEngine;
/**
 * 对战区行区域逻辑
 */
public class BattleRowAreaModel : RowAreaModel
{
    private bool hasWeatherBuff_;
    public bool hasWeatherBuff {
        get {
            return hasWeatherBuff_;
        }
        set {
            hasWeatherBuff_ = value;
            UpdateWeatherBuff();
        }
    }

    // 指导老师牌
    public SingleCardRowAreaModel hornUtilCardArea { get; private set; }

    CardBadgeType rowType = CardBadgeType.None;

    public BattleRowAreaModel(CardBadgeType rowType)
    {
        this.rowType = rowType;
        hasWeatherBuff = false;
        hornUtilCardArea = new SingleCardRowAreaModel();
    }

    public override void AddCardList(List<CardModel> newCardList)
    {
        foreach (CardModel card in newCardList) {
            card.cardLocation = CardLocation.BattleArea;
        }
        base.AddCardList(newCardList);
        UpdateBuff();
    }

    public override void AddCard(CardModel card)
    {
        if (card.cardInfo.ability == CardAbility.HornUtil || card.cardInfo.ability == CardAbility.HornBrass) {
            hornUtilCardArea.AddCard(card);
        } else {
            base.AddCard(card);
        }
        card.cardLocation = CardLocation.BattleArea;
        UpdateBuff();
    }

    public override void RemoveCard(CardModel card)
    {
        card.RemoveAllBuff();
        base.RemoveCard(card);
        UpdateBuff();
    }

    public override void RemoveAllCard()
    {
        foreach (CardModel card in cardList) {
            card.RemoveAllBuff();
        }
        base.RemoveAllCard();
        hornUtilCardArea.RemoveAllCard();
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
            if (card.cardInfo.cardType != CardType.Normal) {
                continue;
            }
            int cardPower = card.currentPower;
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

    public override int GetCurrentPower()
    {
        int sum = 0;
        foreach (CardModel card in cardList) {
            sum += card.currentPower;
        }
        return sum;
    }

    // 更新天气、horn、morale等行生效的buff
    private void UpdateBuff()
    {
        UpdateMoraleBuff();
        UpdateHornBuff();
        UpdateWeatherBuff();
    }

    private void UpdateMoraleBuff()
    {
        int moraleCount = 0;
        foreach (CardModel card in cardList) {
            if (card.cardInfo.ability == CardAbility.Morale) {
                moraleCount++;
            }
        }
        foreach (CardModel card in cardList) {
            if (card.cardInfo.ability != CardAbility.Morale) {
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
            if (card.cardInfo.ability == CardAbility.Horn) {
                hornCount++;
            }
        }
        if (hornUtilCardArea.cardList.Count > 0) {
            hornCount += 1;
        }
        foreach (CardModel card in cardList) {
            if (card.cardInfo.ability != CardAbility.Horn) {
                card.SetBuff(CardBuffType.Horn, hornCount);
            } else if (hornCount > 0) {
                card.SetBuff(CardBuffType.Horn, hornCount - 1);
            }
        }
    }

    private void UpdateWeatherBuff()
    {
        foreach (CardModel card in cardList) {
            if (hasWeatherBuff) {
                card.SetBuff(CardBuffType.Weather, 1);
            } else {
                card.RemoveBuff(CardBuffType.Weather);
            }
        }
    }
}
