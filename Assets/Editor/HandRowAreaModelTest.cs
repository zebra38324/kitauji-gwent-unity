using NUnit.Framework;
using System;
using System.Collections.Generic;

public class HandRowAreaModelTest
{
    private List<CardInfo> cardInfoList;

    public HandRowAreaModelTest()
    {
        int total = 10;
        cardInfoList = new List<CardInfo>();
        for (int i = 0; i < total; i++) {
            cardInfoList.Add(new CardInfo());
        }
    }

    public List<CardInfo> GetCardInfoList(int num)
    {
        return cardInfoList.GetRange(0, num);
    }

    [Test]
    public void Normal()
    {
        HandRowAreaModel handRowAreaModel = new HandRowAreaModel();
        handRowAreaModel.AddCardList(CardGenerator.Instance.GetCards(GetCardInfoList(3), 3));
        List<CardModel> cardList = handRowAreaModel.GetCardList();
        foreach (CardModel cardModel in cardList) {
            Assert.AreEqual(CardLocation.HandArea, cardModel.cardLocation);
            Assert.AreEqual(CardSelectType.HandCard, cardModel.selectType);
        }
        int targetId = cardList[0].GetCardInfo().id;
        CardModel targetCard = handRowAreaModel.FindCard(targetId);
        Assert.AreNotEqual(null, targetCard);
        handRowAreaModel.RemoveCard(targetCard);
        Assert.AreEqual(2, handRowAreaModel.GetCardList().Count);
        Assert.AreEqual(CardLocation.None, targetCard.cardLocation);
        Assert.AreEqual(CardSelectType.None, targetCard.selectType);
    }
}
