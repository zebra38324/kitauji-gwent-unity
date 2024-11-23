using NUnit.Framework;
using System;
using System.Collections.Generic;

public class CardGeneratorTest
{
    private static string TAG = "CardGeneratorTest";

    private List<CardInfo> cardInfoList;

    private CardGenerator cardGenerator;

    [SetUp]
    public void SetUp()
    {
        KLog.I(TAG, "SetUp");
        cardGenerator = new CardGenerator(CardGenerator.serverSalt);
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
        CardModel cardModel1 = cardGenerator.GetCard(cardInfo);
        CardModel cardModel2 = cardGenerator.GetCard(cardInfo);

        Assert.AreNotEqual(cardModel1.cardInfo.id, cardModel2.cardInfo.id);
    }
}
