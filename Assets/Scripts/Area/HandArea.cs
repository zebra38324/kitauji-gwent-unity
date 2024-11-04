using System.Collections.Generic;
using UnityEngine;

// 手牌区域
public class HandArea : CardArea
{
    public override void AddCard(GameObject newCard)
    {
        base.AddCard(newCard);
        //newCard.GetComponent<CardInfoDisplay>().SetEnableUp(true);
        newCard.GetComponent<CardSelect>().selectType = CardSelectType.HandCard;
    }

    public void PlayMusterCard(string musterType)
    {
        foreach (GameObject card in cardList) {
            if (card.GetComponent<CardDisplay>().GetCardInfo().musterType == musterType) {
                card.GetComponent<CardSelect>().PlayPassively();
                break; // 打出一张牌就够，剩下的由链式调用逐张打出
            }
        }
    }
}
