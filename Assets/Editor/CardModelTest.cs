using NUnit.Framework;

public class CardModelTest
{
    [Test]
    public void Demo()
    {
        CardInfo cardInfo = new CardInfo();
        cardInfo.originPower = 1;
        CardModel cardModel = new CardModel(cardInfo);
        Assert.AreEqual(cardInfo.originPower, cardModel.GetOriginPower());
        Assert.AreEqual(cardInfo.originPower, cardModel.GetCurrentPower());
    }
}
