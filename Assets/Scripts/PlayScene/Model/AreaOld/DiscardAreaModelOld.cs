using System;
using System.Collections.Generic;
/**
 * 弃牌区域的逻辑
 */
public class DiscardAreaModelOld
{
    private static string TAG = "DiscardAreaModelOld";

    public static readonly int rowNum = 3;

    private static readonly int rowMaxCardNum = 10; // 每行最多十张牌

    public List<DiscardRowAreaModel> rowAreaList {  get; private set; }

    public List<CardModelOld> normalCardList { get; private set; } // 非英雄的角色牌

    public List<CardModelOld> cardList { get; private set; } // 所有的角色牌

    public DiscardAreaModelOld()
    {
        rowAreaList = new List<DiscardRowAreaModel>();
        for (int i = 0; i < rowNum; i++) {
            rowAreaList.Add(new DiscardRowAreaModel());
        }
        cardList = new List<CardModelOld>();
        normalCardList = new List<CardModelOld>();
    }

    public void AddCard(CardModelOld card)
    {
        card.cardLocation = CardLocation.DiscardArea;
        cardList.Add(card);
        if (card.cardInfo.cardType == CardType.Normal) {
            normalCardList.Add(card);
        }
    }

    public void RemoveCard(CardModelOld card)
    {
        card.cardLocation = CardLocation.None;
        if (cardList.Exists(t => t == card)) {
            cardList.Remove(card);
        }
        if (normalCardList.Exists(t => t == card)) {
            normalCardList.Remove(card);
        }
    }

    public void SetRow(bool onlyNormal)
    {
        ClearRow();
        List<CardModelOld> targetList = null;
        if (onlyNormal) {
            targetList = normalCardList;
        } else {
            targetList = cardList;
        }
        int startIndex = 0;
        int remain = targetList.Count;
        // 先一行rowMaxCardNum个
        foreach (DiscardRowAreaModel rowArea in rowAreaList) {
            if (remain <= 0) {
                break;
            }
            int num = Math.Min(remain, rowMaxCardNum);
            List<CardModelOld> rowCardList = targetList.GetRange(startIndex, num);
            rowArea.AddCardList(rowCardList);
            startIndex += num;
            remain -= num;
        }
        int rowIndex = 0;
        while (remain > 0) {
            CardModelOld card = targetList[startIndex];
            rowAreaList[rowIndex].AddCard(card);
            startIndex += 1;
            remain -= 1;
            rowIndex = (rowIndex + 1) % rowNum;
        }
        KLog.I(TAG, "SetRow onlyNormal = " + onlyNormal + ", row card num = " +
            rowAreaList[0].cardList.Count + ", " +
            rowAreaList[1].cardList.Count + ", " +
            rowAreaList[2].cardList.Count);
    }

    public void ClearRow()
    {
        foreach (DiscardRowAreaModel rowArea in rowAreaList) {
            rowArea.RemoveAllCard();
        }
    }
}
