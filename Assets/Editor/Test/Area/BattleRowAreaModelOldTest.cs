using NUnit.Framework;
using System;
using System.Collections.Generic;

public class BattleRowAreaModelOldTest
{
    [Test]
    public void Horn()
    {
        BattleRowAreaModelOld battleRowAreaModel = new BattleRowAreaModelOld(CardBadgeType.Brass);
        int brassHornInfoId = 2036;
        List<int> brassNormalInfoIdList = new List<int> {2037, 2038};
        List<CardModelOld> brassNormalCardList = TestGenCards.GetCardList(brassNormalInfoIdList);
        CardModelOld brassHornCard = TestGenCards.GetCard(brassHornInfoId);

        // 加入第一张普通牌
        battleRowAreaModel.AddCard(brassNormalCardList[0]);
        Assert.AreEqual(CardLocation.BattleArea, brassNormalCardList[0].cardLocation);
        Assert.AreEqual(CardSelectType.None, brassNormalCardList[0].selectType);
        Assert.AreEqual(brassNormalCardList[0].cardInfo.originPower, brassNormalCardList[0].currentPower);
        Assert.AreEqual(brassNormalCardList[0].currentPower, battleRowAreaModel.GetCurrentPower());
        Assert.AreEqual(brassNormalCardList[0].currentPower, battleRowAreaModel.GetCurrentPower());

        // 加入horn牌
        battleRowAreaModel.AddCard(brassHornCard);
        Assert.AreEqual(brassHornCard.cardInfo.originPower, brassHornCard.currentPower);
        int expectPower = brassNormalCardList[0].cardInfo.originPower * 2 + brassHornCard.cardInfo.originPower;
        Assert.AreEqual(expectPower, battleRowAreaModel.GetCurrentPower());

        // 加入第二张普通牌，测试已存在horn时加入新牌的情况
        battleRowAreaModel.AddCard(brassNormalCardList[1]);
        Assert.AreEqual(brassNormalCardList[1].cardInfo.originPower * 2, brassNormalCardList[1].currentPower);
        expectPower = (brassNormalCardList[0].cardInfo.originPower + brassNormalCardList[1].cardInfo.originPower) * 2 +
            brassHornCard.cardInfo.originPower;
        Assert.AreEqual(expectPower, battleRowAreaModel.GetCurrentPower());

        // 移除horn牌，测试buff的清除情况
        battleRowAreaModel.RemoveCard(brassHornCard);
        expectPower = brassNormalCardList[0].cardInfo.originPower +
            brassNormalCardList[1].cardInfo.originPower;
        Assert.AreEqual(expectPower, battleRowAreaModel.GetCurrentPower());
    }

    [Test]
    public void Morale()
    {
        BattleRowAreaModelOld battleRowAreaModel = new BattleRowAreaModelOld(CardBadgeType.Brass);
        int brassMoraleInfoId = 2020; // 特殊：这是个英雄牌
        List<int> brassNormalInfoIdList = new List<int> { 2037, 2038 };
        List<CardModelOld> brassNormalCardList = TestGenCards.GetCardList(brassNormalInfoIdList);
        CardModelOld brassMoraleCard = TestGenCards.GetCard(brassMoraleInfoId);

        // 加入第一张普通牌
        battleRowAreaModel.AddCard(brassNormalCardList[0]);
        Assert.AreEqual(brassNormalCardList[0].cardInfo.originPower, brassNormalCardList[0].currentPower);
        Assert.AreEqual(brassNormalCardList[0].currentPower, battleRowAreaModel.GetCurrentPower());

        // 加入morale牌
        battleRowAreaModel.AddCard(brassMoraleCard);
        Assert.AreEqual(brassMoraleCard.cardInfo.originPower, brassMoraleCard.currentPower);
        int expectPower = brassNormalCardList[0].cardInfo.originPower + 1 + brassMoraleCard.cardInfo.originPower;
        Assert.AreEqual(expectPower, battleRowAreaModel.GetCurrentPower());

        // 加入第二张普通牌，测试已存在morale时加入新牌的情况
        battleRowAreaModel.AddCard(brassNormalCardList[1]);
        Assert.AreEqual(brassNormalCardList[1].cardInfo.originPower + 1, brassNormalCardList[1].currentPower);
        expectPower = brassNormalCardList[0].cardInfo.originPower + brassNormalCardList[1].cardInfo.originPower + 2 +
            brassMoraleCard.cardInfo.originPower;
        Assert.AreEqual(expectPower, battleRowAreaModel.GetCurrentPower());

        // 移除morale牌，测试buff的清除情况。这是个英雄牌，正常不会被移除，此处仅为测试morale技能
        battleRowAreaModel.RemoveCard(brassMoraleCard);
        expectPower = brassNormalCardList[0].cardInfo.originPower +
            brassNormalCardList[1].cardInfo.originPower;
        Assert.AreEqual(expectPower, battleRowAreaModel.GetCurrentPower());
    }

    [Test]
    public void MoraleHorn()
    {
        BattleRowAreaModelOld battleRowAreaModel = new BattleRowAreaModelOld(CardBadgeType.Brass);
        int brassMoraleInfoId = 2020; // 特殊：这是个英雄牌
        int brassHornInfoId = 2036;
        List<int> brassNormalInfoIdList = new List<int> { 2037, 2038 };
        List<CardModelOld> brassNormalCardList = TestGenCards.GetCardList(brassNormalInfoIdList);
        CardModelOld brassMoraleCard = TestGenCards.GetCard(brassMoraleInfoId);
        CardModelOld brassHornCard = TestGenCards.GetCard(brassHornInfoId);

        // 加入第一张普通牌
        battleRowAreaModel.AddCard(brassNormalCardList[0]);
        Assert.AreEqual(brassNormalCardList[0].cardInfo.originPower, brassNormalCardList[0].currentPower);
        Assert.AreEqual(brassNormalCardList[0].currentPower, battleRowAreaModel.GetCurrentPower());

        // 加入morale牌
        battleRowAreaModel.AddCard(brassMoraleCard);
        Assert.AreEqual(brassMoraleCard.cardInfo.originPower, brassMoraleCard.currentPower);
        int expectPower = brassNormalCardList[0].cardInfo.originPower + 1 +
            brassMoraleCard.cardInfo.originPower;
        Assert.AreEqual(expectPower, battleRowAreaModel.GetCurrentPower());

        // 加入horn牌
        battleRowAreaModel.AddCard(brassHornCard);
        Assert.AreEqual(brassHornCard.cardInfo.originPower + 1, brassHornCard.currentPower);
        expectPower = brassNormalCardList[0].cardInfo.originPower * 2 + 1 +
            brassMoraleCard.cardInfo.originPower +
            brassHornCard.cardInfo.originPower + 1;
        Assert.AreEqual(expectPower, battleRowAreaModel.GetCurrentPower());

        // 加入第二张普通牌
        battleRowAreaModel.AddCard(brassNormalCardList[1]);
        Assert.AreEqual(brassNormalCardList[1].cardInfo.originPower * 2 + 1, brassNormalCardList[1].currentPower);
        expectPower = brassNormalCardList[0].cardInfo.originPower * 2 + 1 +
            brassNormalCardList[1].cardInfo.originPower * 2 + 1 +
            brassMoraleCard.cardInfo.originPower +
            brassHornCard.cardInfo.originPower + 1;
        Assert.AreEqual(expectPower, battleRowAreaModel.GetCurrentPower());

        // 移除morale牌，测试buff的清除情况。这是个英雄牌，正常不会被移除，此处仅为测试morale技能
        battleRowAreaModel.RemoveCard(brassMoraleCard);
        expectPower = brassNormalCardList[0].cardInfo.originPower * 2 +
            brassNormalCardList[1].cardInfo.originPower * 2 +
            brassHornCard.cardInfo.originPower;
        Assert.AreEqual(expectPower, battleRowAreaModel.GetCurrentPower());

        // 移除horn牌，测试buff的清除情况
        battleRowAreaModel.RemoveCard(brassHornCard);
        expectPower = brassNormalCardList[0].cardInfo.originPower +
            brassNormalCardList[1].cardInfo.originPower;
        Assert.AreEqual(expectPower, battleRowAreaModel.GetCurrentPower());
    }

    // 测试普通牌被伞击
    [Test]
    public void NormalScorchWood()
    {
        BattleRowAreaModelOld battleRowAreaModel = new BattleRowAreaModelOld(CardBadgeType.Wood);
        List<int> woodNormalInfoIdList = new List<int> { 2009, 2017 }; // 能力分别为7、6
        List<CardModelOld> woodNormalCardList = TestGenCards.GetCardList(woodNormalInfoIdList);

        // 一张普通牌，总点数不到10，伞击无事发生
        battleRowAreaModel.AddCard(woodNormalCardList[0]);
        List<CardModelOld> scorchTargetList = battleRowAreaModel.ApplyScorchWood();
        Assert.AreEqual(0, scorchTargetList.Count);
        Assert.AreEqual(woodNormalCardList[0].cardInfo.originPower, battleRowAreaModel.GetCurrentPower());

        // 两张普通牌，总点数超过10，最高点数的牌被移除
        battleRowAreaModel.AddCard(woodNormalCardList[1]);
        scorchTargetList = battleRowAreaModel.ApplyScorchWood();
        Assert.AreEqual(1, scorchTargetList.Count);
        Assert.AreEqual(woodNormalCardList[1].cardInfo.originPower, battleRowAreaModel.GetCurrentPower());
    }

    // 测试包含英雄牌被伞击
    [Test]
    public void HeroScorchWood()
    {
        BattleRowAreaModelOld battleRowAreaModel = new BattleRowAreaModelOld(CardBadgeType.Wood);
        List<int> woodHeroInfoIdList = new List<int> { 2005, 2010 }; // 能力分别为10、7
        List<CardModelOld> woodHeroCardList = TestGenCards.GetCardList(woodHeroInfoIdList);
        List<int> woodNormalInfoIdList = new List<int> { 2009, 2017 }; // 能力分别为7、6
        List<CardModelOld> woodNormalCardList = TestGenCards.GetCardList(woodNormalInfoIdList);

        // 两张英雄牌，总点数17，伞击无事发生
        battleRowAreaModel.AddCard(woodHeroCardList[0]);
        battleRowAreaModel.AddCard(woodHeroCardList[1]);
        List<CardModelOld> scorchTargetList = battleRowAreaModel.ApplyScorchWood();
        Assert.AreEqual(0, scorchTargetList.Count);
        Assert.Less(10, battleRowAreaModel.GetCurrentPower());
        int expectPower = woodHeroCardList[0].cardInfo.originPower +
            woodHeroCardList[1].cardInfo.originPower;
        Assert.AreEqual(expectPower, battleRowAreaModel.GetCurrentPower());

        // 一张普通牌与两张英雄牌，总点数超过10，最高点数的普通牌被移除
        battleRowAreaModel.AddCard(woodNormalCardList[0]);
        scorchTargetList = battleRowAreaModel.ApplyScorchWood();
        Assert.AreEqual(1, scorchTargetList.Count);
        Assert.AreEqual(expectPower, battleRowAreaModel.GetCurrentPower());
    }

    // 测试多张点数相同的牌被伞击
    [Test]
    public void MultiScorchWood()
    {
        BattleRowAreaModelOld battleRowAreaModel = new BattleRowAreaModelOld(CardBadgeType.Wood);
        List<int> woodNormalInfoIdList = new List<int> { 2002, 2019, 2006 }; // 能力分别为5、5、4
        List<CardModelOld> woodNormalCardList = TestGenCards.GetCardList(woodNormalInfoIdList);

        // 两张普通牌能力都是5，总点数10，无事发生
        battleRowAreaModel.AddCard(woodNormalCardList[0]);
        battleRowAreaModel.AddCard(woodNormalCardList[1]);
        int expectPower = woodNormalCardList[0].cardInfo.originPower +
            woodNormalCardList[1].cardInfo.originPower;
        Assert.AreEqual(expectPower, battleRowAreaModel.GetCurrentPower());
        List<CardModelOld> scorchTargetList = battleRowAreaModel.ApplyScorchWood();
        Assert.AreEqual(0, scorchTargetList.Count);
        Assert.AreEqual(expectPower, battleRowAreaModel.GetCurrentPower());

        // 三张普通牌总点数14，两张5点的被移除
        battleRowAreaModel.AddCard(woodNormalCardList[2]);
        scorchTargetList = battleRowAreaModel.ApplyScorchWood();
        Assert.AreEqual(2, scorchTargetList.Count);
        expectPower = woodNormalCardList[2].cardInfo.originPower;
        Assert.AreEqual(expectPower, battleRowAreaModel.GetCurrentPower());
    }

    [Test]
    public void BrassScorchWood()
    {
        BattleRowAreaModelOld battleRowAreaModel = new BattleRowAreaModelOld(CardBadgeType.Brass);
        List<int> brassNormalInfoIdList = new List<int> { 2025, 2028 }; // 能力分别为6、6
        List<CardModelOld> brassNormalCardList = TestGenCards.GetCardList(brassNormalInfoIdList);

        // 两张普通牌能力都是6，总点数12，但不是木管行，无事发生
        battleRowAreaModel.AddCard(brassNormalCardList[0]);
        battleRowAreaModel.AddCard(brassNormalCardList[1]);
        int expectPower = brassNormalCardList[0].cardInfo.originPower +
            brassNormalCardList[1].cardInfo.originPower;
        Assert.Less(10, battleRowAreaModel.GetCurrentPower());
        List<CardModelOld> scorchTargetList = battleRowAreaModel.ApplyScorchWood();
        Assert.AreEqual(0, scorchTargetList.Count);
        Assert.AreEqual(expectPower, battleRowAreaModel.GetCurrentPower());
    }

    // 测试移除卡牌后，被移除的牌是否buff状态清零
    [Test]
    public void RemoveCard()
    {
        BattleRowAreaModelOld battleRowAreaModel = new BattleRowAreaModelOld(CardBadgeType.Brass);
        int brassMoraleInfoId = 2020; // 特殊：这是个英雄牌
        int brassHornInfoId = 2036;
        List<int> brassNormalInfoIdList = new List<int> { 2037, 2038 };
        List<CardModelOld> brassNormalCardList = TestGenCards.GetCardList(brassNormalInfoIdList);
        CardModelOld brassMoraleCard = TestGenCards.GetCard(brassMoraleInfoId);
        CardModelOld brassHornCard = TestGenCards.GetCard(brassHornInfoId);

        battleRowAreaModel.AddCard(brassMoraleCard);
        battleRowAreaModel.AddCard(brassHornCard);
        battleRowAreaModel.AddCardList(brassNormalCardList);
        int expectPower = brassNormalCardList[0].cardInfo.originPower * 2 + 1 +
            brassNormalCardList[1].cardInfo.originPower * 2 + 1 +
            brassMoraleCard.cardInfo.originPower +
            brassHornCard.cardInfo.originPower + 1;
        Assert.AreEqual(expectPower, battleRowAreaModel.GetCurrentPower());

        // 移除带buff的牌
        battleRowAreaModel.RemoveCard(brassNormalCardList[0]);
        Assert.AreEqual(brassNormalCardList[0].cardInfo.originPower, brassNormalCardList[0].currentPower);

        // 移除所有牌
        battleRowAreaModel.RemoveAllCard();
        Assert.AreEqual(0, battleRowAreaModel.cardList.Count);
        Assert.AreEqual(brassNormalCardList[0].cardInfo.originPower, brassNormalCardList[0].currentPower);
        Assert.AreEqual(brassNormalCardList[1].cardInfo.originPower, brassNormalCardList[1].currentPower);
        Assert.AreEqual(brassMoraleCard.cardInfo.originPower, brassMoraleCard.currentPower);
        Assert.AreEqual(brassHornCard.cardInfo.originPower, brassHornCard.currentPower);
    }

    // 测试horn util
    [Test]
    public void HornUtil()
    {
        BattleRowAreaModelOld battleRowAreaModel = new BattleRowAreaModelOld(CardBadgeType.Brass);
        CardModelOld brassHeroCard = TestGenCards.GetCard(2040); // medic hero(9)
        CardModelOld brassNormalCard = TestGenCards.GetCard(2024); // normal(4)
        CardModelOld brassHornCard = TestGenCards.GetCard(2036); // horn(2)
        CardModelOld hornUtilCard = TestGenCards.GetCard(5008); // horn util

        battleRowAreaModel.AddCard(brassHeroCard);
        battleRowAreaModel.AddCard(brassNormalCard);
        battleRowAreaModel.AddCard(brassHornCard);
        battleRowAreaModel.AddCard(hornUtilCard);
        int expectPower = brassHeroCard.cardInfo.originPower +
            brassNormalCard.cardInfo.originPower * 3 +
            brassHornCard.cardInfo.originPower * 2;
        Assert.AreEqual(expectPower, battleRowAreaModel.GetCurrentPower());

        // 移除所有牌
        battleRowAreaModel.RemoveAllCard();
        Assert.AreEqual(0, battleRowAreaModel.GetCurrentPower());
        Assert.AreEqual(0, battleRowAreaModel.hornUtilCardArea.cardList.Count);
        Assert.AreEqual(brassHeroCard.cardInfo.originPower, brassHeroCard.currentPower);
        Assert.AreEqual(brassNormalCard.cardInfo.originPower, brassNormalCard.currentPower);
        Assert.AreEqual(brassHornCard.cardInfo.originPower, brassHornCard.currentPower);
    }

    // 测试kasa
    [Test]
    public void Kasa()
    {
        BattleRowAreaModelOld battleRowAreaModel = new BattleRowAreaModelOld(CardBadgeType.Wood);
        CardModelOld k1MizoreCard1 = TestGenCards.GetCard(1051); // 铠冢霙，普通(8)
        CardModelOld k1MizoreCard2 = TestGenCards.GetCard(1051); // 铠冢霙，普通(8)
        CardModelOld k1NozomiCard = TestGenCards.GetCard(1052); // 伞木希美，kasa(7)

        battleRowAreaModel.AddCard(k1MizoreCard1);
        k1MizoreCard1.AddBuff(CardBuffType.Attack2, 1);
        Assert.AreEqual(6, battleRowAreaModel.GetCurrentPower());
        battleRowAreaModel.AddCard(k1MizoreCard2);
        k1MizoreCard2.AddBuff(CardBuffType.Weather, 1);
        Assert.AreEqual(7, battleRowAreaModel.GetCurrentPower()); // 6 + 1
        battleRowAreaModel.AddCard(k1NozomiCard);
        Assert.AreEqual(33, battleRowAreaModel.GetCurrentPower()); // 13 + 13 + 7
    }
}
