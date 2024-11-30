using NUnit.Framework;
using System;
using System.Collections.Generic;

public class CardGeneratorTest
{
    private static string TAG = "CardGeneratorTest";

    private CardGenerator cardGenerator;

    [SetUp]
    public void SetUp()
    {
        KLog.I(TAG, "SetUp");
        cardGenerator = new CardGenerator();
    }

    // GetCard接口，id不会重复
    [Test]
    public void GetCard()
    {
        CardModel cardModel1 = cardGenerator.GetCard(2001);
        CardModel cardModel2 = cardGenerator.GetCard(2002);
        Assert.AreNotEqual(cardModel1.cardInfo.id, cardModel2.cardInfo.id);

        CardModel cardModel3 = cardGenerator.GetCard(2001, 12);
        Assert.AreEqual(12, cardModel3.cardInfo.id);
    }
}
