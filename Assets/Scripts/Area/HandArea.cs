using System.Collections.Generic;
using UnityEngine;

// 手牌区域
public class HandArea : CardArea
{
    public override void AddCard(GameObject newCard)
    {
        base.AddCard(newCard);
        newCard.GetComponent<CardAction>().cardLocation = CardLocation.HandArea;
    }

    public List<GameObject> GetMusterCardList(string musterType)
    {
        List<GameObject> list = new List<GameObject>();
        foreach (GameObject card in cardList) {
            if (card.GetComponent<CardDisplay>().GetCardInfo().musterType == musterType) {
                list.Add(card);
            }
        }
        foreach (GameObject card in list) {
            cardList.Remove(card);
        }
        return list;
    }
}
