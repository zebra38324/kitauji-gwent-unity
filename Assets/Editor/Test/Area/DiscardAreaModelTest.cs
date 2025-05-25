using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

public class DiscardAreaModelTest
{
    [Test]
    public void Constructor_InitialState_GetShowListEmpty()
    {
        var model = new DiscardAreaModel();
        var listAll = model.GetShowList(onlyNormal: false);
        var listNormal = model.GetShowList(onlyNormal: true);
        Assert.AreEqual(3, listAll.Count);
        Assert.AreEqual(3, listNormal.Count);
        foreach (var row in listAll) {
            Assert.IsEmpty(row);
        }
        foreach (var row in listNormal) {
            Assert.IsEmpty(row);
        }
    }

    [Test]
    public void AddCard_SetsLocationAndAddsToList()
    {
        var card = TestUtil.MakeCard();
        var model = new DiscardAreaModel()
            .AddCard(card);
        // New instance returned
        Assert.AreEqual(CardLocation.DiscardArea, model.cardListModel.cardList.First().cardLocation);
        // GetShowList should include this card in first row
        var rows = model.GetShowList(false);
        Assert.AreEqual(1, rows[0].Count);
        Assert.AreEqual(model.cardListModel.cardList.First(), rows[0][0]);
    }

    [Test]
    public void RemoveCard_RemovesAndOutputsRemovedCard()
    {
        var card = TestUtil.MakeCard();
        var added = new DiscardAreaModel().AddCard(card);
        var removedModel = added.RemoveCard(added.cardListModel.cardList[0], out var removed);
        Assert.AreEqual(card, removed);
        Assert.IsEmpty(removedModel.cardListModel.cardList);
    }

    [Test]
    public void GetShowList_DistributeMoreThanRowMax()
    {
        var cards = Enumerable.Range(1, 14)
            .Select(i => TestUtil.MakeCard(originPower: i))
            .ToList();
        var model = new DiscardAreaModel();
        foreach (var c in cards) {
            model = model.AddCard(c);
        }
        var rows = model.GetShowList(false);
        Assert.AreEqual(3, rows.Count);
        Assert.AreEqual(10, rows[0].Count);
        Assert.AreEqual(4, rows[1].Count);
        Assert.AreEqual(0, rows[2].Count);
        // Verify all cards present
        var all = rows.SelectMany(r => r).ToList();
        Assert.AreEqual(14, all.Count);
    }

    [Test]
    public void GetShowList_DistributeMoreThanAllRowMax()
    {
        var cards = Enumerable.Range(1, 35)
            .Select(i => TestUtil.MakeCard(originPower: i))
            .ToList();
        var model = new DiscardAreaModel();
        foreach (var c in cards) {
            model = model.AddCard(c);
        }
        var rows = model.GetShowList(false);
        Assert.AreEqual(3, rows.Count);
        Assert.AreEqual(12, rows[0].Count);
        Assert.AreEqual(12, rows[1].Count);
        Assert.AreEqual(11, rows[2].Count);
        // Verify all cards present
        var all = rows.SelectMany(r => r).ToList();
        Assert.AreEqual(35, all.Count);
    }

    [Test]
    public void GetShowList_OnlyNormalFiltersHeroes()
    {
        var normal = Enumerable.Range(1, 5).Select(i => TestUtil.MakeCard(originPower: i, cardType: CardType.Normal));
        var heroes = Enumerable.Range(6, 4).Select(i => TestUtil.MakeCard(originPower: i, cardType: CardType.Hero));
        var model = new DiscardAreaModel();
        foreach (var c in normal.Concat(heroes)) {
            model = model.AddCard(c);
        }
        var rowsNormal = model.GetShowList(true);
        var all = rowsNormal.SelectMany(r => r).ToList();
        // only normals
        Assert.AreEqual(5, all.Count);
    }
}
