using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

public class PlaySceneModelTest
{
    private static string TAG = "PlaySceneModelTest";

    private PlaySceneModel playSceneModel;

    [SetUp]
    public void SetUp()
    {
        KLog.I(TAG, "SetUp");
        playSceneModel = new PlaySceneModel();
    }

    [TearDown]
    public void Teardown()
    {
        KLog.I(TAG, "Teardown");
    }

    // 测试双方打出普通牌的一局流程
    [Test]
    public void Normal()
    {
        // 木管普通牌，能力6
        List<int> selfInfoIdList = Enumerable.Repeat(2017, 20).ToList();
        List<int> enemyInfoIdList = Enumerable.Repeat(2017, 20).ToList();
        playSceneModel.SetBackupCardInfoIdList(selfInfoIdList, enemyInfoIdList);
        HandRowAreaModel selfHandRowAreaModel = playSceneModel.selfSinglePlayerAreaModel.handRowAreaModel;
        HandRowAreaModel enemyHandRowAreaModel = playSceneModel.enemySinglePlayerAreaModel.handRowAreaModel;

        // 开始对局
        playSceneModel.StartSet(true);

        // 本方打牌
        Assert.AreEqual(true, playSceneModel.IsTurn(true));
        Assert.AreEqual(false, playSceneModel.IsTurn(false));
        CardModel selectedCard = selfHandRowAreaModel.cardList[0];
        playSceneModel.ChooseCard(selectedCard, true);
        Assert.AreEqual(9, selfHandRowAreaModel.cardList.Count);
        Assert.AreEqual(10, enemyHandRowAreaModel.cardList.Count);
        Assert.AreEqual(6, playSceneModel.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(0, playSceneModel.enemySinglePlayerAreaModel.GetCurrentPower());

        // 对方打牌
        Assert.AreEqual(false, playSceneModel.IsTurn(true));
        Assert.AreEqual(true, playSceneModel.IsTurn(false));
        selectedCard = enemyHandRowAreaModel.cardList[0];
        playSceneModel.ChooseCard(selectedCard, false);
        Assert.AreEqual(9, selfHandRowAreaModel.cardList.Count);
        Assert.AreEqual(9, enemyHandRowAreaModel.cardList.Count);
        Assert.AreEqual(6, playSceneModel.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(6, playSceneModel.enemySinglePlayerAreaModel.GetCurrentPower());

        // 双方pass，结束本局
        Assert.AreEqual(true, playSceneModel.IsTurn(true));
        Assert.AreEqual(false, playSceneModel.IsTurn(false));
        playSceneModel.Pass(true);
        Assert.AreEqual(false, playSceneModel.IsTurn(true));
        Assert.AreEqual(true, playSceneModel.IsTurn(false));
        playSceneModel.Pass(false);
        // 本局结束，都不能出牌
        Assert.AreEqual(false, playSceneModel.IsTurn(true));
        Assert.AreEqual(false, playSceneModel.IsTurn(false));
    }

    // 测试双方打出间谍牌
    [Test]
    public void Spy()
    {
        // 铜管间谍牌，能力4
        List<int> selfInfoIdList = Enumerable.Repeat(2030, 20).ToList();
        List<int> enemyInfoIdList = Enumerable.Repeat(2030, 20).ToList();
        playSceneModel.SetBackupCardInfoIdList(selfInfoIdList, enemyInfoIdList);
        HandRowAreaModel selfHandRowAreaModel = playSceneModel.selfSinglePlayerAreaModel.handRowAreaModel;
        HandRowAreaModel enemyHandRowAreaModel = playSceneModel.enemySinglePlayerAreaModel.handRowAreaModel;

        // 开始对局
        playSceneModel.StartSet(true);

        // 本方打牌
        Assert.AreEqual(true, playSceneModel.IsTurn(true));
        Assert.AreEqual(false, playSceneModel.IsTurn(false));
        CardModel selectedCard = selfHandRowAreaModel.cardList[0];
        playSceneModel.ChooseCard(selectedCard, true);
        Assert.AreEqual(9 + 2, selfHandRowAreaModel.cardList.Count);
        Assert.AreEqual(10, enemyHandRowAreaModel.cardList.Count);
        Assert.AreEqual(0, playSceneModel.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(4, playSceneModel.enemySinglePlayerAreaModel.GetCurrentPower());

        // 对方打牌
        Assert.AreEqual(false, playSceneModel.IsTurn(true));
        Assert.AreEqual(true, playSceneModel.IsTurn(false));
        selectedCard = enemyHandRowAreaModel.cardList[0];
        playSceneModel.ChooseCard(selectedCard, false);
        Assert.AreEqual(9 + 2, selfHandRowAreaModel.cardList.Count);
        Assert.AreEqual(9 + 2, enemyHandRowAreaModel.cardList.Count);
        Assert.AreEqual(4, playSceneModel.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(4, playSceneModel.enemySinglePlayerAreaModel.GetCurrentPower());

        // 双方pass，结束本局
        Assert.AreEqual(true, playSceneModel.IsTurn(true));
        Assert.AreEqual(false, playSceneModel.IsTurn(false));
        playSceneModel.Pass(true);
        Assert.AreEqual(false, playSceneModel.IsTurn(true));
        Assert.AreEqual(true, playSceneModel.IsTurn(false));
        playSceneModel.Pass(false);
        // 本局结束，都不能出牌
        Assert.AreEqual(false, playSceneModel.IsTurn(true));
        Assert.AreEqual(false, playSceneModel.IsTurn(false));
    }

    // 测试伞击技能。主要测试已在BattleRowAreaModelTest中完成，此处简单测试整体流程
    [Test]
    public void ScorchWood()
    {
        // 分别是普通牌（5），伞击牌（7）
        List<int> selfInfoIdList = new List<int> { 2004, 2010 };
        // 两张木管普通牌，能力分别为5、7
        List<int> enemyInfoIdList = new List<int> { 2002, 2009 };
        playSceneModel.SetBackupCardInfoIdList(selfInfoIdList, enemyInfoIdList);
        HandRowAreaModel selfHandRowAreaModel = playSceneModel.selfSinglePlayerAreaModel.handRowAreaModel;
        HandRowAreaModel enemyHandRowAreaModel = playSceneModel.enemySinglePlayerAreaModel.handRowAreaModel;

        // 开始对局，对方先出牌
        playSceneModel.StartSet(false);

        CardModel normalCard;
        CardModel scorchCard;
        if (selfHandRowAreaModel.cardList[0].cardInfo.ability == CardAbility.ScorchWood) {
            normalCard = selfHandRowAreaModel.cardList[1];
            scorchCard = selfHandRowAreaModel.cardList[0];
        } else {
            normalCard = selfHandRowAreaModel.cardList[0];
            scorchCard = selfHandRowAreaModel.cardList[1];
        }

        playSceneModel.ChooseCard(enemyHandRowAreaModel.cardList[0], false);
        playSceneModel.ChooseCard(normalCard, true);
        playSceneModel.ChooseCard(enemyHandRowAreaModel.cardList[0], false);

        // 本方打出伞击牌，移除对面能力为7的牌
        playSceneModel.ChooseCard(scorchCard, true);
        Assert.AreEqual(12, playSceneModel.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(5, playSceneModel.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(1, playSceneModel.enemySinglePlayerAreaModel.discardAreaModel.normalCardList.Count);
    }

    // 测试复活牌技能：无复活目标
    [Test]
    public void MedicNoTarget()
    {
        // 能力5，非英雄牌
        List<int> selfInfoIdList = Enumerable.Repeat(2027, 1).ToList();
        List<int> enemyInfoIdList = Enumerable.Repeat(2027, 1).ToList();
        playSceneModel.SetBackupCardInfoIdList(selfInfoIdList, enemyInfoIdList);
        HandRowAreaModel selfHandRowAreaModel = playSceneModel.selfSinglePlayerAreaModel.handRowAreaModel;
        DiscardAreaModel selfDiscardAreaModel = playSceneModel.selfSinglePlayerAreaModel.discardAreaModel;

        // 开始对局
        playSceneModel.StartSet(true);

        // 测试用，给弃牌区加一张英雄牌
        CardModel heroCard = TestGenCards.GetCard(2042);
        selfDiscardAreaModel.AddCard(heroCard);
        // 打出复活牌，但没有可复活的，轮到对方出牌
        CardModel selectedCard = selfHandRowAreaModel.cardList[0];
        playSceneModel.ChooseCard(selectedCard, true);
        Assert.AreEqual(5, playSceneModel.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(0, playSceneModel.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(false, playSceneModel.IsTurn(true));
        Assert.AreEqual(true, playSceneModel.IsTurn(false));
    }

    // 测试复活牌技能：复活一张普通牌
    [Test]
    public void MedicNormal()
    {
        // 能力5，非英雄牌
        List<int> selfInfoIdList = Enumerable.Repeat(2027, 1).ToList();
        List<int> enemyInfoIdList = Enumerable.Repeat(2027, 1).ToList();
        playSceneModel.SetBackupCardInfoIdList(selfInfoIdList, enemyInfoIdList);
        HandRowAreaModel selfHandRowAreaModel = playSceneModel.selfSinglePlayerAreaModel.handRowAreaModel;
        DiscardAreaModel selfDiscardAreaModel = playSceneModel.selfSinglePlayerAreaModel.discardAreaModel;

        // 开始对局
        playSceneModel.StartSet(true);

        // 测试用，给弃牌区加一张普通牌，能力5
        CardModel normalCard = TestGenCards.GetCard(2043);
        selfDiscardAreaModel.AddCard(normalCard);
        // 打出复活牌，等待本方进一步操作
        CardModel selectedCard = selfHandRowAreaModel.cardList[0];
        playSceneModel.ChooseCard(selectedCard, true);
        Assert.AreEqual(5, playSceneModel.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(0, playSceneModel.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(true, playSceneModel.IsTurn(true));
        Assert.AreEqual(false, playSceneModel.IsTurn(false));

        // 选择复活普通牌，然后轮到对方出牌
        playSceneModel.ChooseCard(normalCard, true);
        Assert.AreEqual(10, playSceneModel.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(0, playSceneModel.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(false, playSceneModel.IsTurn(true));
        Assert.AreEqual(true, playSceneModel.IsTurn(false));
    }

    // 测试复活牌技能：复活一张复活牌
    [Test]
    public void MedicMedic()
    {
        // 能力5，非英雄牌
        List<int> selfInfoIdList = Enumerable.Repeat(2027, 1).ToList();
        List<int> enemyInfoIdList = Enumerable.Repeat(2027, 1).ToList();
        playSceneModel.SetBackupCardInfoIdList(selfInfoIdList, enemyInfoIdList);
        HandRowAreaModel selfHandRowAreaModel = playSceneModel.selfSinglePlayerAreaModel.handRowAreaModel;
        DiscardAreaModel selfDiscardAreaModel = playSceneModel.selfSinglePlayerAreaModel.discardAreaModel;

        // 开始对局
        playSceneModel.StartSet(true);

        // 测试用，给弃牌区加一张普通牌和复活牌，能力都是5
        CardModel medicCard = TestGenCards.GetCard(2027);
        CardModel normalCard = TestGenCards.GetCard(2043);
        selfDiscardAreaModel.AddCard(medicCard);
        selfDiscardAreaModel.AddCard(normalCard);
        // 打出复活牌，等待本方进一步操作
        CardModel selectedCard = selfHandRowAreaModel.cardList[0];
        playSceneModel.ChooseCard(selectedCard, true);
        Assert.AreEqual(5, playSceneModel.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(0, playSceneModel.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(true, playSceneModel.IsTurn(true));
        Assert.AreEqual(false, playSceneModel.IsTurn(false));

        // 选择复活一张复活牌，然后仍是本方操作
        playSceneModel.ChooseCard(medicCard, true);
        Assert.AreEqual(10, playSceneModel.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(0, playSceneModel.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(true, playSceneModel.IsTurn(true));
        Assert.AreEqual(false, playSceneModel.IsTurn(false));

        // 选择复活普通牌，然后轮到对方出牌
        playSceneModel.ChooseCard(normalCard, true);
        Assert.AreEqual(15, playSceneModel.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(0, playSceneModel.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(false, playSceneModel.IsTurn(true));
        Assert.AreEqual(true, playSceneModel.IsTurn(false));
    }

    // 测试攻击牌技能：无可攻击目标
    [Test]
    public void AttckNoTarget()
    {
        // 攻击牌，攻击力4，能力10
        List<int> selfInfoIdList = new List<int> { 2023 };
        // 英雄牌，能力10
        List<int> enemyInfoIdList = new List<int> { 2005 };
        playSceneModel.SetBackupCardInfoIdList(selfInfoIdList, enemyInfoIdList);
        HandRowAreaModel selfHandRowAreaModel = playSceneModel.selfSinglePlayerAreaModel.handRowAreaModel;
        HandRowAreaModel enemyHandRowAreaModel = playSceneModel.enemySinglePlayerAreaModel.handRowAreaModel;

        // 开始对局，对方先行
        playSceneModel.StartSet(false);
        playSceneModel.ChooseCard(enemyHandRowAreaModel.cardList[0], false);
        // 本方打出攻击牌，但没有可攻击对象
        playSceneModel.ChooseCard(selfHandRowAreaModel.cardList[0], true);
        Assert.AreEqual(10, playSceneModel.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(10, playSceneModel.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(false, playSceneModel.IsTurn(true));
        Assert.AreEqual(true, playSceneModel.IsTurn(false));
    }

    // 测试攻击牌技能：攻击普通牌
    [Test]
    public void AttackNormal()
    {
        // 攻击牌，攻击力4，能力10
        List<int> selfInfoIdList = new List<int> { 2023 };
        // 普通牌，能力5
        List<int> enemyInfoIdList = new List<int> { 2004 };
        playSceneModel.SetBackupCardInfoIdList(selfInfoIdList, enemyInfoIdList);
        HandRowAreaModel selfHandRowAreaModel = playSceneModel.selfSinglePlayerAreaModel.handRowAreaModel;
        HandRowAreaModel enemyHandRowAreaModel = playSceneModel.enemySinglePlayerAreaModel.handRowAreaModel;

        // 开始对局，对方先行
        playSceneModel.StartSet(false);
        CardModel enemyCard = enemyHandRowAreaModel.cardList[0];
        playSceneModel.ChooseCard(enemyCard, false);
        // 本方打出攻击牌，并选择攻击对象
        playSceneModel.ChooseCard(selfHandRowAreaModel.cardList[0], true);
        playSceneModel.ChooseCard(enemyCard, true);
        Assert.AreEqual(10, playSceneModel.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(1, playSceneModel.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(false, playSceneModel.IsTurn(true));
        Assert.AreEqual(true, playSceneModel.IsTurn(false));
    }

    // 测试攻击牌技能：攻击导致卡牌被移除
    [Test]
    public void AttackDead()
    {

        // 攻击牌，攻击力4，能力10
        List<int> selfInfoIdList = new List<int> { 2023 };
        // 普通牌，能力4
        List<int> enemyInfoIdList = new List<int> { 2007 };
        playSceneModel.SetBackupCardInfoIdList(selfInfoIdList, enemyInfoIdList);
        HandRowAreaModel selfHandRowAreaModel = playSceneModel.selfSinglePlayerAreaModel.handRowAreaModel;
        HandRowAreaModel enemyHandRowAreaModel = playSceneModel.enemySinglePlayerAreaModel.handRowAreaModel;

        // 开始对局，对方先行
        playSceneModel.StartSet(false);
        CardModel enemyCard = enemyHandRowAreaModel.cardList[0];
        playSceneModel.ChooseCard(enemyCard, false);
        // 本方打出攻击牌，并选择攻击对象
        playSceneModel.ChooseCard(selfHandRowAreaModel.cardList[0], true);
        playSceneModel.ChooseCard(enemyCard, true);
        Assert.AreEqual(10, playSceneModel.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(0, playSceneModel.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(0, playSceneModel.enemySinglePlayerAreaModel.woodRowAreaModel.cardList.Count);
        Assert.AreEqual(1, playSceneModel.enemySinglePlayerAreaModel.discardAreaModel.normalCardList.Count);
        Assert.AreEqual(false, playSceneModel.IsTurn(true));
        Assert.AreEqual(true, playSceneModel.IsTurn(false));
    }

    // 测试Tunning技能
    [Test]
    public void Tunning()
    {
        // 攻击牌，攻击力4，能力10
        List<int> selfInfoIdList = new List<int> { 2023 };
        // Tunning牌（7）、普通牌（5）
        List<int> enemyInfoIdList = new List<int> { 2001, 2002 };
        playSceneModel.SetBackupCardInfoIdList(selfInfoIdList, enemyInfoIdList);
        HandRowAreaModel selfHandRowAreaModel = playSceneModel.selfSinglePlayerAreaModel.handRowAreaModel;
        HandRowAreaModel enemyHandRowAreaModel = playSceneModel.enemySinglePlayerAreaModel.handRowAreaModel;

        // 开始对局，对方先出牌
        playSceneModel.StartSet(false);

        CardModel normalCard;
        CardModel tunningCard;
        if (enemyHandRowAreaModel.cardList[0].cardInfo.ability == CardAbility.Tunning) {
            normalCard = enemyHandRowAreaModel.cardList[1];
            tunningCard = enemyHandRowAreaModel.cardList[0];
        } else {
            normalCard = enemyHandRowAreaModel.cardList[0];
            tunningCard = enemyHandRowAreaModel.cardList[1];
        }

        playSceneModel.ChooseCard(normalCard, false);
        // 本方打出攻击牌
        playSceneModel.ChooseCard(selfHandRowAreaModel.cardList[0], true);
        playSceneModel.ChooseCard(normalCard, true);
        Assert.AreEqual(10, playSceneModel.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(1, playSceneModel.enemySinglePlayerAreaModel.GetCurrentPower());
        // 对方打出Tunning牌
        playSceneModel.ChooseCard(tunningCard, false);
        Assert.AreEqual(10, playSceneModel.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(12, playSceneModel.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(true, playSceneModel.IsTurn(true));
        Assert.AreEqual(false, playSceneModel.IsTurn(false));
    }
}
