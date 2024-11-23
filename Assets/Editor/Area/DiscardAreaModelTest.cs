using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

public class DiscardAreaModelTest
{
    public List<int> GetExpectedRowCardNum(int cardNum)
    {
        List<int> rowCardNumList = new List<int> { 0, 0, 0 };
        int remain = cardNum;
        for (int i = 0; i < DiscardAreaModel.rowNum; i++) {
            if (remain <= 0) {
                break;
            }
            int num = Math.Min(remain, 10);
            rowCardNumList[i] = num;
            remain -= num;
        }
        for (int i = 0; i < DiscardAreaModel.rowNum; i++) {
            int extra = (remain % 3) - i > 0 ? 1 : 0;
            rowCardNumList[i] += remain / 3 + extra;
        }
        return rowCardNumList;
    }

    // 测试所有废弃角色牌的情况
    [TestCase(25)]
    [TestCase(37)]
    public void AllCard(int cardNum)
    {
        DiscardAreaModel discardAreaModel = new DiscardAreaModel();
        List<int> infoIdList = Enumerable.Range(2001, cardNum).ToList();
        List<CardModel> cardList = TestGenCards.GetCardList(infoIdList);

        foreach (CardModel card in cardList) {
            discardAreaModel.AddCard(card);
        }
        discardAreaModel.SetRow(false);
        List<DiscardRowAreaModel> rowAreaList = discardAreaModel.rowAreaList;
        List<int> rowCardNumList = GetExpectedRowCardNum(cardNum);
        for (int i = 0; i < DiscardAreaModel.rowNum; i++) {
            Assert.AreEqual(rowCardNumList[i], rowAreaList[i].cardList.Count);
            foreach (CardModel card in rowAreaList[i].cardList) {
                Assert.AreEqual(CardLocation.DiscardArea, card.cardLocation);
                Assert.AreEqual(CardSelectType.None, card.selectType);
            }
        }

        CardModel removeCard = rowAreaList[0].cardList[0];
        discardAreaModel.RemoveCard(removeCard);
        Assert.AreEqual(CardLocation.None, removeCard.cardLocation);
        Assert.AreEqual(CardSelectType.None, removeCard.selectType);

        discardAreaModel.SetRow(false);
        rowAreaList = discardAreaModel.rowAreaList;
        rowCardNumList = GetExpectedRowCardNum(cardNum - 1); // 移除了一张牌
        for (int i = 0; i < DiscardAreaModel.rowNum; i++) {
            Assert.AreEqual(rowCardNumList[i], rowAreaList[i].cardList.Count);
            foreach (CardModel card in rowAreaList[i].cardList) {
                Assert.AreEqual(CardLocation.DiscardArea, card.cardLocation);
                Assert.AreEqual(CardSelectType.None, card.selectType);
            }
        }

        discardAreaModel.ClearRow();
        rowAreaList = discardAreaModel.rowAreaList;
        for (int i = 0; i < DiscardAreaModel.rowNum; i++) {
            Assert.AreEqual(0, rowAreaList[i].cardList.Count); // 没有SetRow，list为空
        }
    }

    // 测试非英雄角色牌的情况
    [TestCase(25)]
    [TestCase(38)]
    public void NormalCard(int cardNum)
    {
        DiscardAreaModel discardAreaModel = new DiscardAreaModel();
        List<int> infoIdList = Enumerable.Range(2001, cardNum).ToList();
        List<CardModel> cardList = TestGenCards.GetCardList(infoIdList);
        List<CardModel> normalCardList = new List<CardModel>(cardList);
        normalCardList.RemoveAll(o => { return o.cardInfo.cardType != CardType.Normal; });
        int normalCardNum = normalCardList.Count;

        foreach (CardModel card in cardList) {
            discardAreaModel.AddCard(card);
        }
        discardAreaModel.SetRow(true);
        List<DiscardRowAreaModel> rowAreaList = discardAreaModel.rowAreaList;
        List<int> rowCardNumList = GetExpectedRowCardNum(normalCardNum);
        for (int i = 0; i < DiscardAreaModel.rowNum; i++) {
            Assert.AreEqual(rowCardNumList[i], rowAreaList[i].cardList.Count);
            foreach (CardModel card in rowAreaList[i].cardList) {
                Assert.AreEqual(CardLocation.DiscardArea, card.cardLocation);
                Assert.AreEqual(CardSelectType.None, card.selectType);
            }
        }
    }
}
