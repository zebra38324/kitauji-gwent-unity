using NUnit.Framework;

public class CardModelTest
{
    [Test]
    public void Normal()
    {
        CardInfo cardInfo = new CardInfo();
        cardInfo.originPower = 1;
        CardModel cardModel = new CardModel(cardInfo);
        Assert.AreEqual(cardInfo.originPower, cardModel.GetCurrentPower());
        Assert.AreEqual(false, cardModel.IsDead());
    }

     [Test]
    public void NormalBuff()
    {
        CardInfo cardInfo = new CardInfo();
        cardInfo.originPower = 10;
        CardModel cardModel = new CardModel(cardInfo);

        cardModel.AddBuff(CardBuffType.Bond, 2);
        Assert.AreEqual(10 * 3, cardModel.GetCurrentPower());

        cardModel.AddBuff(CardBuffType.Horn, 1);
        Assert.AreEqual(10 * 4, cardModel.GetCurrentPower());

        cardModel.AddBuff(CardBuffType.Morale, 3);
        Assert.AreEqual(10 * 4 + 3, cardModel.GetCurrentPower());

        cardModel.AddBuff(CardBuffType.Attack2, 2);
        Assert.AreEqual(10 * 4 - 1, cardModel.GetCurrentPower());

        cardModel.AddBuff(CardBuffType.Attack4, 1);
        Assert.AreEqual(10 * 4 - 5, cardModel.GetCurrentPower());

        cardModel.RemoveBuff(CardBuffType.Bond);
        Assert.AreEqual(10 * 2 - 5, cardModel.GetCurrentPower());

        cardModel.RemoveBuff(CardBuffType.Attack2, 1);
        Assert.AreEqual(10 * 2 - 3, cardModel.GetCurrentPower());
        Assert.AreEqual(false, cardModel.IsDead());

        cardModel.AddBuff(CardBuffType.Weather, 1);
        Assert.AreEqual(true, cardModel.IsDead());
    }

    // 0点数角色牌死亡条件
    [Test]
    public void ZeroPowerDead()
    {
        CardInfo cardInfo = new CardInfo();
        cardInfo.originPower = 0;
        CardModel cardModel = new CardModel(cardInfo);
        Assert.AreEqual(false, cardModel.IsDead());

        cardModel.AddBuff(CardBuffType.Weather, 1);
        Assert.AreEqual(0, cardModel.GetCurrentPower());
        Assert.AreEqual(false, cardModel.IsDead());

        cardModel.AddBuff(CardBuffType.Morale, 1);
        Assert.AreEqual(1, cardModel.GetCurrentPower());
        Assert.AreEqual(false, cardModel.IsDead());

        cardModel.AddBuff(CardBuffType.Attack2, 1);
        Assert.AreEqual(true, cardModel.IsDead());
    }

    // 英雄牌
    [Test]
    public void HeroBuff()
    {
        CardInfo cardInfo = new CardInfo();
        cardInfo.originPower = 10;
        cardInfo.cardType = CardType.Hero;
        CardModel cardModel = new CardModel(cardInfo);

        cardModel.AddBuff(CardBuffType.Weather, 1);
        Assert.AreEqual(10, cardModel.GetCurrentPower());
    }

    [Test]
    public void RemoveNormalDebuff()
    {
        CardInfo cardInfo = new CardInfo();
        cardInfo.originPower = 10;
        CardModel cardModel = new CardModel(cardInfo);
        Assert.AreEqual(cardInfo.originPower, cardModel.GetCurrentPower());
        Assert.AreEqual(false, cardModel.IsDead());

        cardModel.AddBuff(CardBuffType.Bond, 2);
        Assert.AreEqual(10 * 3, cardModel.GetCurrentPower());

        cardModel.AddBuff(CardBuffType.Attack2, 1);
        Assert.AreEqual(10 * 3 - 2, cardModel.GetCurrentPower());

        cardModel.RemoveNormalDebuff();
        Assert.AreEqual(10 * 3, cardModel.GetCurrentPower());
    }

    [Test]
    public void RemoveAllBuff()
    {
        CardInfo cardInfo = new CardInfo();
        cardInfo.originPower = 10;
        CardModel cardModel = new CardModel(cardInfo);
        Assert.AreEqual(cardInfo.originPower, cardModel.GetCurrentPower());
        Assert.AreEqual(false, cardModel.IsDead());

        cardModel.AddBuff(CardBuffType.Bond, 2);
        Assert.AreEqual(10 * 3, cardModel.GetCurrentPower());

        cardModel.AddBuff(CardBuffType.Weather, 1);
        Assert.AreEqual(1 * 3, cardModel.GetCurrentPower());

        cardModel.RemoveAllBuff();
        Assert.AreEqual(10, cardModel.GetCurrentPower());
    }

    // 测试修改GetCardInfo()不会造成错误
    [Test]
    public void GetCardInfo()
    {
        CardInfo cardInfo = new CardInfo();
        cardInfo.originPower = 10;
        CardModel cardModel = new CardModel(cardInfo);

        CardInfo cardInfo1 = cardModel.GetCardInfo();
        cardInfo1.originPower = 20;
        CardInfo cardInfo2 = cardModel.GetCardInfo();
        Assert.AreEqual(cardInfo.originPower, cardInfo2.originPower);
    }

    [Test]
    public void SetBuff()
    {
        CardInfo cardInfo = new CardInfo();
        cardInfo.originPower = 10;
        CardModel cardModel = new CardModel(cardInfo);
        Assert.AreEqual(cardInfo.originPower, cardModel.GetCurrentPower());
        Assert.AreEqual(false, cardModel.IsDead());

        cardModel.AddBuff(CardBuffType.Bond, 2);
        Assert.AreEqual(10 * 3, cardModel.GetCurrentPower());

        cardModel.SetBuff(CardBuffType.Bond, 1);
        Assert.AreEqual(10 * 2, cardModel.GetCurrentPower());

        cardModel.SetBuff(CardBuffType.Horn, 1);
        Assert.AreEqual(10 * 3, cardModel.GetCurrentPower());
    }
}
