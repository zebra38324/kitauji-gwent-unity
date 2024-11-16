using NUnit.Framework;
using System;
using System.Collections.Generic;

public class BattleRowAreaModelTest
{
    [Test]
    public void Horn()
    {
        BattleRowAreaModel battleRowAreaModel = new BattleRowAreaModel(CardBadgeType.Brass);
        int brassHornInfoId = 2036;
        List<int> brassNormalInfoIdList = new List<int> {2037, 2038};
        List<CardModel> brassNormalCardList = TestGenCards.GetCardList(brassNormalInfoIdList);
        CardModel brassHornCard = TestGenCards.GetCard(brassHornInfoId);

        // 加入第一张普通牌
        battleRowAreaModel.AddCard(brassNormalCardList[0]);
        Assert.AreEqual(CardLocation.BattleArea, brassNormalCardList[0].cardLocation);
        Assert.AreEqual(CardSelectType.None, brassNormalCardList[0].selectType);
        Assert.AreEqual(brassNormalCardList[0].GetCardInfo().originPower, brassNormalCardList[0].GetCurrentPower());
        Assert.AreEqual(brassNormalCardList[0].GetCurrentPower(), battleRowAreaModel.GetCurrentPower());

        // 加入horn牌
        battleRowAreaModel.AddCard(brassHornCard);
        Assert.AreEqual(brassHornCard.GetCardInfo().originPower, brassHornCard.GetCurrentPower());
        int expectPower = brassNormalCardList[0].GetCardInfo().originPower * 2 + brassHornCard.GetCardInfo().originPower;
        Assert.AreEqual(expectPower, battleRowAreaModel.GetCurrentPower());

        // 加入第二张普通牌，测试已存在horn时加入新牌的情况
        battleRowAreaModel.AddCard(brassNormalCardList[1]);
        Assert.AreEqual(brassNormalCardList[1].GetCardInfo().originPower * 2, brassNormalCardList[1].GetCurrentPower());
        expectPower = (brassNormalCardList[0].GetCardInfo().originPower + brassNormalCardList[1].GetCardInfo().originPower) * 2 +
            brassHornCard.GetCardInfo().originPower;
        Assert.AreEqual(expectPower, battleRowAreaModel.GetCurrentPower());

        // 移除horn牌，测试buff的清除情况
        battleRowAreaModel.RemoveCard(brassHornCard);
        expectPower = brassNormalCardList[0].GetCardInfo().originPower +
            brassNormalCardList[1].GetCardInfo().originPower;
        Assert.AreEqual(expectPower, battleRowAreaModel.GetCurrentPower());
    }

    [Test]
    public void Morale()
    {
        BattleRowAreaModel battleRowAreaModel = new BattleRowAreaModel(CardBadgeType.Brass);
        int brassMoraleInfoId = 2020; // 特殊：这是个英雄牌
        List<int> brassNormalInfoIdList = new List<int> { 2037, 2038 };
        List<CardModel> brassNormalCardList = TestGenCards.GetCardList(brassNormalInfoIdList);
        CardModel brassMoraleCard = TestGenCards.GetCard(brassMoraleInfoId);

        // 加入第一张普通牌
        battleRowAreaModel.AddCard(brassNormalCardList[0]);
        Assert.AreEqual(brassNormalCardList[0].GetCardInfo().originPower, brassNormalCardList[0].GetCurrentPower());
        Assert.AreEqual(brassNormalCardList[0].GetCurrentPower(), battleRowAreaModel.GetCurrentPower());

        // 加入morale牌
        battleRowAreaModel.AddCard(brassMoraleCard);
        Assert.AreEqual(brassMoraleCard.GetCardInfo().originPower, brassMoraleCard.GetCurrentPower());
        int expectPower = brassNormalCardList[0].GetCardInfo().originPower + 1 + brassMoraleCard.GetCardInfo().originPower;
        Assert.AreEqual(expectPower, battleRowAreaModel.GetCurrentPower());

        // 加入第二张普通牌，测试已存在morale时加入新牌的情况
        battleRowAreaModel.AddCard(brassNormalCardList[1]);
        Assert.AreEqual(brassNormalCardList[1].GetCardInfo().originPower + 1, brassNormalCardList[1].GetCurrentPower());
        expectPower = brassNormalCardList[0].GetCardInfo().originPower + brassNormalCardList[1].GetCardInfo().originPower + 2 +
            brassMoraleCard.GetCardInfo().originPower;
        Assert.AreEqual(expectPower, battleRowAreaModel.GetCurrentPower());

        // 移除morale牌，测试buff的清除情况。这是个英雄牌，正常不会被移除，此处仅为测试morale技能
        battleRowAreaModel.RemoveCard(brassMoraleCard);
        expectPower = brassNormalCardList[0].GetCardInfo().originPower +
            brassNormalCardList[1].GetCardInfo().originPower;
        Assert.AreEqual(expectPower, battleRowAreaModel.GetCurrentPower());
    }

    [Test]
    public void MoraleHorn()
    {
        BattleRowAreaModel battleRowAreaModel = new BattleRowAreaModel(CardBadgeType.Brass);
        int brassMoraleInfoId = 2020; // 特殊：这是个英雄牌
        int brassHornInfoId = 2036;
        List<int> brassNormalInfoIdList = new List<int> { 2037, 2038 };
        List<CardModel> brassNormalCardList = TestGenCards.GetCardList(brassNormalInfoIdList);
        CardModel brassMoraleCard = TestGenCards.GetCard(brassMoraleInfoId);
        CardModel brassHornCard = TestGenCards.GetCard(brassHornInfoId);

        // 加入第一张普通牌
        battleRowAreaModel.AddCard(brassNormalCardList[0]);
        Assert.AreEqual(brassNormalCardList[0].GetCardInfo().originPower, brassNormalCardList[0].GetCurrentPower());
        Assert.AreEqual(brassNormalCardList[0].GetCurrentPower(), battleRowAreaModel.GetCurrentPower());

        // 加入morale牌
        battleRowAreaModel.AddCard(brassMoraleCard);
        Assert.AreEqual(brassMoraleCard.GetCardInfo().originPower, brassMoraleCard.GetCurrentPower());
        int expectPower = brassNormalCardList[0].GetCardInfo().originPower + 1 +
            brassMoraleCard.GetCardInfo().originPower;
        Assert.AreEqual(expectPower, battleRowAreaModel.GetCurrentPower());

        // 加入horn牌
        battleRowAreaModel.AddCard(brassHornCard);
        Assert.AreEqual(brassHornCard.GetCardInfo().originPower + 1, brassHornCard.GetCurrentPower());
        expectPower = brassNormalCardList[0].GetCardInfo().originPower * 2 + 1 +
            brassMoraleCard.GetCardInfo().originPower +
            brassHornCard.GetCardInfo().originPower + 1;
        Assert.AreEqual(expectPower, battleRowAreaModel.GetCurrentPower());

        // 加入第二张普通牌
        battleRowAreaModel.AddCard(brassNormalCardList[1]);
        Assert.AreEqual(brassNormalCardList[1].GetCardInfo().originPower * 2 + 1, brassNormalCardList[1].GetCurrentPower());
        expectPower = brassNormalCardList[0].GetCardInfo().originPower * 2 + 1 +
            brassNormalCardList[1].GetCardInfo().originPower * 2 + 1 +
            brassMoraleCard.GetCardInfo().originPower +
            brassHornCard.GetCardInfo().originPower + 1;
        Assert.AreEqual(expectPower, battleRowAreaModel.GetCurrentPower());

        // 移除morale牌，测试buff的清除情况。这是个英雄牌，正常不会被移除，此处仅为测试morale技能
        battleRowAreaModel.RemoveCard(brassMoraleCard);
        expectPower = brassNormalCardList[0].GetCardInfo().originPower * 2 +
            brassNormalCardList[1].GetCardInfo().originPower * 2 +
            brassHornCard.GetCardInfo().originPower;
        Assert.AreEqual(expectPower, battleRowAreaModel.GetCurrentPower());

        // 移除horn牌，测试buff的清除情况
        battleRowAreaModel.RemoveCard(brassHornCard);
        expectPower = brassNormalCardList[0].GetCardInfo().originPower +
            brassNormalCardList[1].GetCardInfo().originPower;
        Assert.AreEqual(expectPower, battleRowAreaModel.GetCurrentPower());
    }

    // 测试普通牌被伞击
    [Test]
    public void NormalScorchWood()
    {
        BattleRowAreaModel battleRowAreaModel = new BattleRowAreaModel(CardBadgeType.Wood);
        List<int> woodNormalInfoIdList = new List<int> { 2009, 2017 }; // 能力分别为7、6
        List<CardModel> woodNormalCardList = TestGenCards.GetCardList(woodNormalInfoIdList);

        // 一张普通牌，总点数不到10，伞击无事发生
        battleRowAreaModel.AddCard(woodNormalCardList[0]);
        List<CardModel> scorchTargetList = battleRowAreaModel.ApplyScorchWood();
        Assert.AreEqual(0, scorchTargetList.Count);
        Assert.AreEqual(woodNormalCardList[0].GetCardInfo().originPower, battleRowAreaModel.GetCurrentPower());

        // 两张普通牌，总点数超过10，最高点数的牌被移除
        battleRowAreaModel.AddCard(woodNormalCardList[1]);
        scorchTargetList = battleRowAreaModel.ApplyScorchWood();
        Assert.AreEqual(1, scorchTargetList.Count);
        Assert.AreEqual(woodNormalCardList[1].GetCardInfo().originPower, battleRowAreaModel.GetCurrentPower());
    }

    // 测试包含英雄牌被伞击
    [Test]
    public void HeroScorchWood()
    {
        BattleRowAreaModel battleRowAreaModel = new BattleRowAreaModel(CardBadgeType.Wood);
        List<int> woodHeroInfoIdList = new List<int> { 2005, 2010 }; // 能力分别为10、7
        List<CardModel> woodHeroCardList = TestGenCards.GetCardList(woodHeroInfoIdList);
        List<int> woodNormalInfoIdList = new List<int> { 2009, 2017 }; // 能力分别为7、6
        List<CardModel> woodNormalCardList = TestGenCards.GetCardList(woodNormalInfoIdList);

        // 两张英雄牌，总点数17，伞击无事发生
        battleRowAreaModel.AddCard(woodHeroCardList[0]);
        battleRowAreaModel.AddCard(woodHeroCardList[1]);
        List<CardModel> scorchTargetList = battleRowAreaModel.ApplyScorchWood();
        Assert.AreEqual(0, scorchTargetList.Count);
        Assert.Less(10, battleRowAreaModel.GetCurrentPower());
        int expectPower = woodHeroCardList[0].GetCardInfo().originPower +
            woodHeroCardList[1].GetCardInfo().originPower;
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
        BattleRowAreaModel battleRowAreaModel = new BattleRowAreaModel(CardBadgeType.Wood);
        List<int> woodNormalInfoIdList = new List<int> { 2002, 2019, 2006 }; // 能力分别为5、5、4
        List<CardModel> woodNormalCardList = TestGenCards.GetCardList(woodNormalInfoIdList);

        // 两张普通牌能力都是5，总点数10，无事发生
        battleRowAreaModel.AddCard(woodNormalCardList[0]);
        battleRowAreaModel.AddCard(woodNormalCardList[1]);
        int expectPower = woodNormalCardList[0].GetCardInfo().originPower +
            woodNormalCardList[1].GetCardInfo().originPower;
        Assert.AreEqual(expectPower, battleRowAreaModel.GetCurrentPower());
        List<CardModel> scorchTargetList = battleRowAreaModel.ApplyScorchWood();
        Assert.AreEqual(0, scorchTargetList.Count);
        Assert.AreEqual(expectPower, battleRowAreaModel.GetCurrentPower());

        // 三张普通牌总点数14，两张5点的被移除
        battleRowAreaModel.AddCard(woodNormalCardList[2]);
        scorchTargetList = battleRowAreaModel.ApplyScorchWood();
        Assert.AreEqual(2, scorchTargetList.Count);
        expectPower = woodNormalCardList[2].GetCardInfo().originPower;
        Assert.AreEqual(expectPower, battleRowAreaModel.GetCurrentPower());
    }

    [Test]
    public void BrassScorchWood()
    {
        BattleRowAreaModel battleRowAreaModel = new BattleRowAreaModel(CardBadgeType.Brass);
        List<int> brassNormalInfoIdList = new List<int> { 2025, 2028 }; // 能力分别为6、6
        List<CardModel> brassNormalCardList = TestGenCards.GetCardList(brassNormalInfoIdList);

        // 两张普通牌能力都是6，总点数12，但不是木管行，无事发生
        battleRowAreaModel.AddCard(brassNormalCardList[0]);
        battleRowAreaModel.AddCard(brassNormalCardList[1]);
        int expectPower = brassNormalCardList[0].GetCardInfo().originPower +
            brassNormalCardList[1].GetCardInfo().originPower;
        Assert.Less(10, battleRowAreaModel.GetCurrentPower());
        List<CardModel> scorchTargetList = battleRowAreaModel.ApplyScorchWood();
        Assert.AreEqual(0, scorchTargetList.Count);
        Assert.AreEqual(expectPower, battleRowAreaModel.GetCurrentPower());
    }
}
