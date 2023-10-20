using System.Collections.Generic;
using UnityEngine;

// 手牌区域
public class HandArea : CardArea
{
    public override void AddCard(GameObject newCard)
    {
        base.AddCard(newCard);
        newCard.GetComponent<CardSelect>().EnableSelect(true);
    }

    public void PlayMusterCard(string musterType)
    {
        List<GameObject> musterCards = new List<GameObject>();
        foreach (GameObject card in cardList) {
            if (card.GetComponent<CardDisplay>().GetCardInfo().musterType == musterType) {
                musterCards.Add(card);
            }
        }
        foreach(GameObject card in musterCards) {
            card.GetComponent<CardSelect>().PlayPassively();
        }
    }
}
