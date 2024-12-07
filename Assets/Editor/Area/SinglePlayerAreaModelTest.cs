using NUnit.Framework;
using System;
using System.Linq;
using System.Collections.Generic;

public class SinglePlayerAreaModelTest
{
    // 测试self初始手牌
    [Test]
    public void InitHandCardSelf()
    {
        SinglePlayerAreaModel singlePlayerAreaModel = new SinglePlayerAreaModel();
        List<int> infoIdList = Enumerable.Range(2001, 50).ToList();

        singlePlayerAreaModel.SetBackupCardInfoIdList(infoIdList);
        singlePlayerAreaModel.DrawInitHandCard();
        CardModel selectedCard = singlePlayerAreaModel.initHandRowAreaModel.cardList[0];
        Assert.AreEqual(10, singlePlayerAreaModel.initHandRowAreaModel.cardList.Count);
        Assert.AreEqual(40, singlePlayerAreaModel.backupCardList.Count);

        singlePlayerAreaModel.initHandRowAreaModel.SelectCard(selectedCard);
        singlePlayerAreaModel.ReDrawInitHandCard();
        Assert.AreEqual(0, singlePlayerAreaModel.initHandRowAreaModel.cardList.Count);
        Assert.AreEqual(10, singlePlayerAreaModel.handRowAreaModel.cardList.Count);
        Assert.AreEqual(40, singlePlayerAreaModel.backupCardList.Count);
    }

    // 测试enemy初始手牌
    [Test]
    public void InitHandCardEnemy()
    {
        SinglePlayerAreaModel singlePlayerAreaModel = new SinglePlayerAreaModel();
        List<int> infoIdList = Enumerable.Range(2001, 50).ToList();
        List<int> idList = Enumerable.Range(1, 50).ToList();
        List<int> handCardIdList = Enumerable.Range(1, 10).ToList();

        singlePlayerAreaModel.SetBackupCardInfoIdList(infoIdList, idList);
        singlePlayerAreaModel.DrawHandCards(handCardIdList);
        Assert.AreEqual(0, singlePlayerAreaModel.initHandRowAreaModel.cardList.Count);
        Assert.AreEqual(10, singlePlayerAreaModel.handRowAreaModel.cardList.Count);
        Assert.AreEqual(40, singlePlayerAreaModel.backupCardList.Count);
    }

    // 测试添加手牌
    [Test]
    public void DrawHandCard()
    {
        SinglePlayerAreaModel singlePlayerAreaModel = new SinglePlayerAreaModel();
        List<int> infoIdList = Enumerable.Range(2001, 50).ToList();
        HandRowAreaModel handRowAreaModel = singlePlayerAreaModel.handRowAreaModel;

        singlePlayerAreaModel.SetBackupCardInfoIdList(infoIdList);
        singlePlayerAreaModel.DrawHandCards(SinglePlayerAreaModel.initHandCardNum);
        singlePlayerAreaModel.DrawHandCards(2);
        Assert.AreEqual(12, handRowAreaModel.cardList.Count);
        Assert.AreEqual(38, singlePlayerAreaModel.backupCardList.Count);
    }

    // 测试enemy添加手牌
    [Test]
    public void EnemyDrawHandCard()
    {
        SinglePlayerAreaModel singlePlayerAreaModel = new SinglePlayerAreaModel();
        List<int> infoIdList = Enumerable.Range(2001, 50).ToList();
        List<int> idList = Enumerable.Range(1, 50).ToList();
        HandRowAreaModel handRowAreaModel = singlePlayerAreaModel.handRowAreaModel;

        singlePlayerAreaModel.SetBackupCardInfoIdList(infoIdList, idList);
        List<int> initHandCardIdList = Enumerable.Range(1, 10).ToList();
        singlePlayerAreaModel.DrawHandCards(initHandCardIdList);
        initHandCardIdList = Enumerable.Range(11, 2).ToList();
        singlePlayerAreaModel.DrawHandCards(initHandCardIdList);
        Assert.AreEqual(12, handRowAreaModel.cardList.Count);
        Assert.AreEqual(38, singlePlayerAreaModel.backupCardList.Count);
    }

    // 测试添加卡牌到对战区
    [Test]
    public void AddCard()
    {
        SinglePlayerAreaModel singlePlayerAreaModel = new SinglePlayerAreaModel();
        // 分别木管5，铜管4，打击7
        List<int> infoIdList = new List<int> { 2002, 2024, 2044 };
        List<CardModel> cardList = TestGenCards.GetCardList(infoIdList);

        // 打出三张卡牌
        singlePlayerAreaModel.AddBattleAreaCard(cardList[0]);
        singlePlayerAreaModel.AddBattleAreaCard(cardList[1]);
        singlePlayerAreaModel.AddBattleAreaCard(cardList[2]);

        Assert.AreEqual(1, singlePlayerAreaModel.woodRowAreaModel.cardList.Count);
        Assert.AreEqual(1, singlePlayerAreaModel.brassRowAreaModel.cardList.Count);
        Assert.AreEqual(1, singlePlayerAreaModel.percussionRowAreaModel.cardList.Count);
        Assert.AreEqual(16, singlePlayerAreaModel.GetCurrentPower());
    }

    // 测试添加bond卡牌
    [Test]
    public void AddBond()
    {
        // 基础点数为4
        List<int> bondInfoIdList = new List<int> { 2006, 2007, 2008 };
        List<CardModel> bondCardList = TestGenCards.GetCardList(bondInfoIdList);

        SinglePlayerAreaModel singlePlayerAreaModel = new SinglePlayerAreaModel();
        singlePlayerAreaModel.AddBattleAreaCard(bondCardList[0]);
        Assert.AreEqual(4, singlePlayerAreaModel.GetCurrentPower());
        singlePlayerAreaModel.AddBattleAreaCard(bondCardList[1]);
        Assert.AreEqual(8 + 8, singlePlayerAreaModel.GetCurrentPower());
        singlePlayerAreaModel.AddBattleAreaCard(bondCardList[2]);
        Assert.AreEqual(12 + 12 + 12, singlePlayerAreaModel.GetCurrentPower());
    }

    [Test]
    public void AddMuster()
    {
        // 基础点数为4。便于测试，使用20张相同卡牌，10张手牌，10张备选牌
        List<int> musterInfoIdList = Enumerable.Repeat(2011, 20).ToList();
        SinglePlayerAreaModel singlePlayerAreaModel = new SinglePlayerAreaModel();
        HandRowAreaModel handRowAreaModel = singlePlayerAreaModel.handRowAreaModel;

        singlePlayerAreaModel.SetBackupCardInfoIdList(musterInfoIdList);
        singlePlayerAreaModel.DrawHandCards(SinglePlayerAreaModel.initHandCardNum);
        CardModel selectedCard = handRowAreaModel.cardList[0];
        handRowAreaModel.RemoveCard(selectedCard);

        singlePlayerAreaModel.AddBattleAreaCard(selectedCard);
        Assert.AreEqual(80, singlePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(0, handRowAreaModel.cardList.Count);
        Assert.AreEqual(0, singlePlayerAreaModel.backupCardList.Count);
    }
}
