using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// 卡牌区域，主要用于场上普通牌的排布
public class RowNormalCardArea : CardArea
{
    private int moraleCount = 0; // TODO: 移除效果

    public override void AddCard(GameObject newCard)
    {
        TryUpdateMoraleBuff(newCard); // morale这种只影响本行的buff，在row area这层操作就可以了。morale不对自己生效，因此先加buff再添加卡牌
        base.AddCard(newCard);
        newCard.GetComponent<CardDisplay>().SetStatus(CardStatus.PlayArea);
    }

    // 消除除天气外的debuff
    public void ClearNormalDebuff()
    {
        foreach (GameObject card in cardList)
        {
            card.GetComponent<CardDisplay>().ClearNormalDebuff();
        }
    }

    // 统计一个类型bond的卡牌数量
    public int GetBondCardNum(string bondType)
    {
        int count = 0;
        foreach (GameObject card in cardList)
        {
            if (card.GetComponent<CardDisplay>().GetCardInfo().bondType == bondType) {
                count++;
            }
        }
        return count;
    }

    // 更新bond的buff
    public void UpdateBondBuff(string bondType, int times)
    {
        foreach (GameObject card in cardList)
        {
            if (card.GetComponent<CardDisplay>().GetCardInfo().bondType == bondType) {
                card.GetComponent<CardDisplay>().SetBuffTimes(times);
            }
        }
    }

    public int GetCurrentScore()
    {
        int sum = 0;
        foreach (GameObject card in cardList)
        {
            sum += card.GetComponent<CardDisplay>().GetCurrentPower();
        }
        return sum;
    }

    public void ScorchWood()
    {
        if (GetCurrentScore() <= 10) {
            return;
        }
        List<GameObject> targetCards = new List<GameObject>();
        int maxPower = -1;
        foreach (GameObject card in cardList)
        {
            if (card.GetComponent<CardDisplay>().GetCardInfo().cardType != CardType.Normal) {
                continue;
            }
            int cardPower = card.GetComponent<CardDisplay>().GetCurrentPower();
            if (cardPower > maxPower) {
                targetCards.Clear();
                targetCards.Add(card);
                maxPower = cardPower;
            } else if (cardPower == maxPower) {
                targetCards.Add(card);
            }
        }
        foreach (GameObject card in targetCards)
        {
            RemoveCard(card); // TODO: 特效
        }
    }

    public void TryUpdateMoraleBuff(GameObject newCard)
    {
        if (moraleCount > 0) {
            // 本来就有morale，先给新卡牌加上
            newCard.GetComponent<CardDisplay>().SetBuffAddMinus(moraleCount);
        }
        if (newCard.GetComponent<CardDisplay>().GetCardInfo().ability == CardAbility.Morale) {
            moraleCount++;
            foreach (GameObject card in cardList)
            {
                card.GetComponent<CardDisplay>().SetBuffAddMinus(1);
            }
        }
    }

    private void ClearMoraleBuff()
    {
        moraleCount = 0;
    }

    public void ClearCard(DiscardCardManager manager)
    {
        List<GameObject> tempList = new List<GameObject>();
        foreach(GameObject card in cardList) {
            manager.AddCard(card);
            tempList.Add(card);
        }
        foreach(GameObject card in tempList) {
            RemoveCard(card);
            card.GetComponent<CardDisplay>().ClearAllBuff();
            card.GetComponent<CardDisplay>().SetStatus(CardStatus.Discard);
        }
        ClearMoraleBuff();
    }
}
