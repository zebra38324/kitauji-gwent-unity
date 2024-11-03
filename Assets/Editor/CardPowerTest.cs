using NUnit.Framework;

public class CardPowerTest
{
    [Test]
    public void Normal()
    {
        CardInfo cardInfo = new CardInfo();
        cardInfo.originPower = 1;
        CardPower cardPower = new CardPower(cardInfo);
        Assert.AreEqual(cardInfo.originPower, cardPower.GetCurrentPower());
        Assert.AreEqual(false, cardPower.IsDead());
    }

     [Test]
    public void NormalBuff()
    {
        CardInfo cardInfo = new CardInfo();
        cardInfo.originPower = 10;
        CardPower cardPower = new CardPower(cardInfo);

        cardPower.AddBuff(CardBuffType.Bond, 2);
        Assert.AreEqual(10 * 3, cardPower.GetCurrentPower());

        cardPower.AddBuff(CardBuffType.Horn, 1);
        Assert.AreEqual(10 * 4, cardPower.GetCurrentPower());

        cardPower.AddBuff(CardBuffType.Morale, 3);
        Assert.AreEqual(10 * 4 + 3, cardPower.GetCurrentPower());

        cardPower.AddBuff(CardBuffType.Attack2, 2);
        Assert.AreEqual(10 * 4 - 1, cardPower.GetCurrentPower());

        cardPower.AddBuff(CardBuffType.Attack4, 1);
        Assert.AreEqual(10 * 4 - 5, cardPower.GetCurrentPower());

        cardPower.RemoveBuff(CardBuffType.Bond);
        Assert.AreEqual(10 * 2 - 5, cardPower.GetCurrentPower());

        cardPower.RemoveBuff(CardBuffType.Attack2, 1);
        Assert.AreEqual(10 * 2 - 3, cardPower.GetCurrentPower());
        Assert.AreEqual(false, cardPower.IsDead());

        cardPower.AddBuff(CardBuffType.Weather, 1);
        Assert.AreEqual(true, cardPower.IsDead());
    }

    // 0点数角色牌死亡条件
    [Test]
    public void ZeroPowerDead()
    {
        CardInfo cardInfo = new CardInfo();
        cardInfo.originPower = 0;
        CardPower cardPower = new CardPower(cardInfo);
        Assert.AreEqual(false, cardPower.IsDead());

        cardPower.AddBuff(CardBuffType.Weather, 1);
        Assert.AreEqual(0, cardPower.GetCurrentPower());
        Assert.AreEqual(false, cardPower.IsDead());

        cardPower.AddBuff(CardBuffType.Morale, 1);
        Assert.AreEqual(1, cardPower.GetCurrentPower());
        Assert.AreEqual(false, cardPower.IsDead());

        cardPower.AddBuff(CardBuffType.Attack2, 1);
        Assert.AreEqual(true, cardPower.IsDead());
    }

    // 英雄牌
    [Test]
    public void HeroBuff()
    {
        CardInfo cardInfo = new CardInfo();
        cardInfo.originPower = 10;
        cardInfo.cardType = CardType.Hero;
        CardPower cardPower = new CardPower(cardInfo);

        cardPower.AddBuff(CardBuffType.Weather, 1);
        Assert.AreEqual(10, cardPower.GetCurrentPower());
    }
}
