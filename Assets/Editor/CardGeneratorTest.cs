using NUnit.Framework;
using System;
using System.Collections.Generic;

public class CardGeneratorTest
{
    private List<CardInfo> cardInfoList;

    public CardGeneratorTest()
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

    // GetCard接口，id不会重复
    [Test]
    public void GetCard()
    {
        CardInfo cardInfo = cardInfoList[0];
        CardModel cardModel1 = CardGenerator.Instance.GetCard(cardInfo);
        CardModel cardModel2 = CardGenerator.Instance.GetCard(cardInfo);

        Assert.AreNotEqual(cardModel1.GetCardInfo().id, cardModel2.GetCardInfo().id);
    }

    [TestCase(3, 2)]
    [TestCase(3, 3)]
    [TestCase(3, 4)]
    public void GetCards(int infoListNum, int num)
    {
        List<CardInfo> infoList = GetCardInfoList(infoListNum);
        List<CardModel> modelList = CardGenerator.Instance.GetCards(infoList, num);
        Assert.AreEqual(Math.Min(infoListNum, num), modelList.Count);
    }
}
