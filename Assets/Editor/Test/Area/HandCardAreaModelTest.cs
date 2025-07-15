using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;

public class HandCardAreaModelTest
{
    private FieldInfo allInfoField;
    private CardGenerator hostGen;
    private CardGenerator playerGen;

    [SetUp]
    public void SetUp()
    {
        // 重置并注入可控的 CardInfo 数据
        allInfoField = typeof(CardGenerator)
            .GetField("allCardInfoList", BindingFlags.Static | BindingFlags.NonPublic)!;
        // 注意有一张指挥牌
        var infos = new List<CardInfo>
        {
            new CardInfo { infoId = 1, id = 1, originPower = 1, cardType = CardType.Leader },
            new CardInfo { infoId = 2, id = 2, originPower = 1, cardType = CardType.Normal },
            new CardInfo { infoId = 3, id = 3, originPower = 1, cardType = CardType.Normal }
        };
        allInfoField.SetValue(null, infos.ToImmutableList());

        hostGen = new CardGenerator(isHost: true);
        playerGen = new CardGenerator(isHost: false);
    }

    [Test]
    public void Constructor_InitialState_ListsEmpty()
    {
        var model = new HandCardAreaModel(hostGen);
        Assert.IsEmpty(model.backupCardList);
        Assert.IsEmpty(model.handCardListModel.cardList);
        Assert.IsEmpty(model.initHandCardListModel.cardList);
        Assert.IsEmpty(model.initHandCardListModel.selectedCardList);
        Assert.IsEmpty(model.leaderCardListModel.cardList);
    }

    [Test]
    public void SetBackupCardInfoIdList_GeneratesCardsAndSeparatesLeader()
    {
        var infoIds = new List<int> { 1, 2 };
        var model = new HandCardAreaModel(hostGen, true)
            .SetBackupCardInfoIdList(infoIds);

        Assert.AreEqual(1, model.backupCardList.Count);
        Assert.IsTrue(model.backupCardList.All(c => c.cardInfo.infoId == 2));

        // Leader list should contain the leader card
        Assert.AreEqual(1, model.leaderCardListModel.cardList.Count);
        Assert.IsTrue(model.leaderCardListModel.cardList[0].cardInfo.infoId == 1);
        Assert.AreEqual(CardLocation.SelfLeaderCardArea, model.leaderCardListModel.cardList[0].cardLocation);
    }

    [Test]
    public void SetBackupCardInfoIdList_EnemyLeader()
    {
        var infoIds = new List<int> { 1 };
        var model = new HandCardAreaModel(hostGen, false)
            .SetBackupCardInfoIdList(infoIds);

        // Leader list should contain the leader card
        Assert.AreEqual(1, model.leaderCardListModel.cardList.Count);
        Assert.IsTrue(model.leaderCardListModel.cardList[0].cardInfo.infoId == 1);
        Assert.AreEqual(CardLocation.EnemyLeaderCardArea, model.leaderCardListModel.cardList[0].cardLocation);
    }

    [Test]
    public void SetBackupCardInfoIdList_DifferentId()
    {
        var infoIds = new List<int> { 2, 2 };
        var model = new HandCardAreaModel(hostGen, false)
            .SetBackupCardInfoIdList(infoIds);
        Assert.AreNotEqual(model.backupCardList[0].cardInfo.id, model.backupCardList[1].cardInfo.id);
    }

    [Test]
    public void DrawInitHandCard_WithInsufficientBackup_DrawsAllAndClearsBackup()
    {
        // 备选区只放 3 张
        var genModel = new HandCardAreaModel(hostGen)
            .SetBackupCardInfoIdList(new List<int> { 1, 2, 3 }, new List<int> { 10, 11, 12 });
        // 抽取 10 张
        var result = genModel.DrawInitHandCard();

        // 由于备选不足，抽完后备选应空
        Assert.IsEmpty(result.backupCardList);
        // initHandCardListModel 尚未加载入手牌，仅记录在 initHandCardListModel.cardList
        Assert.AreEqual(2, result.initHandCardListModel.cardList.Count);
    }

    [Test]
    public void ReDrawInitHandCard_ReplacesSelectedCountAndUpdatesBackup()
    {
        var model = new HandCardAreaModel(hostGen)
            .SetBackupCardInfoIdList(new List<int> { 2, 2, 2, 2 }, new List<int> { 10, 11, 12, 13 });
        // 手动设置 initHandCardListModel 并选中
        var initModel = model.DrawInitHandCard();
        initModel = initModel with {
            initHandCardListModel = initModel.initHandCardListModel.SelectCard(initModel.initHandCardListModel.cardList[0])
        };
        initModel = initModel with {
            initHandCardListModel = initModel.initHandCardListModel.SelectCard(initModel.initHandCardListModel.cardList[1])
        };
        Assert.IsTrue(initModel.initHandCardListModel.cardList[0].isSelected);
        Assert.IsTrue(initModel.initHandCardListModel.cardList[1].isSelected);
        Assert.IsFalse(initModel.initHandCardListModel.cardList[2].isSelected);
        Assert.IsFalse(initModel.initHandCardListModel.cardList[3].isSelected);
        Assert.IsFalse(initModel.hasReDrawInitHandCard);

        var result = initModel.ReDrawInitHandCard();
        Assert.AreEqual(4, result.initHandCardListModel.cardList.Count);
        Assert.AreEqual(0, result.backupCardList.Count);
        Assert.IsFalse(result.initHandCardListModel.cardList[0].isSelected);
        Assert.IsFalse(result.initHandCardListModel.cardList[1].isSelected);
        Assert.IsFalse(result.initHandCardListModel.cardList[2].isSelected);
        Assert.IsFalse(result.initHandCardListModel.cardList[3].isSelected);
        Assert.IsTrue(result.hasReDrawInitHandCard);
    }

    [Test]
    public void LoadFromInitHandCard_MovesInitCardsToHand()
    {
        var model = new HandCardAreaModel(hostGen)
            .SetBackupCardInfoIdList(new List<int> { 2 }, new List<int> { 10 })
            .DrawInitHandCard();

        var result = model.LoadHandCard();
        // init 清空
        Assert.IsEmpty(result.initHandCardListModel.cardList);
        // 手牌区获得一张卡
        Assert.AreEqual(1, result.handCardListModel.cardList.Count);
        Assert.AreEqual(2, result.handCardListModel.cardList[0].cardInfo.infoId);
    }

    [Test]
    public void DrawHandCardsRandom_MovesFromBackupToHand()
    {
        var model = new HandCardAreaModel(playerGen)
            .SetBackupCardInfoIdList(new List<int> { 2, 2 }, new List<int> { 10, 11 });
        var result = model.DrawHandCardsRandom(2, out var idList);

        Assert.AreEqual(2, result.handCardListModel.cardList.Count);
        Assert.AreEqual(0, result.backupCardList.Count);
        CollectionAssert.AreEquivalent(new[] { 10, 11 }, idList);
    }

    [Test]
    public void DrawHandCardsWithoutRandom_MovesFromBackupToHand()
    {
        var model = new HandCardAreaModel(playerGen)
            .SetBackupCardInfoIdList(new List<int> { 2, 2 }, new List<int> { 10, 11 });
        var idList = new List<int> { 10 };
        var result = model.DrawHandCardsWithoutRandom(idList);

        Assert.AreEqual(1, result.handCardListModel.cardList.Count);
        Assert.AreEqual(10, result.handCardListModel.cardList[0].cardInfo.id);
        Assert.AreEqual(1, result.backupCardList.Count);
        Assert.AreEqual(11, result.backupCardList[0].cardInfo.id);
    }

    [Test]
    public void RemoveAndGetMuster_FromHandAndBackup_RemovesAndReturnsCorrectCards()
    {
        var model = new HandCardAreaModel(hostGen);
        // Arrange: 各准备一张 musterType="A" 的卡，分别放入手牌区和备选区
        var handCard = TestUtil.MakeCard(
            ability: CardAbility.None,
            originPower: 1,
            cardType: CardType.Normal,
            chineseName: "H1",
            id: 101,
            musterType: "A"
        );
        model = model with {
            handCardListModel = model.handCardListModel.AddCard(handCard) as HandCardListModel
        };

        var backupCard = TestUtil.MakeCard(
            ability: CardAbility.None,
            originPower: 1,
            cardType: CardType.Normal,
            chineseName: "B1",
            id: 102,
            musterType: "A"
        );
        model = model with {
            backupCardList = model.backupCardList.Add(backupCard)
        };

        // Act
        var result = model.RemoveAndGetMuster("A", out var musterCards);

        // Assert
        CollectionAssert.AreEquivalent(
            new[] { handCard, backupCard },
            musterCards,
            "应返回所有 musterType 为 A 的卡牌"
        );
        Assert.AreEqual(0, result.handCardListModel.cardList.Count);
        Assert.AreEqual(0, result.backupCardList.Count);
    }

    [Test]
    public void RemoveAndGetMuster_NoMatchingMusterType_ReturnsEmptyAndNoChange()
    {
        var model = new HandCardAreaModel(hostGen);
        // Arrange: 准备两张不同 musterType 的卡
        var handCard = TestUtil.MakeCard(
            chineseName: "H2",
            id: 201,
            musterType: "X"
        );
        model = model with {
            handCardListModel = model.handCardListModel.AddCard(handCard) as HandCardListModel
        };

        var backupCard = TestUtil.MakeCard(
            chineseName: "B2",
            id: 202,
            musterType: "Y"
        );
        model = model with {
            backupCardList = model.backupCardList.Add(backupCard)
        };

        // Act
        var result = model.RemoveAndGetMuster("Z", out var musterCards);

        // Assert
        Assert.IsEmpty(
            musterCards,
            "没有匹配的 musterType 时，应返回空列表"
        );
        Assert.AreSame(
            model, result,
            "无卡可取时，应返回同一实例"
        );
        Assert.AreEqual(1, result.handCardListModel.cardList.Count);
        Assert.AreEqual(1, result.backupCardList.Count);
    }

    [Test]
    public void ReplaceHandAndBackupCard()
    {
        var infoIds = new List<int> { 1 };
        infoIds.AddRange(Enumerable.Repeat(2, 11));
        var model = new HandCardAreaModel(hostGen, false)
            .SetBackupCardInfoIdList(infoIds);

        var handInfoIds = Enumerable.Repeat(3, 8).ToList();
        var handIds = Enumerable.Range(8, 8).Select(x => x * 10 + 3).ToList();
        var backupInfoIds = Enumerable.Repeat(3, 3).ToList();
        var backupIds = Enumerable.Range(20, 3).Select(x => x * 10 + 3).ToList();
        model = model.ReplaceHandAndBackupCard(handInfoIds, handIds, backupInfoIds, backupIds);

        Assert.AreEqual(1, model.leaderCardListModel.cardList.Count);
        Assert.IsTrue(model.leaderCardListModel.cardList[0].cardInfo.infoId == 1);
        foreach (var card in model.handCardListModel.cardList) {
            Assert.AreEqual(3, card.cardInfo.infoId);
        }
        foreach (var card in model.backupCardList) {
            Assert.AreEqual(3, card.cardInfo.infoId);
        }
    }
}
