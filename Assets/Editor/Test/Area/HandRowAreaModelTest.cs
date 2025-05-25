using NUnit.Framework;
using System;
using System.Collections.Generic;

public class HandRowAreaModelTest
{
    private static string TAG = "HandRowAreaModelTest";

    [SetUp]
    public void SetUp()
    {
        KLog.I(TAG, "SetUp");
    }

    [Test]
    public void Normal()
    {
        HandRowAreaModel handRowAreaModel = new HandRowAreaModel();
        handRowAreaModel.AddCardList(TestGenCards.GetCardList(new List<int> { 2001, 2002, 2003 }));
        List<CardModelOld> cardList = handRowAreaModel.cardList;
        foreach (CardModelOld cardModel in cardList) {
            Assert.AreEqual(CardLocation.HandArea, cardModel.cardLocation);
            Assert.AreEqual(CardSelectType.PlayCard, cardModel.selectType);
        }
        int targetId = cardList[0].cardInfo.id;
        CardModelOld targetCard = handRowAreaModel.FindCard(targetId);
        Assert.AreNotEqual(null, targetCard);
        handRowAreaModel.RemoveCard(targetCard);
        Assert.AreEqual(2, handRowAreaModel.cardList.Count);
        Assert.AreEqual(CardLocation.None, targetCard.cardLocation);
        Assert.AreEqual(CardSelectType.None, targetCard.selectType);
    }
}
