using NUnit.Framework;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Collections.Immutable;

public class SinglePlayerAreaModelTest
{
    private FieldInfo allInfoField;
    private CardGenerator hostGen;

    [SetUp]
    public void SetUp()
    {
        // 初始化静态卡牌信息列表，避免资源依赖
        allInfoField = typeof(CardGenerator)
            .GetField("allCardInfoList", BindingFlags.Static | BindingFlags.NonPublic)!;
        var infos = new List<CardInfo>
        {
            new CardInfo { infoId = 1, id = 1, originPower = 5, cardType = CardType.Normal, badgeType = CardBadgeType.Wood },
            new CardInfo { infoId = 2, id = 2, originPower = 3, cardType = CardType.Normal, badgeType = CardBadgeType.Brass },
            new CardInfo { infoId = 3, id = 3, originPower = 2, cardType = CardType.Normal, badgeType = CardBadgeType.Percussion }
        };
        allInfoField.SetValue(null, infos.ToImmutableList());

        hostGen = new CardGenerator(isHost: true);
    }

    [Test]
    public void Constructor_InitializesBattleRowsAndHandArea()
    {
        var model = new SinglePlayerAreaModel(hostGen);
        // 应创建三行对战区
        Assert.AreEqual(3, model.battleRowAreaList.Count);
        // 手牌区初始化
        Assert.IsNotNull(model.handCardAreaModel);
        // 弃牌区初始化
        Assert.IsNotNull(model.discardAreaModel);
        Assert.AreEqual(0, model.GetMaxNormalPower());
    }

    [Test]
    public void AddBattleAreaCard_NormalAbility_AddsToCorrectRow()
    {
        var card = TestUtil.MakeCard(ability: CardAbility.None,
            originPower: 4,
            cardType: CardType.Normal,
            chineseName: "N",
            id: 10,
            musterType: "",
            badgeType: CardBadgeType.Wood);

        var model = new SinglePlayerAreaModel(hostGen)
            .AddBattleAreaCard(card);

        var row = model.battleRowAreaList[(int)CardBadgeType.Wood];
        Assert.AreEqual(card.cardInfo, row.cardListModel.cardList[0].cardInfo);
        Assert.IsEmpty(row.hornCardListModel.cardList);
    }

    [Test]
    public void AddBattleAreaCard_HornBrassAbility_AddsToBrassHornList()
    {
        var card1 = TestUtil.MakeCard(ability: CardAbility.HornBrass,
            originPower: 6,
            cardType: CardType.Util,
            chineseName: "H",
            id: 20,
            musterType: "",
            badgeType: CardBadgeType.Brass);
        var card2 = TestUtil.MakeCard(originPower: 6,
            cardType: CardType.Normal,
            chineseName: "H2",
            id: 21,
            musterType: "",
            badgeType: CardBadgeType.Brass);
        var card3 = TestUtil.MakeCard(originPower: 6,
            cardType: CardType.Normal,
            chineseName: "H3",
            id: 22,
            musterType: "",
            badgeType: CardBadgeType.Wood);

        var model = new SinglePlayerAreaModel(hostGen)
            .AddBattleAreaCard(card1)
            .AddBattleAreaCard(card2)
            .AddBattleAreaCard(card3);

        // HornBrass 会将卡再放入 Brass 行的 hornCardList
        var brassRow = model.battleRowAreaList[(int)CardBadgeType.Brass];
        Assert.AreEqual(card1.cardInfo, brassRow.hornCardListModel.cardList[0].cardInfo);
        Assert.AreEqual(6, model.battleRowAreaList[(int)CardBadgeType.Wood].GetCurrentPower());
        Assert.AreEqual(12, model.battleRowAreaList[(int)CardBadgeType.Brass].GetCurrentPower());
    }

    [Test]
    public void GetCurrentPower_SumsAllRows()
    {
        var c1 = TestUtil.MakeCard(originPower: 5,
            id: 101,
            badgeType: CardBadgeType.Wood);
        var c2 = TestUtil.MakeCard(originPower: 3,
            id: 102,
            badgeType: CardBadgeType.Wood);

        var model = new SinglePlayerAreaModel(hostGen)
            .AddBattleAreaCard(c1)
            .AddBattleAreaCard(c2);

        Assert.AreEqual(5 + 3, model.GetCurrentPower());
    }

    [Test]
    public void AddBattleAreaCard_TunningAbility()
    {
        var c1 = TestUtil.MakeCard(originPower: 5,
            id: 101,
            badgeType: CardBadgeType.Brass);
        c1 = c1.AddBuff(CardBuffType.Attack2, 1);
        var c2 = TestUtil.MakeCard(originPower: 5,
            id: 102,
            ability: CardAbility.Morale,
            badgeType: CardBadgeType.Brass);
        c2 = c2.AddBuff(CardBuffType.Attack2, 1);
        var c3 = TestUtil.MakeCard(originPower: 5,
            id: 103,
            ability: CardAbility.Tunning,
            badgeType: CardBadgeType.Wood);

        var model = new SinglePlayerAreaModel(hostGen)
            .AddBattleAreaCard(c1)
            .AddBattleAreaCard(c2);
        Assert.AreEqual(3 + 3 + 1, model.GetCurrentPower());
        model = model.AddBattleAreaCard(c3);
        Assert.AreEqual(5 + 5 + 1 + 5, model.GetCurrentPower());
    }

    [Test]
    public void AddBattleAreaCard_BondAbility_AppliesBondBuff()
    {
        // 两张同类型 Bond 卡
        var card1 = TestUtil.MakeCard(ability: CardAbility.Bond,
            originPower: 2,
            cardType: CardType.Normal,
            chineseName: "B1",
            id: 30,
            musterType: "",
            bondType: "X",
            badgeType: CardBadgeType.Wood);
        var card2 = TestUtil.MakeCard(ability: CardAbility.Bond,
            originPower: 2,
            cardType: CardType.Normal,
            chineseName: "B2",
            id: 31,
            musterType: "",
            bondType: "X",
            badgeType: CardBadgeType.Brass);
        var card3 = TestUtil.MakeCard(ability: CardAbility.Bond,
            originPower: 2,
            cardType: CardType.Normal,
            chineseName: "B3",
            id: 32,
            musterType: "",
            bondType: "Y",
            badgeType: CardBadgeType.Brass);
        var card4 = TestUtil.MakeCard(ability: CardAbility.Bond,
            originPower: 2,
            cardType: CardType.Normal,
            chineseName: "B4",
            id: 33,
            musterType: "",
            bondType: "X",
            badgeType: CardBadgeType.Brass);

        var model = new SinglePlayerAreaModel(hostGen)
            .AddBattleAreaCard(card1)
            .AddBattleAreaCard(card2)
            .AddBattleAreaCard(card3)
            .AddBattleAreaCard(card4);

        // 第二次添加后，两张卡都应获得 Bond buff
        var r1 = model.battleRowAreaList[(int)CardBadgeType.Wood].cardListModel.cardList[0];
        var r2 = model.battleRowAreaList[(int)CardBadgeType.Brass].cardListModel.cardList[0];
        var r3 = model.battleRowAreaList[(int)CardBadgeType.Brass].cardListModel.cardList[1];
        var r4 = model.battleRowAreaList[(int)CardBadgeType.Brass].cardListModel.cardList[2];
        Assert.AreEqual(2 * 3, r1.currentPower);
        Assert.AreEqual(2 * 3, r2.currentPower);
        Assert.AreEqual(2, r3.currentPower);
        Assert.AreEqual(2 * 3, r4.currentPower);
    }

    [Test]
    public void AddBattleAreaCard_MusterAbility_AppliesMusterAndMovesCards()
    {
        // Arrange: 准备两张手牌
        var musterCard1 = TestUtil.MakeCard(ability: CardAbility.Muster,
            originPower: 4,
            cardType: CardType.Normal,
            chineseName: "M1",
            id: 40,
            musterType: "M1",
            badgeType: CardBadgeType.Wood);
        var musterCard2 = TestUtil.MakeCard(ability: CardAbility.Muster,
            originPower: 4,
            cardType: CardType.Normal,
            chineseName: "M2",
            id: 50,
            musterType: "M1",
            badgeType: CardBadgeType.Wood);
        // 将其放入手牌区
        var initial = new SinglePlayerAreaModel(hostGen);
        var handModel = initial.handCardAreaModel;
        handModel = handModel with {
            handCardListModel = handModel.handCardListModel.AddCard(musterCard1) as HandCardListModel
        };
        initial = initial with { handCardAreaModel = handModel };

        // Act: 打出 muster 卡
        var result = initial.AddBattleAreaCard(musterCard2);

        // Assert: 手牌区移除，战区添加
        Assert.AreEqual(0, result.handCardAreaModel.handCardListModel.cardList.Count);
        var row = result.battleRowAreaList[(int)CardBadgeType.Wood];
        Assert.AreEqual(2, row.cardListModel.cardList.Count);
    }

    [Test]
    public void ApplyScorchWood_RemovesCard()
    {
        var initial = new SinglePlayerAreaModel(hostGen)
            .AddBattleAreaCard(TestUtil.MakeCard(badgeType: CardBadgeType.Wood, originPower: 5))
            .AddBattleAreaCard(TestUtil.MakeCard(badgeType: CardBadgeType.Wood, originPower: 5))
            .AddBattleAreaCard(TestUtil.MakeCard(badgeType: CardBadgeType.Wood, originPower: 8, cardType: CardType.Hero));
        // Act
        var result = initial.ApplyScorchWood();
        // Assert
        Assert.AreEqual(true, result.battleRowAreaList[(int)CardBadgeType.Wood].cardListModel.cardList[0].hasScorch);
        Assert.AreEqual(true, result.battleRowAreaList[(int)CardBadgeType.Wood].cardListModel.cardList[1].hasScorch);
        Assert.AreEqual(false, result.battleRowAreaList[(int)CardBadgeType.Wood].cardListModel.cardList[2].hasScorch);
    }

    [Test]
    public void GetMaxNormalPower_MultipleNormals_ReturnsMaxValue()
    {
        var c1 = TestUtil.MakeCard(originPower: 4, cardType: CardType.Normal, badgeType: CardBadgeType.Wood, id: 10);
        var c2 = TestUtil.MakeCard(originPower: 9, cardType: CardType.Normal, badgeType: CardBadgeType.Brass, id: 11);
        var model = new SinglePlayerAreaModel(hostGen)
            .AddBattleAreaCard(c1)
            .AddBattleAreaCard(c2);
        Assert.AreEqual(9, model.GetMaxNormalPower());
    }

    [Test]
    public void GetMaxNormalPower_MixedTypes_IgnoresNonNormals()
    {
        var hero = TestUtil.MakeCard(originPower: 12, cardType: CardType.Hero, badgeType: CardBadgeType.Wood, id: 20);
        var normal = TestUtil.MakeCard(originPower: 6, cardType: CardType.Normal, badgeType: CardBadgeType.Wood, id: 21);
        var model = new SinglePlayerAreaModel(hostGen)
            .AddBattleAreaCard(hero)
            .AddBattleAreaCard(normal);
        Assert.AreEqual(6, model.GetMaxNormalPower());
    }

    [Test]
    public void ApplyScorch_RemovesSingleMatchingCard()
    {
        var c1 = TestUtil.MakeCard(originPower: 5, cardType: CardType.Normal, badgeType: CardBadgeType.Wood, id: 40);
        var c2 = TestUtil.MakeCard(originPower: 3, cardType: CardType.Normal, badgeType: CardBadgeType.Wood, id: 41);
        var model = new SinglePlayerAreaModel(hostGen)
            .AddBattleAreaCard(c1)
            .AddBattleAreaCard(c2);
        var result = model.ApplyScorch(5);
        Assert.AreEqual(true, result.battleRowAreaList[(int)CardBadgeType.Wood].cardListModel.cardList[0].hasScorch);
        Assert.AreEqual(false, result.battleRowAreaList[(int)CardBadgeType.Wood].cardListModel.cardList[1].hasScorch);
    }

    [Test]
    public void ApplyScorch_RemovesMultipleMatchingCardsAcrossRows()
    {
        var c1 = TestUtil.MakeCard(originPower: 3, cardType: CardType.Normal, badgeType: CardBadgeType.Wood, id: 40);
        var c2 = TestUtil.MakeCard(originPower: 3, cardType: CardType.Normal, badgeType: CardBadgeType.Percussion, id: 41);
        var model = new SinglePlayerAreaModel(hostGen)
            .AddBattleAreaCard(c1)
            .AddBattleAreaCard(c2);
        var result = model.ApplyScorch(3);
        Assert.AreEqual(true, result.battleRowAreaList[(int)CardBadgeType.Wood].cardListModel.cardList[0].hasScorch);
        Assert.AreEqual(true, result.battleRowAreaList[(int)CardBadgeType.Percussion].cardListModel.cardList[0].hasScorch);
    }

    [Test]
    public void ApplyScorch_IgnoresNonNormalCards()
    {
        var hero = TestUtil.MakeCard(originPower: 7, cardType: CardType.Hero, badgeType: CardBadgeType.Brass, id: 50);
        var normal = TestUtil.MakeCard(originPower: 7, cardType: CardType.Normal, badgeType: CardBadgeType.Brass, id: 51);
        var model = new SinglePlayerAreaModel(hostGen)
            .AddBattleAreaCard(hero)
            .AddBattleAreaCard(normal);
        var result = model.ApplyScorch(7);
        Assert.AreEqual(false, result.battleRowAreaList[(int)CardBadgeType.Brass].cardListModel.cardList[0].hasScorch);
        Assert.AreEqual(true, result.battleRowAreaList[(int)CardBadgeType.Brass].cardListModel.cardList[1].hasScorch);
    }

    [Test]
    public void RemoveDeadCard()
    {
        var hero = TestUtil.MakeCard(originPower: 7, cardType: CardType.Hero, badgeType: CardBadgeType.Brass, id: 50);
        var normal = TestUtil.MakeCard(originPower: 7, cardType: CardType.Normal, badgeType: CardBadgeType.Brass, id: 51);
        var model = new SinglePlayerAreaModel(hostGen)
            .AddBattleAreaCard(hero)
            .AddBattleAreaCard(normal);
        var result = model.ApplyScorch(7).RemoveDeadCard(out var removed);
        Assert.AreEqual(1, removed.Count);
        Assert.AreEqual(1, result.discardAreaModel.cardListModel.cardList.Count);
        Assert.AreEqual(false, result.discardAreaModel.cardListModel.cardList[0].hasScorch);
        Assert.IsTrue(removed[0].cardInfo.cardType == CardType.Normal);
        Assert.AreEqual(50, result.battleRowAreaList[(int)CardBadgeType.Brass].cardListModel.cardList[0].cardInfo.id);
    }

    [Test]
    public void RemoveDeadCard_Bond()
    {
        var bond1 = TestUtil.MakeCard(originPower: 6, ability: CardAbility.Bond, bondType: "bond", badgeType: CardBadgeType.Brass, id: 50);
        var bond2 = TestUtil.MakeCard(originPower: 6, ability: CardAbility.Bond, bondType: "bond", badgeType: CardBadgeType.Brass, id: 51);
        var bond3 = TestUtil.MakeCard(originPower: 7, ability: CardAbility.Bond, bondType: "bond", badgeType: CardBadgeType.Brass, id: 52);
        var model = new SinglePlayerAreaModel(hostGen)
            .AddBattleAreaCard(bond1)
            .AddBattleAreaCard(bond2)
            .AddBattleAreaCard(bond3);
        Assert.AreEqual(57, model.GetCurrentPower());
        var result = model.ApplyScorch(18).RemoveDeadCard(out var removed);
        Assert.AreEqual(2, removed.Count);
        Assert.AreEqual(7, result.GetCurrentPower());
    }

    [Test]
    public void ApplyLip()
    {
        var c1 = TestUtil.MakeCard(originPower: 7, isMale: false, badgeType: CardBadgeType.Wood);
        var c2 = TestUtil.MakeCard(originPower: 7, isMale: true, badgeType: CardBadgeType.Brass);
        var model = new SinglePlayerAreaModel(hostGen)
            .AddBattleAreaCard(c1)
            .AddBattleAreaCard(c2);
        var result = model.ApplyLip(out var attackCardList);
        Assert.AreEqual(1, attackCardList.Count);
        Assert.AreEqual(true, attackCardList[0].cardInfo.isMale);
        Assert.AreEqual(7, result.battleRowAreaList[(int)CardBadgeType.Wood].GetCurrentPower());
        Assert.AreEqual(5, result.battleRowAreaList[(int)CardBadgeType.Brass].GetCurrentPower());
    }

    [Test]
    public void PrepareAttackTarget()
    {
        var c1 = TestUtil.MakeCard(originPower: 7, cardType: CardType.Normal, badgeType: CardBadgeType.Wood);
        var c2 = TestUtil.MakeCard(originPower: 7, cardType: CardType.Hero, badgeType: CardBadgeType.Brass);
        var model = new SinglePlayerAreaModel(hostGen)
            .AddBattleAreaCard(c1)
            .AddBattleAreaCard(c2);
        var result = model.PrepareAttackTarget(out var targetCardList);
        Assert.AreEqual(1, targetCardList.Count);
        Assert.AreEqual(CardSelectType.WithstandAttack, result.battleRowAreaList[(int)CardBadgeType.Wood].cardListModel.cardList[0].cardSelectType);
        Assert.AreEqual(CardSelectType.None, result.battleRowAreaList[(int)CardBadgeType.Brass].cardListModel.cardList[0].cardSelectType);
    }

    [Test]
    public void PrepareMedicTarget()
    {
        var c1 = TestUtil.MakeCard(originPower: 7, cardType: CardType.Normal);
        var c2 = TestUtil.MakeCard(originPower: 7, cardType: CardType.Hero);
        var model = new SinglePlayerAreaModel(hostGen);
        model = model with {
            discardAreaModel = model.discardAreaModel.AddCard(c1).AddCard(c2)
        };
        var result = model.PrepareMedicTarget(out var targetCardList);
        Assert.AreEqual(1, targetCardList.Count);
        Assert.AreEqual(CardSelectType.PlayCard, result.discardAreaModel.cardListModel.cardList[0].cardSelectType);
        Assert.AreEqual(CardSelectType.None, result.discardAreaModel.cardListModel.cardList[1].cardSelectType);
    }

    [Test]
    public void PrepareDecoyTarget()
    {
        var c1 = TestUtil.MakeCard(originPower: 7, cardType: CardType.Normal, badgeType: CardBadgeType.Wood);
        var c2 = TestUtil.MakeCard(originPower: 7, cardType: CardType.Hero, badgeType: CardBadgeType.Brass);
        var model = new SinglePlayerAreaModel(hostGen)
            .AddBattleAreaCard(c1)
            .AddBattleAreaCard(c2);
        var result = model.PrepareDecoyTarget(out var targetCardList);
        Assert.AreEqual(1, targetCardList.Count);
        Assert.AreEqual(CardSelectType.DecoyWithdraw, result.battleRowAreaList[(int)CardBadgeType.Wood].cardListModel.cardList[0].cardSelectType);
        Assert.AreEqual(CardSelectType.None, result.battleRowAreaList[(int)CardBadgeType.Brass].cardListModel.cardList[0].cardSelectType);
    }

    [Test]
    public void DecoyWithdrawCard()
    {
        var c1 = TestUtil.MakeCard(originPower: 7, cardType: CardType.Normal, badgeType: CardBadgeType.Wood);
        var c2 = TestUtil.MakeCard(originPower: 0, ability: CardAbility.Decoy);
        var model = new SinglePlayerAreaModel(hostGen)
            .AddBattleAreaCard(c1);
        var result = model.DecoyWithdrawCard(model.battleRowAreaList[(int)CardBadgeType.Wood].cardListModel.cardList[0], c2);
        Assert.AreEqual(1, result.handCardAreaModel.handCardListModel.cardList.Count);
        Assert.AreEqual(1, result.battleRowAreaList[(int)CardBadgeType.Wood].cardListModel.cardList.Count);
        Assert.AreEqual(0, result.GetCurrentPower());
    }

    [Test]
    public void HasCardInBattleArea()
    {
        var c1 = TestUtil.MakeCard(chineseName: "test1");
        var c2 = TestUtil.MakeCard(chineseName: "test2", ability: CardAbility.HornUtil);
        var model = new SinglePlayerAreaModel(hostGen)
            .AddBattleAreaCard(c1)
            .AddBattleAreaCard(c2);
        Assert.AreEqual(true, model.HasCardInBattleArea("test1"));
        Assert.AreEqual(true, model.HasCardInBattleArea("test2"));
        Assert.AreEqual(false, model.HasCardInBattleArea("test3"));
    }

    [Test]
    public void PrepareMonakaTarget()
    {
        var c1 = TestUtil.MakeCard(originPower: 7, cardType: CardType.Normal, badgeType: CardBadgeType.Wood);
        var c2 = TestUtil.MakeCard(originPower: 7, cardType: CardType.Hero, badgeType: CardBadgeType.Brass);
        var model = new SinglePlayerAreaModel(hostGen)
            .AddBattleAreaCard(c1)
            .AddBattleAreaCard(c2);
        var result = model.PrepareMonakaTarget(out var targetCardList);
        Assert.AreEqual(1, targetCardList.Count);
        Assert.AreEqual(CardSelectType.Monaka, result.battleRowAreaList[(int)CardBadgeType.Wood].cardListModel.cardList[0].cardSelectType);
        Assert.AreEqual(CardSelectType.None, result.battleRowAreaList[(int)CardBadgeType.Brass].cardListModel.cardList[0].cardSelectType);
    }

    [Test]
    public void ResetCardSelectType()
    {
        var c1 = TestUtil.MakeCard(originPower: 7, cardType: CardType.Normal, badgeType: CardBadgeType.Wood);
        var c2 = TestUtil.MakeCard(originPower: 7, cardType: CardType.Normal);
        var model = new SinglePlayerAreaModel(hostGen)
            .AddBattleAreaCard(c1);
        model = model with {
            discardAreaModel = model.discardAreaModel.AddCard(c2)
        };
        var result = model.PrepareAttackTarget(out var targetCardList);
        result = result.PrepareMedicTarget(out targetCardList);
        result = result.ResetCardSelectType();
        Assert.AreEqual(CardSelectType.None, result.battleRowAreaList[(int)CardBadgeType.Wood].cardListModel.cardList[0].cardSelectType);
        Assert.AreEqual(CardSelectType.None, result.discardAreaModel.cardListModel.cardList[0].cardSelectType);
    }

    [Test]
    public void RemoveAllBattleCard()
    {
        var model = new SinglePlayerAreaModel(hostGen)
            .AddBattleAreaCard(TestUtil.MakeCard(id: 11, originPower: 7, cardType: CardType.Normal, badgeType: CardBadgeType.Wood))
            .AddBattleAreaCard(TestUtil.MakeCard(id: 21, originPower: 7, cardType: CardType.Normal, badgeType: CardBadgeType.Brass))
            .AddBattleAreaCard(TestUtil.MakeCard(id: 31, originPower: 7, cardType: CardType.Normal, badgeType: CardBadgeType.Percussion));
        var newWoodRow = model.battleRowAreaList[(int)CardBadgeType.Wood]
            .AddCard(TestUtil.MakeCard(id: 41, cardType: CardType.Util, ability: CardAbility.HornUtil))
            .AddCard(TestUtil.MakeCard(id: 51, cardType: CardType.Leader, ability: CardAbility.Decoy));
        model = model with {
            battleRowAreaList = model.battleRowAreaList.SetItem((int)CardBadgeType.Wood, newWoodRow)
        };
        var result = model.RemoveAllBattleCard();
        Assert.AreEqual(0, result.GetCurrentPower());
        Assert.AreEqual(3, result.discardAreaModel.cardListModel.cardList.Count);
        foreach (var card in result.discardAreaModel.cardListModel.cardList) {
            Assert.AreNotEqual(41, card.cardInfo.id);
            Assert.AreNotEqual(51, card.cardInfo.id);
        }
    }

    [Test]
    public void RemoveAllBattleCard_Keep_Empty()
    {
        var model = new SinglePlayerAreaModel(hostGen)
            .AddBattleAreaCard(TestUtil.MakeCard(id: 11, originPower: 7, cardType: CardType.Normal, badgeType: CardBadgeType.Wood).AddBuff(CardBuffType.Attack2, 1))
            .AddBattleAreaCard(TestUtil.MakeCard(id: 21, originPower: 7, cardType: CardType.Normal, badgeType: CardBadgeType.Wood).AddBuff(CardBuffType.Attack2, 1))
            .AddBattleAreaCard(TestUtil.MakeCard(id: 31, originPower: 7, cardType: CardType.Normal, badgeType: CardBadgeType.Wood).AddBuff(CardBuffType.Attack2, 1));
        var result = model.RemoveAllBattleCard(true);
        Assert.AreEqual(7, result.GetCurrentPower());
        Assert.AreEqual(1, result.battleRowAreaList[(int)CardBadgeType.Wood].cardListModel.cardList.Count);
        Assert.AreEqual(2, result.discardAreaModel.cardListModel.cardList.Count);
    }

    [Test]
    public void RemoveAllBattleCard_Keep_Exist()
    {
        var model = new SinglePlayerAreaModel(hostGen);
        var newWoodRow = model.battleRowAreaList[(int)CardBadgeType.Wood]
            .AddCard(TestUtil.MakeCard(id: 51, cardType: CardType.Leader, ability: CardAbility.Decoy));
        model = model with {
            battleRowAreaList = model.battleRowAreaList.SetItem((int)CardBadgeType.Wood, newWoodRow)
        };
        var result = model.RemoveAllBattleCard(true);
        Assert.AreEqual(0, result.battleRowAreaList[(int)CardBadgeType.Wood].cardListModel.cardList.Count);
        Assert.AreEqual(0, result.discardAreaModel.cardListModel.cardList.Count);
    }

    [Test]
    public void AddBattleAreaCard_K5LeaderAbility()
    {
        var card1 = TestUtil.MakeCard(originPower: 6,
            id: 20,
            badgeType: CardBadgeType.Wood,
            grade: 1);
        var card2 = TestUtil.MakeCard(originPower: 6,
            id: 21,
            badgeType: CardBadgeType.Brass,
            grade: 1);
        var card3 = TestUtil.MakeCard(originPower: 6,
            cardType: CardType.Hero,
            id: 22,
            badgeType: CardBadgeType.Brass,
            grade: 2);
        var card4 = TestUtil.MakeCard(originPower: 6,
            id: 23,
            badgeType: CardBadgeType.Percussion,
            grade: 2);
        var card5 = TestUtil.MakeCard(originPower: 6,
            id: 24,
            badgeType: CardBadgeType.Brass,
            ability: CardAbility.K5Leader,
            grade: 1);

        var model = new SinglePlayerAreaModel(hostGen)
            .AddBattleAreaCard(card1)
            .AddBattleAreaCard(card2)
            .AddBattleAreaCard(card3)
            .AddBattleAreaCard(card4)
            .AddBattleAreaCard(card5);

        Assert.AreEqual(8, model.battleRowAreaList[(int)CardBadgeType.Wood].GetCurrentPower());
        Assert.AreEqual(8 + 6 + 6, model.battleRowAreaList[(int)CardBadgeType.Brass].GetCurrentPower());
        Assert.AreEqual(6, model.battleRowAreaList[(int)CardBadgeType.Percussion].GetCurrentPower());
    }

    [Test]
    public void AddBattleAreaCard_PressureAbility()
    {
        var normalCard = TestUtil.MakeCard(originPower: 6,
            id: 20,
            badgeType: CardBadgeType.Wood);
        var pressureCard = TestUtil.MakeCard(originPower: 6,
            id: 21,
            ability: CardAbility.Pressure,
            badgeType: CardBadgeType.Brass,
            cardType: CardType.Hero);
        var model = new SinglePlayerAreaModel(hostGen)
            .InitKRandom(10)
            .AddBattleAreaCard(normalCard)
            .AddBattleAreaCard(pressureCard);
        if (model.battleRowAreaList[(int)CardBadgeType.Wood].cardListModel.cardList[0].currentPower == 8) {
            Assert.AreEqual(14, model.GetCurrentPower());
        } else {
            Assert.AreEqual(11, model.GetCurrentPower());
        }
    }

    [Test]
    public void ApplyPowerFirst()
    {
        var c1 = TestUtil.MakeCard(id: 1, originPower: 4, badgeType: CardBadgeType.Wood);
        var c2 = TestUtil.MakeCard(id: 2, originPower: 3, badgeType: CardBadgeType.Brass);
        var model = new SinglePlayerAreaModel(hostGen)
            .AddBattleAreaCard(c1)
            .AddBattleAreaCard(c2);
        var result = model.ApplyPowerFirst(out var attackCardList);
        Assert.AreEqual(1, attackCardList.Count);
        Assert.AreEqual(2, attackCardList[0].cardInfo.id);
        Assert.AreEqual(4, result.battleRowAreaList[(int)CardBadgeType.Wood].GetCurrentPower());
        Assert.AreEqual(2, result.battleRowAreaList[(int)CardBadgeType.Brass].GetCurrentPower());
    }

    [Test]
    public void ApplyPowerFirst_Morale()
    {
        var c1 = TestUtil.MakeCard(id: 1, originPower: 1, badgeType: CardBadgeType.Wood);
        var c2 = TestUtil.MakeCard(id: 2, originPower: 1, badgeType: CardBadgeType.Wood, ability: CardAbility.Morale);
        var model = new SinglePlayerAreaModel(hostGen)
            .AddBattleAreaCard(c1)
            .AddBattleAreaCard(c2);
        model = model.ApplyPowerFirst(out var attackCardList);
        Assert.AreEqual(2, attackCardList.Count);
        Assert.AreEqual(1, model.battleRowAreaList[(int)CardBadgeType.Wood].GetCurrentPower());
        model = model.RemoveDeadCard(out var removedCardList);
        Assert.AreEqual(2, removedCardList.Count);
        Assert.AreEqual(0, model.battleRowAreaList[(int)CardBadgeType.Wood].GetCurrentPower());
    }
}
