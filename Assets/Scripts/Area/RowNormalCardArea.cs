using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// 卡牌区域，主要用于场上普通牌的排布
public class RowNormalCardArea : CardArea
{
    public override void AddCard(GameObject newCard)
    {
        // morale和horn这种只影响本行的buff，在row area这层操作就可以了
        base.AddCard(newCard);
        UpdateBuff();
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

    public override void RemoveCard(GameObject card)
    {
        base.RemoveCard(card);
        UpdateBuff(); // 先移除卡牌，再对剩下的牌结算影响
    }

    public void ClearCard(DiscardCardManager manager)
    {
        List<GameObject> tempList = new List<GameObject>();
        foreach (GameObject card in cardList) {
            card.GetComponent<CardAction>().cardLocation = CardLocation.DiscardArea;
            manager.AddCard(card);
            tempList.Add(card);
        }
        foreach (GameObject card in tempList) {
            RemoveCard(card);
            card.GetComponent<CardDisplay>().RemoveAllBuff();
        }
    }

    // 更新本行的morale与horn
    private void UpdateBuff()
    {
        UpdateMoraleBuff();
        UpdateHornBuff();
    }

    private void UpdateMoraleBuff()
    {
        int moraleCount = 0;
        foreach (GameObject card in cardList) {
            if (card.GetComponent<CardDisplay>().GetCardInfo().ability == CardAbility.Morale) {
                moraleCount++;
            }
        }
        foreach (GameObject card in cardList) {
            if (card.GetComponent<CardDisplay>().GetCardInfo().ability != CardAbility.Morale) {
                card.GetComponent<CardDisplay>().SetBuff(CardBuffType.Morale, moraleCount);
            } else if (moraleCount > 0) {
                card.GetComponent<CardDisplay>().SetBuff(CardBuffType.Morale, moraleCount - 1);
            }
        }
    }

    private void UpdateHornBuff()
    {
        int hornCount = 0;
        foreach (GameObject card in cardList) {
            if (card.GetComponent<CardDisplay>().GetCardInfo().ability == CardAbility.Horn) {
                hornCount++;
            }
        }
        foreach (GameObject card in cardList) {
            if (card.GetComponent<CardDisplay>().GetCardInfo().ability != CardAbility.Horn) {
                card.GetComponent<CardDisplay>().SetBuff(CardBuffType.Horn, hornCount);
            } else if (hornCount > 0) {
                card.GetComponent<CardDisplay>().SetBuff(CardBuffType.Horn, hornCount - 1);
            }
        }
    }
}
