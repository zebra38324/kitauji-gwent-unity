using NUnit.Framework;

public class CardModelTest
{
    [Test]
    public void Normal()
    {
        CardInfo cardInfo = new CardInfo();
        cardInfo.originPower = 1;
        CardModel cardModel = new CardModel(cardInfo);
        Assert.AreEqual(cardInfo.originPower, cardModel.currentPower);
        Assert.AreEqual(false, cardModel.IsDead());
    }

     [Test]
    public void NormalBuff()
    {
        CardInfo cardInfo = new CardInfo();
        cardInfo.originPower = 10;
        CardModel cardModel = new CardModel(cardInfo);

        cardModel.AddBuff(CardBuffType.Bond, 2);
        Assert.AreEqual(10 * 3, cardModel.currentPower);

        cardModel.AddBuff(CardBuffType.Horn, 1);
        Assert.AreEqual(10 * 4, cardModel.currentPower);

        cardModel.AddBuff(CardBuffType.Morale, 3);
        Assert.AreEqual(10 * 4 + 3, cardModel.currentPower);

        cardModel.AddBuff(CardBuffType.Attack2, 2);
        Assert.AreEqual(10 * 4 - 1, cardModel.currentPower);

        cardModel.AddBuff(CardBuffType.Attack4, 1);
        Assert.AreEqual(10 * 4 - 5, cardModel.currentPower);

        cardModel.RemoveBuff(CardBuffType.Bond);
        Assert.AreEqual(10 * 2 - 5, cardModel.currentPower);

        cardModel.RemoveBuff(CardBuffType.Attack2, 1);
        Assert.AreEqual(10 * 2 - 3, cardModel.currentPower);
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
        Assert.AreEqual(0, cardModel.currentPower);
        Assert.AreEqual(false, cardModel.IsDead());

        cardModel.AddBuff(CardBuffType.Morale, 1);
        Assert.AreEqual(1, cardModel.currentPower);
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
        Assert.AreEqual(10, cardModel.currentPower);
    }

    [Test]
    public void RemoveNormalDebuff()
    {
        CardInfo cardInfo = new CardInfo();
        cardInfo.originPower = 10;
        CardModel cardModel = new CardModel(cardInfo);
        Assert.AreEqual(cardInfo.originPower, cardModel.currentPower);
        Assert.AreEqual(false, cardModel.IsDead());

        cardModel.AddBuff(CardBuffType.Bond, 2);
        Assert.AreEqual(10 * 3, cardModel.currentPower);

        cardModel.AddBuff(CardBuffType.Attack2, 1);
        Assert.AreEqual(10 * 3 - 2, cardModel.currentPower);

        cardModel.RemoveNormalDebuff();
        Assert.AreEqual(10 * 3, cardModel.currentPower);
    }

    [Test]
    public void RemoveAllBuff()
    {
        CardInfo cardInfo = new CardInfo();
        cardInfo.originPower = 10;
        CardModel cardModel = new CardModel(cardInfo);
        Assert.AreEqual(cardInfo.originPower, cardModel.currentPower);
        Assert.AreEqual(false, cardModel.IsDead());

        cardModel.AddBuff(CardBuffType.Bond, 2);
        Assert.AreEqual(10 * 3, cardModel.currentPower);

        cardModel.AddBuff(CardBuffType.Weather, 1);
        Assert.AreEqual(1 * 3, cardModel.currentPower);

        cardModel.RemoveAllBuff();
        Assert.AreEqual(10, cardModel.currentPower);
    }

    // 测试修改cardInfo不会造成错误
    [Test]
    public void GetCardInfo()
    {
        CardInfo cardInfo = new CardInfo();
        cardInfo.originPower = 10;
        CardModel cardModel = new CardModel(cardInfo);

        CardInfo cardInfo1 = cardModel.cardInfo;
        cardInfo1.originPower = 20;
        CardInfo cardInfo2 = cardModel.cardInfo;
        Assert.AreEqual(cardInfo.originPower, cardInfo2.originPower);
    }

    [Test]
    public void SetBuff()
    {
        CardInfo cardInfo = new CardInfo();
        cardInfo.originPower = 10;
        CardModel cardModel = new CardModel(cardInfo);
        Assert.AreEqual(cardInfo.originPower, cardModel.currentPower);
        Assert.AreEqual(false, cardModel.IsDead());

        cardModel.AddBuff(CardBuffType.Bond, 2);
        Assert.AreEqual(10 * 3, cardModel.currentPower);

        cardModel.SetBuff(CardBuffType.Bond, 1);
        Assert.AreEqual(10 * 2, cardModel.currentPower);

        cardModel.SetBuff(CardBuffType.Horn, 1);
        Assert.AreEqual(10 * 3, cardModel.currentPower);
    }
}
