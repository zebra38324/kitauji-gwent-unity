using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

public class BattleRowAreaModelTest
{
    [Test]
    public void Constructor_InitializesEmptyState()
    {
        var row = new BattleRowAreaModel(CardBadgeType.Wood);
        Assert.IsEmpty(row.cardListModel.cardList);
        Assert.IsEmpty(row.hornCardListModel.cardList);
        Assert.AreEqual(0, row.GetCurrentPower());
    }

    [Test]
    public void AddCard_HornAbility_AddsToHornListAndUpdatesBuff()
    {
        var row = new BattleRowAreaModel(CardBadgeType.Wood);
        var hornCard = TestUtil.MakeCard(ability: CardAbility.HornUtil, cardType: CardType.Util);
        var updated = row.AddCard(hornCard);

        Assert.AreEqual(1, updated.hornCardListModel.cardList.Count);
        Assert.AreEqual(CardLocation.BattleArea, updated.hornCardListModel.cardList[0].cardLocation);
        // Horn buff 会为普通行牌添加 horn buff
        var normalCard = TestUtil.MakeCard(ability: CardAbility.None, originPower: 3);
        updated = updated.AddCard(normalCard);
        Assert.AreEqual(1, updated.cardListModel.cardList.Count);
        Assert.AreEqual(6, updated.GetCurrentPower());
    }

    [Test]
    public void AddCard_NormalAbility_AddsToCardList_WithKasaEffect()
    {
        var row = new BattleRowAreaModel(CardBadgeType.Wood);
        var mizoreCard = TestUtil.MakeCard(ability: CardAbility.None, originPower: 4, CardType.Normal, "铠冢霙").AddBuff(CardBuffType.Attack2, 1);
        var kasaCard = TestUtil.MakeCard(ability: CardAbility.Kasa, originPower: 4, CardType.Normal);
        var updated = row.AddCard(mizoreCard).AddCard(kasaCard);

        Assert.AreEqual(2, updated.cardListModel.cardList.Count);
        mizoreCard = updated.cardListModel.cardList[0];
        Assert.AreEqual(CardLocation.BattleArea, mizoreCard.cardLocation);
        Assert.AreEqual(4 + 5, mizoreCard.currentPower);
        Assert.AreEqual(4 + 9, updated.GetCurrentPower());
    }

    [Test]
    public void RemoveCard_RemovesAndClearsBuff()
    {
        var row = new BattleRowAreaModel(CardBadgeType.Wood)
            .AddCard(TestUtil.MakeCard(ability: CardAbility.None, originPower: 5).AddBuff(CardBuffType.Attack2, 1));
        var before = row.cardListModel.cardList[0];
        var result = row.RemoveCard(before, out var removed);

        Assert.IsFalse(result.cardListModel.cardList.Contains(before));
        Assert.AreEqual(removed.cardInfo.originPower, removed.currentPower);
    }

    [Test]
    public void RemoveAllCard_ClearsAllAndReturnsList()
    {
        var row = new BattleRowAreaModel(CardBadgeType.Brass)
            .AddCard(TestUtil.MakeCard(ability: CardAbility.None, originPower: 6).AddBuff(CardBuffType.Attack2, 1))
            .AddCard(TestUtil.MakeCard(ability: CardAbility.None, originPower: 6).AddBuff(CardBuffType.Attack4, 1))
            .AddCard(TestUtil.MakeCard(ability: CardAbility.HornUtil, originPower: 0, cardType: CardType.Util));
        var cleared = row.RemoveAllCard(out var list);

        Assert.IsEmpty(cleared.cardListModel.cardList);
        Assert.IsEmpty(cleared.hornCardListModel.cardList);
        Assert.AreEqual(3, list.Count);
        Assert.IsTrue(list.All(c => c.currentPower == c.cardInfo.originPower));
    }

    [Test]
    public void ApplyScorchWood_RemovesMaxPowerCards_WhenThresholdExceeded()
    {
        var row = new BattleRowAreaModel(CardBadgeType.Wood)
            .AddCard(TestUtil.MakeCard(ability: CardAbility.None, originPower: 6))
            .AddCard(TestUtil.MakeCard(ability: CardAbility.None, originPower: 7));
        var result = row.ApplyScorchWood();

        Assert.AreEqual(false, result.cardListModel.cardList[0].hasScorch);
        Assert.AreEqual(true, result.cardListModel.cardList[1].hasScorch);
    }

    [Test]
    public void ApplyScorchWood_RemovesMaxPowerCards_MaxIsHero()
    {
        var row = new BattleRowAreaModel(CardBadgeType.Wood)
            .AddCard(TestUtil.MakeCard(id: 1, ability: CardAbility.None, originPower: 6))
            .AddCard(TestUtil.MakeCard(id: 2, ability: CardAbility.None, originPower: 6))
            .AddCard(TestUtil.MakeCard(id: 3, ability: CardAbility.None, originPower: 7, cardType: CardType.Hero));
        var result = row.ApplyScorchWood();

        Assert.AreEqual(true, result.cardListModel.cardList[0].hasScorch);
        Assert.AreEqual(true, result.cardListModel.cardList[1].hasScorch);
        Assert.AreEqual(false, result.cardListModel.cardList[2].hasScorch);
    }

    [Test]
    public void ApplyScorchWood_NoRemoval_WhenBelowThreshold()
    {
        var row = new BattleRowAreaModel(CardBadgeType.Wood)
            .AddCard(TestUtil.MakeCard(ability: CardAbility.None, originPower: 5));
        var result = row.ApplyScorchWood();

        Assert.AreEqual(false, result.cardListModel.cardList[0].hasScorch);
    }

    [Test]
    public void SetWeatherBuff_AppliesAndRemovesCorrectly()
    {
        var row = new BattleRowAreaModel(CardBadgeType.Percussion)
            .AddCard(TestUtil.MakeCard(ability: CardAbility.None, originPower: 8));
        var withBuff = row.SetWeatherBuff(true);
        Assert.IsTrue(withBuff.cardListModel.cardList.All(c => c.currentPower == 1));
        Assert.AreEqual(1, withBuff.GetCurrentPower());

        var without = withBuff.SetWeatherBuff(false);
        Assert.IsTrue(without.cardListModel.cardList.All(c => c.currentPower == c.cardInfo.originPower));
        Assert.AreEqual(8, without.GetCurrentPower());
    }

    [Test]
    public void FindCard_InCardList_ReturnsCard()
    {
        // Arrange
        int cardId = 42;
        var card = TestUtil.MakeCard(id: cardId);
        var row = new BattleRowAreaModel(CardBadgeType.Wood)
            .AddCard(card);

        // Act
        var found = row.FindCard(cardId);

        // Assert
        Assert.IsNotNull(found);
        Assert.AreEqual(cardId, found.cardInfo.id);
    }

    [Test]
    public void FindCard_InHornList_ReturnsCard()
    {
        // Arrange
        int cardId = 99;
        var hornCard = TestUtil.MakeCard(ability: CardAbility.HornUtil,
            id: cardId);
        var row = new BattleRowAreaModel(CardBadgeType.Wood)
            .AddCard(hornCard);

        // Act
        var found = row.FindCard(cardId);

        // Assert
        Assert.IsNotNull(found);
        Assert.AreEqual(cardId, found.cardInfo.id);
    }

    [Test]
    public void ReplaceCard_ReplacesExistingCardInRow()
    {
        // Arrange
        var oldCard = TestUtil.MakeCard(originPower: 5, id: 1);
        var newCard = TestUtil.MakeCard(originPower: 8, id: 2);
        var model = new BattleRowAreaModel(CardBadgeType.Wood)
            .AddCard(oldCard);

        // Act
        var result = model.ReplaceCard(model.FindCard(oldCard.cardInfo.id), newCard);

        // Assert
        Assert.AreEqual(newCard.cardInfo.id, result.cardListModel.cardList[0].cardInfo.id,
                        "ReplaceCard 应该将新的卡牌放入同一个位置");
        Assert.AreEqual(1, result.cardListModel.cardList.Count,
                        "替换后，列表长度应保持不变");
    }

    [Test]
    public void ReplaceCard_AppliesBuffUpdateAfterReplacement()
    {
        // Arrange
        // 我们在行中加入一张具有“Morale”加成能力的卡，以及另一张普通卡
        var moraleCard = TestUtil.MakeCard(chineseName: "M", originPower: 3, ability: CardAbility.Morale, cardType: CardType.Normal);
        var normalOld = TestUtil.MakeCard(id: 4, originPower: 2);
        var normalNew = TestUtil.MakeCard(id: 5, originPower: 7);

        var model = new BattleRowAreaModel(CardBadgeType.Wood)
            .AddCard(moraleCard)
            .AddCard(normalOld);

        // 当有一个 Morale 卡时，普通卡的 currentPower 会被设置为 MoraleCount（1）* 原始 + any diffs
        var beforeReplace = model.cardListModel.cardList.Single(c => c.cardInfo.id == normalOld.cardInfo.id).currentPower;
        Assert.AreEqual(3, beforeReplace);

        // Act: 用一张点数更高的新卡替换旧卡
        var result = model.ReplaceCard(model.FindCard(normalOld.cardInfo.id), normalNew);

        // Assert
        // 替换后 newCard 应在列表里，并且通过 UpdateBuff，newCard.currentPower 会受到 MoraleBuff 的影响
        Assert.AreEqual(normalNew.cardInfo.id, result.cardListModel.cardList[1].cardInfo.id);
        var updated = result.cardListModel.cardList.Single(c => c.cardInfo.id.Equals(normalNew.cardInfo.id));
        Assert.AreEqual(8, updated.currentPower);
    }

    [Test]
    public void AddCard_SalutdAmour_NoRelated()
    {
        var normalCard = TestUtil.MakeCard(chineseName: "S", originPower: 3, ability: CardAbility.None, cardType: CardType.Normal);
        var salutdAmourCard = TestUtil.MakeCard(chineseName: "M", originPower: 3, ability: CardAbility.SalutdAmour, cardType: CardType.Normal);
        var model = new BattleRowAreaModel(CardBadgeType.Wood)
            .AddCard(normalCard)
            .AddCard(salutdAmourCard);
        Assert.AreEqual(6, model.GetCurrentPower());
    }

    [Test]
    public void AddCard_SalutdAmour_HasRelated()
    {
        var normalCard = TestUtil.MakeCard(chineseName: "川岛绿辉", originPower: 3, ability: CardAbility.None, cardType: CardType.Normal);
        var salutdAmourCard = TestUtil.MakeCard(chineseName: "M", originPower: 3, ability: CardAbility.SalutdAmour, cardType: CardType.Normal);
        var model = new BattleRowAreaModel(CardBadgeType.Wood)
            .AddCard(normalCard)
            .AddCard(salutdAmourCard);
        Assert.AreEqual(9, model.GetCurrentPower());
        Assert.AreEqual(3, model.cardListModel.cardList[0].currentPower);
        Assert.AreEqual(6, model.cardListModel.cardList[1].currentPower);
    }
}
