using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

public class WeatherAreaModelTest
{
    [Test]
    public void Constructor_InitializesEmptyAreas()
    {
        var model = new WeatherAreaModel();
        Assert.IsEmpty(model.wood.cardList);
        Assert.IsEmpty(model.brass.cardList);
        Assert.IsEmpty(model.percussion.cardList);
    }

    [Test]
    public void AddCard_SunFes_AddsToWood()
    {
        var model = new WeatherAreaModel();
        var card = TestUtil.MakeCard(ability: CardAbility.SunFes);

        var result = model.AddCard(card);

        Assert.AreEqual(CardLocation.WeatherCardArea, result.wood.cardList[0].cardLocation);
        Assert.IsEmpty(result.brass.cardList);
        Assert.IsEmpty(result.percussion.cardList);
    }

    [Test]
    public void AddCard_Daisangakushou_AddsToBrass()
    {
        var model = new WeatherAreaModel();
        var card = TestUtil.MakeCard(ability: CardAbility.Daisangakushou);

        var result = model.AddCard(card);

        Assert.AreEqual(CardLocation.WeatherCardArea, result.brass.cardList[0].cardLocation);
        Assert.IsEmpty(result.wood.cardList);
        Assert.IsEmpty(result.percussion.cardList);
    }

    [Test]
    public void AddCard_Drumstick_AddsToPercussion()
    {
        var model = new WeatherAreaModel();
        var card = TestUtil.MakeCard(ability: CardAbility.Drumstick);

        var result = model.AddCard(card);

        Assert.AreEqual(CardLocation.WeatherCardArea, result.percussion.cardList[0].cardLocation);
        Assert.IsEmpty(result.wood.cardList);
        Assert.IsEmpty(result.brass.cardList);
    }

    [Test]
    public void RemoveCard_RemovesFromWoodArea()
    {
        var card = TestUtil.MakeCard(ability: CardAbility.SunFes);
        var model = new WeatherAreaModel().AddCard(card);

        var result = model.RemoveCard(model.wood.cardList[0], out var removed);

        Assert.AreEqual(card, removed);
        Assert.IsEmpty(result.wood.cardList);
    }

    [Test]
    public void RemoveAllCard_ClearsAllAndReturnsList()
    {
        var c1 = TestUtil.MakeCard(ability: CardAbility.SunFes);
        var c2 = TestUtil.MakeCard(ability: CardAbility.Daisangakushou);
        var c3 = TestUtil.MakeCard(ability: CardAbility.Drumstick);
        var model = new WeatherAreaModel()
            .AddCard(c1)
            .AddCard(c2)
            .AddCard(c3);

        var result = model.RemoveAllCard(out var list);

        Assert.IsEmpty(result.wood.cardList);
        Assert.IsEmpty(result.brass.cardList);
        Assert.IsEmpty(result.percussion.cardList);
        Assert.AreEqual(3, list.Count);
        CollectionAssert.AreEquivalent(new[] { c1, c2, c3 }, list);
    }
}
