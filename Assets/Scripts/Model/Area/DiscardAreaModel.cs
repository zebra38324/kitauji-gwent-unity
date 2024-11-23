using System;
using System.Collections.Generic;
/**
 * 弃牌区域的逻辑
 */
public class DiscardAreaModel
{
    private static string TAG = "DiscardAreaModel";

    public static readonly int rowNum = 3;

    private static readonly int rowMaxCardNum = 10; // 每行最多十张牌

    public List<DiscardRowAreaModel> rowAreaList {  get; private set; }

    public List<CardModel> normalCardList { get; private set; } // 非英雄的角色牌

    private List<CardModel> cardList { get; set; } // 所有的角色牌

    public DiscardAreaModel()
    {
        rowAreaList = new List<DiscardRowAreaModel>();
        for (int i = 0; i < rowNum; i++) {
            rowAreaList.Add(new DiscardRowAreaModel());
        }
        cardList = new List<CardModel>();
        normalCardList = new List<CardModel>();
    }

    public void AddCard(CardModel card)
    {
        card.cardLocation = CardLocation.DiscardArea;
        cardList.Add(card);
        if (card.cardInfo.cardType == CardType.Normal) {
            normalCardList.Add(card);
        }
    }

    public void RemoveCard(CardModel card)
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
        List<CardModel> targetList = null;
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
            List<CardModel> rowCardList = targetList.GetRange(startIndex, num);
            rowArea.AddCardList(rowCardList);
            startIndex += num;
            remain -= num;
        }
        int rowIndex = 0;
        // 还剩下的一行一个平均放
        while (remain > 0) {
            CardModel card = targetList[startIndex];
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
