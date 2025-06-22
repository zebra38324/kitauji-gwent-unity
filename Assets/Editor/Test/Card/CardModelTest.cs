using NUnit.Framework;
using System.Collections.Immutable;

public class CardModelTest
{
    [Test]
    public void Constructor_SetsOriginPowerAndZeroBuffs()
    {
        var card = TestUtil.MakeCard(originPower: 7);

        Assert.AreEqual(7, card.currentPower, "初始 currentPower 应等于 originPower");
        Assert.IsFalse(card.IsDead(), "刚创建的卡牌不应被判定为死");
    }

    [Test]
    public void IsDead_OriginZero_NotDead()
    {
        // originPower = 0 时，currentPower 也应为 0
        var cardZero = TestUtil.MakeCard(originPower: 0, cardType: CardType.Normal);

        Assert.AreEqual(0, cardZero.currentPower, "originPower=0 时，currentPower 应为 0");
        Assert.IsFalse(cardZero.IsDead(), "当 originPower=0 时，IsDead 应返回 false");
    }

    [Test]
    public void ChangeCardLocation_SameLocation_ReturnsSameInstance()
    {
        var card = TestUtil.MakeCard(originPower: 10);
        var same = card.ChangeCardLocation(CardLocation.None);
        Assert.AreSame(card, same);
    }

    [Test]
    public void ChangeCardLocation_HandArea_SetsPlaySelectType()
    {
        var card = TestUtil.MakeCard(originPower: 10);
        var moved = card.ChangeCardLocation(CardLocation.HandArea);
        Assert.AreEqual(CardLocation.HandArea, moved.cardLocation);
        Assert.AreEqual(CardSelectType.PlayCard, moved.cardSelectType);
    }

    [Test]
    public void ChangeCardLocation_InitHandArea_SetsReDrawSelectType()
    {
        var card = TestUtil.MakeCard(originPower: 10);
        var moved = card.ChangeCardLocation(CardLocation.InitHandArea);
        Assert.AreEqual(CardLocation.InitHandArea, moved.cardLocation);
        Assert.AreEqual(CardSelectType.ReDrawHandCard, moved.cardSelectType);
    }

    [Test]
    public void ChangeCardSelectType_TogglesType()
    {
        var card = TestUtil.MakeCard(originPower: 10);
        var t1 = card.ChangeCardSelectType(CardSelectType.PlayCard);
        Assert.AreEqual(CardSelectType.PlayCard, t1.cardSelectType);
        var t2 = t1.ChangeCardSelectType(CardSelectType.PlayCard);
        Assert.AreSame(t1, t2);
    }

    [Test]
    public void ChangeSelectStatus_TogglesIsSelected()
    {
        var card = TestUtil.MakeCard(originPower: 10);
        var c1 = card.ChangeSelectStatus(true);
        Assert.IsTrue(c1.isSelected);
        var c2 = c1.ChangeSelectStatus(true);
        Assert.AreSame(c1, c2);
    }

    [Test]
    public void AddBuff_OnNormal_IncreasesBuffAndPower()
    {
        var card = TestUtil.MakeCard(originPower: 5);
        var buffed = card.AddBuff(CardBuffType.Morale, 3);
        // origin 5 + morale*1 = 8
        Assert.AreEqual(8, buffed.currentPower);
    }

    [Test]
    public void AddBuff_OnNonNormal_DoesNothing()
    {
        var card = TestUtil.MakeCard(cardType: CardType.Hero);
        var result = card.AddBuff(CardBuffType.Morale, 10);
        Assert.AreSame(card, result);
    }

    [Test]
    public void RemoveBuff_PartialAndFull()
    {
        var card = TestUtil.MakeCard(originPower: 5)
            .AddBuff(CardBuffType.Morale, 5);
        var partially = card.RemoveBuff(CardBuffType.Morale, 2);
        // origin 5 + (5-2)=3 => 8
        Assert.AreEqual(8, partially.currentPower);

        var cleared = card.RemoveBuff(CardBuffType.Morale);
        // buff cleared => back to origin 5
        Assert.AreEqual(5, cleared.currentPower);
    }

    [Test]
    public void RemoveNormalDebuff_ClearsOnlyAttackDebuffs()
    {
        var card = TestUtil.MakeCard(originPower: 10)
            .AddBuff(CardBuffType.Attack2, 2)
            .AddBuff(CardBuffType.Attack4, 1)
            .AddBuff(CardBuffType.PressureMinus, 1)
            .AddBuff(CardBuffType.PowerFirst, 1)
            .AddBuff(CardBuffType.Morale, 2);
        var cleaned = card.RemoveNormalDebuff();
        Assert.AreEqual(10 + 2, cleaned.currentPower);
    }

    [Test]
    public void RemoveAllBuff_ResetsAllBuffs()
    {
        var card = TestUtil.MakeCard(originPower: 8)
            .AddBuff(CardBuffType.Morale, 2)
            .AddBuff(CardBuffType.Horn, 1);
        var reset = card.RemoveAllBuff();
        // buffs cleared => back to origin, no multipliers
        Assert.AreEqual(8, reset.currentPower);
    }

    [Test]
    public void SetBuff_SetsExactBuffValue()
    {
        var card = TestUtil.MakeCard(originPower: 4)
            .SetBuff(CardBuffType.Bond, 2);
        // times = 2(Bond)+0(Horn)+1 =3; 4*3=12
        Assert.AreEqual(12, card.currentPower);
    }

    [Test]
    public void WeatherBuff_LimitsPowerToOne()
    {
        var card = TestUtil.MakeCard(originPower: 7)
            .SetBuff(CardBuffType.Weather, 1);
        Assert.AreEqual(1, card.currentPower);
    }

    [Test]
    public void IsDead_WhenPowerZeroOrBelowAndReduced()
    {
        var card = TestUtil.MakeCard(originPower: 3)
            .SetBuff(CardBuffType.Attack4, 1)
            .RemoveBuff(CardBuffType.Morale, 0); // no morale buff, but attack4=1 => diff = -4
        // origin 3, times=1 =>3, diff=-4 => -1
        Assert.IsTrue(card.currentPower <= 0);
        Assert.IsTrue(card.IsDead());
    }

    [Test]
    public void SetScorch_SetsScorchFlag()
    {
        var card = TestUtil.MakeCard(originPower: 5);
        var scorched = card.SetScorch();
        Assert.IsTrue(scorched.hasScorch);
        Assert.IsTrue(scorched.IsDead());
        var result = card.RemoveAllBuff();
        Assert.IsFalse(result.hasScorch);
        Assert.IsFalse(result.IsDead());
    }

    [Test]
    public void SetUnderDefend()
    {
        var card = TestUtil.MakeCard(originPower: 5);
        card = card.SetUnderDefend(true);
        card = card.SetScorch();
        Assert.IsFalse(card.IsDead());
        card = card.RemoveAllBuff();
        card = card.SetScorch();
        Assert.IsTrue(card.IsDead());
    }

    [Test]
    public void CurrentPowerNotNegtive()
    {
        var card = TestUtil.MakeCard(originPower: 3);
        card = card.AddBuff(CardBuffType.Attack4, 1);
        Assert.IsTrue(card.IsDead());
        Assert.AreEqual(0, card.currentPower);
    }
}
