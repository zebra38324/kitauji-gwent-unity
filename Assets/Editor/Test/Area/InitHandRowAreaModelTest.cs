using NUnit.Framework;
using System;
using System.Collections.Generic;

public class InitHandRowAreaModelTest
{
    private static string TAG = "InitHandRowAreaModelTest";

    [SetUp]
    public void SetUp()
    {
        KLog.I(TAG, "SetUp");
    }

    [Test]
    public void Normal()
    {
        InitHandRowAreaModel initHandRowAreaModel = new InitHandRowAreaModel();
        initHandRowAreaModel.AddCardList(TestGenCards.GetCardList(new List<int> { 2001, 2002, 2003 }));
        List<CardModelOld> cardList = initHandRowAreaModel.cardList;
        foreach (CardModelOld cardModel in cardList) {
            Assert.AreEqual(CardLocation.InitHandArea, cardModel.cardLocation);
            Assert.AreEqual(CardSelectType.ReDrawHandCard, cardModel.selectType);
        }
        initHandRowAreaModel.SelectCard(cardList[0]);
        initHandRowAreaModel.SelectCard(cardList[1]);
        initHandRowAreaModel.SelectCard(cardList[2]);
        Assert.AreEqual(true, cardList[0].isSelected);
        Assert.AreEqual(true, cardList[1].isSelected);
        Assert.AreEqual(false, cardList[2].isSelected);
        initHandRowAreaModel.SelectCard(cardList[0]);
        initHandRowAreaModel.SelectCard(cardList[2]);
        Assert.AreEqual(false, cardList[0].isSelected);
        Assert.AreEqual(true, cardList[1].isSelected);
        Assert.AreEqual(true, cardList[2].isSelected);
    }
}
