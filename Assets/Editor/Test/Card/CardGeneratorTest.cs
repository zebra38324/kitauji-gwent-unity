using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using System.Linq;

public class CardGeneratorTest
{
    private FieldInfo allInfoField;

    [SetUp]
    public void SetUp()
    {
        // Reset static allCardInfoList to our test data
        allInfoField = typeof(CardGenerator)
            .GetField("allCardInfoList", BindingFlags.Static | BindingFlags.NonPublic)!;

        // Create test CardInfo entries
        var infos = new List<CardInfo>
        {
            new CardInfo { infoId = 5, id = 0, originPower = 1, group = CardGroup.KumikoFirstYear },
            new CardInfo { infoId = 6, id = 0, originPower = 1, group = CardGroup.KumikoSecondYear }
        };
        // Assign to static field
        allInfoField.SetValue(null, infos.ToImmutableList());
    }

    [Test]
    public void Constructor_SetsSaltCorrectly()
    {
        var hostGen = new CardGenerator(isHost: true);
        Assert.AreEqual(1, hostGen.salt);

        var playerGen = new CardGenerator(isHost: false);
        Assert.AreEqual(2, playerGen.salt);
    }

    [Test]
    public void GetCardAndUpdateId_AssignsUniqueIdAndUpdatesGeneratorState()
    {
        var gen = new CardGenerator(isHost: true);
        // First draw
        var gen2 = gen.GetCardAndUpdateId(5, out var card1);
        Assert.AreEqual(5, card1.cardInfo.infoId);
        // id = cardId(1)*10 + salt(1) = 11
        Assert.AreEqual(11, card1.cardInfo.id);

        // Second draw
        var gen3 = gen2.GetCardAndUpdateId(5, out var card2);
        Assert.AreEqual(5, card2.cardInfo.infoId);
        // id = next cardId(2)*10 + salt(1) = 21
        Assert.AreEqual(21, card2.cardInfo.id);

        // Ensure generator state advanced
        Assert.AreNotSame(gen, gen2);
        Assert.AreNotSame(gen2, gen3);
    }

    [Test]
    public void GetCard_UsesProvidedIdWithoutChangingGenerator()
    {
        var gen = new CardGenerator(isHost: false);
        var card = gen.GetCard(6, 99);
        Assert.AreEqual(6, card.cardInfo.infoId);
        Assert.AreEqual(99, card.cardInfo.id);
        // subsequent calls produce same id if provided
        var card2 = gen.GetCard(6, 42);
        Assert.AreEqual(42, card2.cardInfo.id);
    }

    [Test]
    public void GetGroupCardList_ReturnsAllCardsInGroup()
    {
        var gen = new CardGenerator(isHost: false);
        var groupK1List = gen.GetGroupCardList(CardGroup.KumikoFirstYear);
        Assert.AreEqual(1, groupK1List.Count);
        Assert.IsTrue(groupK1List.All(c => c.cardInfo.group == CardGroup.KumikoFirstYear));

        var groupK2List = gen.GetGroupCardList(CardGroup.KumikoSecondYear);
        Assert.AreEqual(1, groupK2List.Count);
        Assert.IsTrue(groupK2List.All(c => c.cardInfo.group == CardGroup.KumikoSecondYear));
    }

    [Test]
    public void GetGroupCardList_ReturnsEmptyForUnknownGroup()
    {
        var gen = new CardGenerator(isHost: false);
        var noneList = gen.GetGroupCardList((CardGroup)999);
        Assert.IsEmpty(noneList);
    }
}
