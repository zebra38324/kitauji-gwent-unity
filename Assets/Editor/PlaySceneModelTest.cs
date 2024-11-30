using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

public class PlaySceneModelTest
{
    private static string TAG = "PlaySceneModelTest";

    private PlaySceneModel hostModel; // 以下测试都以host为self的视角

    private PlaySceneModel playerModel;

    [SetUp]
    public void SetUp()
    {
        KLog.I(TAG, "SetUp");
        // 测试用，先把CardGenerator的资源加载了
        CardGenerator cardGenerator = new CardGenerator();
        cardGenerator.GetCard(2001);
        // 初始化
        hostModel = new PlaySceneModel();
        playerModel = new PlaySceneModel(false);
        hostModel.battleModel.SendToEnemyFunc += playerModel.battleModel.AddEnemyActionMsg;
        playerModel.battleModel.SendToEnemyFunc += hostModel.battleModel.AddEnemyActionMsg;
    }

    [TearDown]
    public void Teardown()
    {
        KLog.I(TAG, "Teardown");
    }

    // 检测curState，不满足时等待一段时间
    private void CheckCurState(PlaySceneModel model, PlayStateTracker.State expectedState)
    {
        long startTs = KTime.CurrentMill();
        long timeout = 1000; // 1s
        while (model.tracker.curState != expectedState) {
            if (KTime.CurrentMill() - startTs > timeout) {
                break;
            }
            Thread.Sleep(1);
        }
        Assert.AreEqual(expectedState, model.tracker.curState);
    }

    // 测试双方打出普通牌的一局流程
    [Test]
    public void Normal()
    {
        Assert.AreEqual(false, hostModel.hasEnemyUpdate);
        Assert.AreEqual(false, playerModel.hasEnemyUpdate);
        Thread hostThread = new Thread(() => {
            HandRowAreaModel selfHandRowAreaModel = hostModel.selfSinglePlayerAreaModel.handRowAreaModel;
            HandRowAreaModel enemyHandRowAreaModel = hostModel.enemySinglePlayerAreaModel.handRowAreaModel;
            List<int> hostInfoIdList = Enumerable.Repeat(2017, 20).ToList();
            Assert.AreEqual(0, hostModel.tracker.stateChangeTs);
            // backup
            hostModel.SetBackupCardInfoIdList(hostInfoIdList);
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_INIT_HAND_CARD);
            Assert.AreEqual(true, hostModel.hasEnemyUpdate);
            // draw card
            hostModel.DrawInitHandCard();
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_START);
            // 开始对局
            hostModel.StartSet(true);
            long currentTs = KTime.CurrentMill();
            Assert.Greater(hostModel.tracker.stateChangeTs, currentTs - 50);
            Assert.Less(hostModel.tracker.stateChangeTs, currentTs + 50);
            // host出牌
            Assert.AreEqual(true, hostModel.IsTurn(true));
            Assert.AreEqual(false, hostModel.IsTurn(false));
            CardModel selectedCard = selfHandRowAreaModel.cardList[0];
            hostModel.ChooseCard(selectedCard);
            Assert.AreEqual(9, selfHandRowAreaModel.cardList.Count);
            Assert.AreEqual(10, enemyHandRowAreaModel.cardList.Count);
            Assert.AreEqual(6, hostModel.selfSinglePlayerAreaModel.GetCurrentPower());
            Assert.AreEqual(0, hostModel.enemySinglePlayerAreaModel.GetCurrentPower());
            // 等待player出牌
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            // host pass
            Assert.AreEqual(true, hostModel.IsTurn(true));
            Assert.AreEqual(false, hostModel.IsTurn(false));
            hostModel.Pass();
            // 等待player pass。双方pass，结束本局
            CheckCurState(hostModel, PlayStateTracker.State.SET_FINFISH);
            // 本局结束，都不能出牌
            Assert.AreEqual(false, hostModel.IsTurn(true));
            Assert.AreEqual(false, hostModel.IsTurn(false));
        });
        Thread playerThread = new Thread(() => {
            HandRowAreaModel selfHandRowAreaModel = playerModel.selfSinglePlayerAreaModel.handRowAreaModel;
            HandRowAreaModel enemyHandRowAreaModel = playerModel.enemySinglePlayerAreaModel.handRowAreaModel;
            List<int> playerInfoIdList = Enumerable.Repeat(2017, 20).ToList();
            Assert.AreEqual(0, playerModel.tracker.stateChangeTs);
            playerModel.SetBackupCardInfoIdList(playerInfoIdList);
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_INIT_HAND_CARD);
            Assert.AreEqual(true, playerModel.hasEnemyUpdate);
            playerModel.DrawInitHandCard();
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_START);
            // 开始对局
            playerModel.StartSet(false);
            // 等待host出牌
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            long currentTs = KTime.CurrentMill();
            Assert.Greater(playerModel.tracker.stateChangeTs, currentTs - 50);
            Assert.Less(playerModel.tracker.stateChangeTs, currentTs + 50);
            // player出牌
            Assert.AreEqual(true, playerModel.IsTurn(true)); // playerModel视角
            Assert.AreEqual(false, playerModel.IsTurn(false));
            CardModel selectedCard = selfHandRowAreaModel.cardList[0];
            playerModel.ChooseCard(selectedCard);
            Assert.AreEqual(9, selfHandRowAreaModel.cardList.Count);
            Assert.AreEqual(9, enemyHandRowAreaModel.cardList.Count);
            Assert.AreEqual(6, playerModel.selfSinglePlayerAreaModel.GetCurrentPower());
            Assert.AreEqual(6, playerModel.enemySinglePlayerAreaModel.GetCurrentPower());
            // 等待host
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            // player pass
            Assert.AreEqual(true, playerModel.IsTurn(true));
            Assert.AreEqual(false, playerModel.IsTurn(false));
            playerModel.Pass();
            // 双方pass，结束本局
            CheckCurState(hostModel, PlayStateTracker.State.SET_FINFISH);
            // 本局结束，都不能出牌
            Assert.AreEqual(false, playerModel.IsTurn(true));
            Assert.AreEqual(false, playerModel.IsTurn(false));
        });
        hostThread.Start();
        playerThread.Start();
        hostThread.Join();
        playerThread.Join();
    }

    // 测试双方打出间谍牌
    [Test]
    public void Spy()
    {
        Thread hostThread = new Thread(() => {
            HandRowAreaModel selfHandRowAreaModel = hostModel.selfSinglePlayerAreaModel.handRowAreaModel;
            HandRowAreaModel enemyHandRowAreaModel = hostModel.enemySinglePlayerAreaModel.handRowAreaModel;
            // 铜管间谍牌，能力4
            List<int> hostInfoIdList = Enumerable.Repeat(2030, 20).ToList();
            // backup
            hostModel.SetBackupCardInfoIdList(hostInfoIdList);
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_INIT_HAND_CARD);
            // draw card
            hostModel.DrawInitHandCard();
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_START);
            // 开始对局
            hostModel.StartSet(true);
            // host出牌
            Assert.AreEqual(true, hostModel.IsTurn(true));
            Assert.AreEqual(false, hostModel.IsTurn(false));
            CardModel selectedCard = selfHandRowAreaModel.cardList[0];
            hostModel.ChooseCard(selectedCard);
            Assert.AreEqual(9 + 2, selfHandRowAreaModel.cardList.Count);
            Assert.AreEqual(10, enemyHandRowAreaModel.cardList.Count);
            Assert.AreEqual(0, hostModel.selfSinglePlayerAreaModel.GetCurrentPower());
            Assert.AreEqual(4, hostModel.enemySinglePlayerAreaModel.GetCurrentPower());
            // 等待player出牌
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            // host pass
            Assert.AreEqual(true, hostModel.IsTurn(true));
            Assert.AreEqual(false, hostModel.IsTurn(false));
            hostModel.Pass();
            // 等待player pass。双方pass，结束本局
            CheckCurState(hostModel, PlayStateTracker.State.SET_FINFISH);
            // 本局结束，都不能出牌
            Assert.AreEqual(false, hostModel.IsTurn(true));
            Assert.AreEqual(false, hostModel.IsTurn(false));
        });
        Thread playerThread = new Thread(() => {
            HandRowAreaModel selfHandRowAreaModel = playerModel.selfSinglePlayerAreaModel.handRowAreaModel;
            HandRowAreaModel enemyHandRowAreaModel = playerModel.enemySinglePlayerAreaModel.handRowAreaModel;
            // 铜管间谍牌，能力4
            List<int> playerInfoIdList = Enumerable.Repeat(2030, 20).ToList();
            playerModel.SetBackupCardInfoIdList(playerInfoIdList);
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_INIT_HAND_CARD);
            playerModel.DrawInitHandCard();
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_START);
            // 开始对局
            playerModel.StartSet(false);
            // 等待host出牌
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            // player出牌
            Assert.AreEqual(true, playerModel.IsTurn(true)); // playerModel视角
            Assert.AreEqual(false, playerModel.IsTurn(false));
            CardModel selectedCard = selfHandRowAreaModel.cardList[0];
            playerModel.ChooseCard(selectedCard);
            Assert.AreEqual(9 + 2, selfHandRowAreaModel.cardList.Count);
            Assert.AreEqual(9 + 2, enemyHandRowAreaModel.cardList.Count);
            Assert.AreEqual(4, playerModel.selfSinglePlayerAreaModel.GetCurrentPower());
            Assert.AreEqual(4, playerModel.enemySinglePlayerAreaModel.GetCurrentPower());
            // 等待host
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            // player pass
            Assert.AreEqual(true, playerModel.IsTurn(true));
            Assert.AreEqual(false, playerModel.IsTurn(false));
            playerModel.Pass();
            // 双方pass，结束本局
            CheckCurState(hostModel, PlayStateTracker.State.SET_FINFISH);
            // 本局结束，都不能出牌
            Assert.AreEqual(false, playerModel.IsTurn(true));
            Assert.AreEqual(false, playerModel.IsTurn(false));
        });
        hostThread.Start();
        playerThread.Start();
        hostThread.Join();
        playerThread.Join();
    }

    // 测试伞击技能。主要测试已在BattleRowAreaModelTest中完成，此处简单测试整体流程
    [Test]
    public void ScorchWood()
    {
        Thread hostThread = new Thread(() => {
            HandRowAreaModel selfHandRowAreaModel = hostModel.selfSinglePlayerAreaModel.handRowAreaModel;
            HandRowAreaModel enemyHandRowAreaModel = hostModel.enemySinglePlayerAreaModel.handRowAreaModel;
            // 分别是普通牌（5），伞击牌（7）
            List<int> hostInfoIdList = new List<int> { 2004, 2010 };
            // backup
            hostModel.SetBackupCardInfoIdList(hostInfoIdList);
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_INIT_HAND_CARD);
            // draw card
            hostModel.DrawInitHandCard();
            CardModel normalCard;
            CardModel scorchCard;
            if (selfHandRowAreaModel.cardList[0].cardInfo.ability == CardAbility.ScorchWood) {
                normalCard = selfHandRowAreaModel.cardList[1];
                scorchCard = selfHandRowAreaModel.cardList[0];
            } else {
                normalCard = selfHandRowAreaModel.cardList[0];
                scorchCard = selfHandRowAreaModel.cardList[1];
            }
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_START);
            // 开始对局，对方先出牌
            hostModel.StartSet(false);
            // 等待player出牌
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            // host出牌
            hostModel.ChooseCard(normalCard);
            // 等待player出牌
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            // host出牌
            hostModel.ChooseCard(scorchCard); // 打出伞击牌，移除对面能力为7的牌
            Assert.AreEqual(12, hostModel.selfSinglePlayerAreaModel.GetCurrentPower());
            Assert.AreEqual(5, hostModel.enemySinglePlayerAreaModel.GetCurrentPower());
            Assert.AreEqual(1, hostModel.enemySinglePlayerAreaModel.discardAreaModel.cardList.Count);
        });
        Thread playerThread = new Thread(() => {
            HandRowAreaModel selfHandRowAreaModel = playerModel.selfSinglePlayerAreaModel.handRowAreaModel;
            HandRowAreaModel enemyHandRowAreaModel = playerModel.enemySinglePlayerAreaModel.handRowAreaModel;
            // 两张木管普通牌，能力分别为5、7
            List<int> playerInfoIdList = new List<int> { 2002, 2009 };
            playerModel.SetBackupCardInfoIdList(playerInfoIdList);
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_INIT_HAND_CARD);
            playerModel.DrawInitHandCard();
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_START);
            // 开始对局
            playerModel.StartSet(true);
            // player出牌
            playerModel.ChooseCard(selfHandRowAreaModel.cardList[0]);
            // 等待host出牌
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            // player出牌
            playerModel.ChooseCard(selfHandRowAreaModel.cardList[0]);
            // 等待host出牌
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            Assert.AreEqual(5, playerModel.selfSinglePlayerAreaModel.GetCurrentPower());
            Assert.AreEqual(12, playerModel.enemySinglePlayerAreaModel.GetCurrentPower());
            Assert.AreEqual(1, playerModel.selfSinglePlayerAreaModel.discardAreaModel.cardList.Count);
        });
        hostThread.Start();
        playerThread.Start();
        hostThread.Join();
        playerThread.Join();
    }

    // 测试复活牌技能：无复活目标
    [Test]
    public void MedicNoTarget()
    {
        Thread hostThread = new Thread(() => {
            HandRowAreaModel selfHandRowAreaModel = hostModel.selfSinglePlayerAreaModel.handRowAreaModel;
            HandRowAreaModel enemyHandRowAreaModel = hostModel.enemySinglePlayerAreaModel.handRowAreaModel;
            // 能力5，非英雄牌
            List<int> hostInfoIdList = new List<int> { 2027 };
            // backup
            hostModel.SetBackupCardInfoIdList(hostInfoIdList);
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_INIT_HAND_CARD);
            // draw card
            hostModel.DrawInitHandCard();
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_START);
            // 开始对局
            hostModel.StartSet(true);
            // 测试用，给弃牌区加一张英雄牌
            hostModel.selfSinglePlayerAreaModel.discardAreaModel.AddCard(TestGenCards.GetCard(2042));
            // host出牌，打出复活牌，但没有可复活的，轮到对方出牌
            hostModel.ChooseCard(selfHandRowAreaModel.cardList[0]);
            Assert.AreEqual(5, hostModel.selfSinglePlayerAreaModel.GetCurrentPower());
            Assert.AreEqual(0, hostModel.enemySinglePlayerAreaModel.GetCurrentPower());
            Assert.AreEqual(false, hostModel.IsTurn(true));
            Assert.AreEqual(true, hostModel.IsTurn(false));
        });
        Thread playerThread = new Thread(() => {
            HandRowAreaModel selfHandRowAreaModel = playerModel.selfSinglePlayerAreaModel.handRowAreaModel;
            HandRowAreaModel enemyHandRowAreaModel = playerModel.enemySinglePlayerAreaModel.handRowAreaModel;
            // 能力5，非英雄牌
            List<int> playerInfoIdList = new List<int> { 2027 };
            playerModel.SetBackupCardInfoIdList(playerInfoIdList);
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_INIT_HAND_CARD);
            playerModel.DrawInitHandCard();
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_START);
            // 开始对局
            playerModel.StartSet(false);
            // 测试用，给弃牌区加一张英雄牌
            playerModel.enemySinglePlayerAreaModel.discardAreaModel.AddCard(TestGenCards.GetCard(2042));
            // 等待host出牌
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            Assert.AreEqual(0, playerModel.selfSinglePlayerAreaModel.GetCurrentPower());
            Assert.AreEqual(5, playerModel.enemySinglePlayerAreaModel.GetCurrentPower());
            Assert.AreEqual(true, playerModel.IsTurn(true));
            Assert.AreEqual(false, playerModel.IsTurn(false));
        });
        hostThread.Start();
        playerThread.Start();
        hostThread.Join();
        playerThread.Join();
    }

    // 测试复活牌技能：复活一张普通牌
    [Test]
    public void MedicNormal()
    {
        Thread hostThread = new Thread(() => {
            HandRowAreaModel selfHandRowAreaModel = hostModel.selfSinglePlayerAreaModel.handRowAreaModel;
            HandRowAreaModel enemyHandRowAreaModel = hostModel.enemySinglePlayerAreaModel.handRowAreaModel;
            // 能力5，非英雄复活牌。普通牌能力6
            List<int> hostInfoIdList = new List<int> { 2027, 2028 };
            // backup
            hostModel.SetBackupCardInfoIdList(hostInfoIdList);
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_INIT_HAND_CARD);
            // draw card
            hostModel.DrawInitHandCard();
            CardModel normalCard;
            CardModel medicCard;
            if (selfHandRowAreaModel.cardList[0].cardInfo.ability == CardAbility.Medic) {
                normalCard = selfHandRowAreaModel.cardList[1];
                medicCard = selfHandRowAreaModel.cardList[0];
            } else {
                normalCard = selfHandRowAreaModel.cardList[0];
                medicCard = selfHandRowAreaModel.cardList[1];
            }
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_START);
            // 开始对局
            hostModel.StartSet(true);
            // 测试用，给弃牌区加一张普通牌，能力5
            hostModel.selfSinglePlayerAreaModel.discardAreaModel.AddCard(TestGenCards.GetCard(2043, 31));
            // host出牌，打出复活牌
            hostModel.ChooseCard(medicCard);
            hostModel.ChooseCard(normalCard); // 此时点普通牌，无事发生
            hostModel.ChooseCard(hostModel.selfSinglePlayerAreaModel.discardAreaModel.cardList[0]); // 复活这张牌
            Assert.AreEqual(10, hostModel.selfSinglePlayerAreaModel.GetCurrentPower());
            Assert.AreEqual(0, hostModel.enemySinglePlayerAreaModel.GetCurrentPower());
            Assert.AreEqual(false, hostModel.IsTurn(true));
            Assert.AreEqual(true, hostModel.IsTurn(false));
        });
        Thread playerThread = new Thread(() => {
            HandRowAreaModel selfHandRowAreaModel = playerModel.selfSinglePlayerAreaModel.handRowAreaModel;
            HandRowAreaModel enemyHandRowAreaModel = playerModel.enemySinglePlayerAreaModel.handRowAreaModel;
            // 能力5，非英雄牌
            List<int> playerInfoIdList = new List<int> { 2027 };
            playerModel.SetBackupCardInfoIdList(playerInfoIdList);
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_INIT_HAND_CARD);
            playerModel.DrawInitHandCard();
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_START);
            // 开始对局
            playerModel.StartSet(false);
            // 测试用，给弃牌区加一张普通牌，能力5
            playerModel.enemySinglePlayerAreaModel.discardAreaModel.AddCard(TestGenCards.GetCard(2043, 31));
            // 等待host出牌
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            Assert.AreEqual(0, playerModel.selfSinglePlayerAreaModel.GetCurrentPower());
            Assert.AreEqual(10, playerModel.enemySinglePlayerAreaModel.GetCurrentPower());
            Assert.AreEqual(true, playerModel.IsTurn(true));
            Assert.AreEqual(false, playerModel.IsTurn(false));
        });
        hostThread.Start();
        playerThread.Start();
        hostThread.Join();
        playerThread.Join();
    }

    // 测试复活牌技能：复活一张复活牌
    [Test]
    public void MedicMedic()
    {
        Thread hostThread = new Thread(() => {
            HandRowAreaModel selfHandRowAreaModel = hostModel.selfSinglePlayerAreaModel.handRowAreaModel;
            HandRowAreaModel enemyHandRowAreaModel = hostModel.enemySinglePlayerAreaModel.handRowAreaModel;
            // 能力5，非英雄牌
            List<int> hostInfoIdList = new List<int> { 2027 };
            // backup
            hostModel.SetBackupCardInfoIdList(hostInfoIdList);
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_INIT_HAND_CARD);
            // draw card
            hostModel.DrawInitHandCard();
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_START);
            // 开始对局
            hostModel.StartSet(true);
            // 测试用，给弃牌区加一张普通牌和复活牌，能力都是5
            CardModel medicCard = TestGenCards.GetCard(2027, 21);
            CardModel normalCard = TestGenCards.GetCard(2043, 31);
            hostModel.selfSinglePlayerAreaModel.discardAreaModel.AddCard(medicCard);
            hostModel.selfSinglePlayerAreaModel.discardAreaModel.AddCard(normalCard);
            // host出牌，打出复活牌
            hostModel.ChooseCard(selfHandRowAreaModel.cardList[0]);
            hostModel.ChooseCard(medicCard); // 复活一张复活牌
            hostModel.ChooseCard(normalCard); // 复活普通牌
            Assert.AreEqual(15, hostModel.selfSinglePlayerAreaModel.GetCurrentPower());
            Assert.AreEqual(0, hostModel.enemySinglePlayerAreaModel.GetCurrentPower());
            Assert.AreEqual(false, hostModel.IsTurn(true));
            Assert.AreEqual(true, hostModel.IsTurn(false));
        });
        Thread playerThread = new Thread(() => {
            HandRowAreaModel selfHandRowAreaModel = playerModel.selfSinglePlayerAreaModel.handRowAreaModel;
            HandRowAreaModel enemyHandRowAreaModel = playerModel.enemySinglePlayerAreaModel.handRowAreaModel;
            // 能力5，非英雄牌
            List<int> playerInfoIdList = new List<int> { 2027 };
            playerModel.SetBackupCardInfoIdList(playerInfoIdList);
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_INIT_HAND_CARD);
            playerModel.DrawInitHandCard();
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_START);
            // 开始对局
            playerModel.StartSet(false);
            // 测试用，给弃牌区加一张普通牌和复活牌，能力都是5
            CardModel medicCard = TestGenCards.GetCard(2027, 21);
            CardModel normalCard = TestGenCards.GetCard(2043, 31);
            playerModel.enemySinglePlayerAreaModel.discardAreaModel.AddCard(medicCard);
            playerModel.enemySinglePlayerAreaModel.discardAreaModel.AddCard(normalCard);
            // 等待host出牌
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            Assert.AreEqual(0, playerModel.selfSinglePlayerAreaModel.GetCurrentPower());
            Assert.AreEqual(15, playerModel.enemySinglePlayerAreaModel.GetCurrentPower());
            Assert.AreEqual(true, playerModel.IsTurn(true));
            Assert.AreEqual(false, playerModel.IsTurn(false));
        });
        hostThread.Start();
        playerThread.Start();
        hostThread.Join();
        playerThread.Join();
    }
    
    // 测试复活牌技能：中止复活技能
    [Test]
    public void MedicInterrupt()
    {
        Thread hostThread = new Thread(() => {
            HandRowAreaModel selfHandRowAreaModel = hostModel.selfSinglePlayerAreaModel.handRowAreaModel;
            HandRowAreaModel enemyHandRowAreaModel = hostModel.enemySinglePlayerAreaModel.handRowAreaModel;
            // 能力5，非英雄复活牌
            List<int> hostInfoIdList = new List<int> { 2027 };
            // backup
            hostModel.SetBackupCardInfoIdList(hostInfoIdList);
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_INIT_HAND_CARD);
            // draw card
            hostModel.DrawInitHandCard();
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_START);
            // 开始对局
            hostModel.StartSet(true);
            // 测试用，给弃牌区加一张普通牌，能力5
            hostModel.selfSinglePlayerAreaModel.discardAreaModel.AddCard(TestGenCards.GetCard(2043, 31));
            // host出牌，打出复活牌
            hostModel.ChooseCard(selfHandRowAreaModel.cardList[0]);
            hostModel.InterruptAction();
            Assert.AreEqual(5, hostModel.selfSinglePlayerAreaModel.GetCurrentPower());
            Assert.AreEqual(0, hostModel.enemySinglePlayerAreaModel.GetCurrentPower());
            Assert.AreEqual(false, hostModel.IsTurn(true));
            Assert.AreEqual(true, hostModel.IsTurn(false));
        });
        Thread playerThread = new Thread(() => {
            HandRowAreaModel selfHandRowAreaModel = playerModel.selfSinglePlayerAreaModel.handRowAreaModel;
            HandRowAreaModel enemyHandRowAreaModel = playerModel.enemySinglePlayerAreaModel.handRowAreaModel;
            // 能力5，非英雄牌
            List<int> playerInfoIdList = new List<int> { 2027 };
            playerModel.SetBackupCardInfoIdList(playerInfoIdList);
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_INIT_HAND_CARD);
            playerModel.DrawInitHandCard();
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_START);
            // 开始对局
            playerModel.StartSet(false);
            // 测试用，给弃牌区加一张普通牌，能力5
            playerModel.enemySinglePlayerAreaModel.discardAreaModel.AddCard(TestGenCards.GetCard(2043, 31));
            // 等待host出牌
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            Assert.AreEqual(0, playerModel.selfSinglePlayerAreaModel.GetCurrentPower());
            Assert.AreEqual(5, playerModel.enemySinglePlayerAreaModel.GetCurrentPower());
            Assert.AreEqual(true, playerModel.IsTurn(true));
            Assert.AreEqual(false, playerModel.IsTurn(false));
        });
        hostThread.Start();
        playerThread.Start();
        hostThread.Join();
        playerThread.Join();
    }

    // 测试攻击牌技能：无可攻击目标
    [Test]
    public void AttckNoTarget()
    {
        Thread hostThread = new Thread(() => {
            HandRowAreaModel selfHandRowAreaModel = hostModel.selfSinglePlayerAreaModel.handRowAreaModel;
            HandRowAreaModel enemyHandRowAreaModel = hostModel.enemySinglePlayerAreaModel.handRowAreaModel;
            // 攻击牌，攻击力4，能力10
            List<int> hostInfoIdList = new List<int> { 2023 };
            // backup
            hostModel.SetBackupCardInfoIdList(hostInfoIdList);
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_INIT_HAND_CARD);
            // draw card
            hostModel.DrawInitHandCard();
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_START);
            // 开始对局，对方先行
            hostModel.StartSet(false);
            // 等待player出牌
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            // host出牌，打出攻击牌，但没有可攻击对象
            hostModel.ChooseCard(selfHandRowAreaModel.cardList[0]);
            Assert.AreEqual(10, hostModel.selfSinglePlayerAreaModel.GetCurrentPower());
            Assert.AreEqual(10, hostModel.enemySinglePlayerAreaModel.GetCurrentPower());
            Assert.AreEqual(false, hostModel.IsTurn(true));
            Assert.AreEqual(true, hostModel.IsTurn(false));
        });
        Thread playerThread = new Thread(() => {
            HandRowAreaModel selfHandRowAreaModel = playerModel.selfSinglePlayerAreaModel.handRowAreaModel;
            HandRowAreaModel enemyHandRowAreaModel = playerModel.enemySinglePlayerAreaModel.handRowAreaModel;
            // 英雄牌，能力10
            List<int> playerInfoIdList = new List<int> { 2005 };
            playerModel.SetBackupCardInfoIdList(playerInfoIdList);
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_INIT_HAND_CARD);
            playerModel.DrawInitHandCard();
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_START);
            // 开始对局
            playerModel.StartSet(true);
            // player出牌
            playerModel.ChooseCard(selfHandRowAreaModel.cardList[0]);
            // 等待host出牌
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            Assert.AreEqual(10, playerModel.selfSinglePlayerAreaModel.GetCurrentPower());
            Assert.AreEqual(10, playerModel.enemySinglePlayerAreaModel.GetCurrentPower());
        });
        hostThread.Start();
        playerThread.Start();
        hostThread.Join();
        playerThread.Join();
    }

    // 测试攻击牌技能：攻击普通牌
    [Test]
    public void AttackNormal()
    {
        Thread hostThread = new Thread(() => {
            HandRowAreaModel selfHandRowAreaModel = hostModel.selfSinglePlayerAreaModel.handRowAreaModel;
            HandRowAreaModel enemyHandRowAreaModel = hostModel.enemySinglePlayerAreaModel.handRowAreaModel;
            // 攻击牌，攻击力4，能力10。普通牌能力4
            List<int> hostInfoIdList = new List<int> { 2023, 2024 };
            // backup
            hostModel.SetBackupCardInfoIdList(hostInfoIdList);
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_INIT_HAND_CARD);
            // draw card
            hostModel.DrawInitHandCard();
            CardModel normalCard;
            CardModel attackCard;
            if (selfHandRowAreaModel.cardList[0].cardInfo.ability == CardAbility.Attack) {
                normalCard = selfHandRowAreaModel.cardList[1];
                attackCard = selfHandRowAreaModel.cardList[0];
            } else {
                normalCard = selfHandRowAreaModel.cardList[0];
                attackCard = selfHandRowAreaModel.cardList[1];
            }
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_START);
            // 开始对局，对方先行
            hostModel.StartSet(false);
            // 等待player出牌
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            // host出牌，打出攻击牌
            hostModel.ChooseCard(attackCard);
            hostModel.ChooseCard(normalCard); // 此时点击普通牌，无事发生
            hostModel.ChooseCard(hostModel.enemySinglePlayerAreaModel.woodRowAreaModel.cardList[0]); // 选择攻击目标
            Assert.AreEqual(10, hostModel.selfSinglePlayerAreaModel.GetCurrentPower());
            Assert.AreEqual(1, hostModel.enemySinglePlayerAreaModel.GetCurrentPower());
            Assert.AreEqual(false, hostModel.IsTurn(true));
            Assert.AreEqual(true, hostModel.IsTurn(false));
        });
        Thread playerThread = new Thread(() => {
            HandRowAreaModel selfHandRowAreaModel = playerModel.selfSinglePlayerAreaModel.handRowAreaModel;
            HandRowAreaModel enemyHandRowAreaModel = playerModel.enemySinglePlayerAreaModel.handRowAreaModel;
            // 普通牌，能力5
            List<int> playerInfoIdList = new List<int> { 2004 };
            playerModel.SetBackupCardInfoIdList(playerInfoIdList);
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_INIT_HAND_CARD);
            playerModel.DrawInitHandCard();
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_START);
            // 开始对局
            playerModel.StartSet(true);
            // player出牌
            playerModel.ChooseCard(selfHandRowAreaModel.cardList[0]);
            // 等待host出牌
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            Assert.AreEqual(1, playerModel.selfSinglePlayerAreaModel.GetCurrentPower());
            Assert.AreEqual(10, playerModel.enemySinglePlayerAreaModel.GetCurrentPower());
        });
        hostThread.Start();
        playerThread.Start();
        hostThread.Join();
        playerThread.Join();
    }

    // 测试攻击牌技能：攻击导致卡牌被移除
    [Test]
    public void AttackDead()
    {
        Thread hostThread = new Thread(() => {
            HandRowAreaModel selfHandRowAreaModel = hostModel.selfSinglePlayerAreaModel.handRowAreaModel;
            HandRowAreaModel enemyHandRowAreaModel = hostModel.enemySinglePlayerAreaModel.handRowAreaModel;
            // 攻击牌，攻击力4，能力10
            List<int> hostInfoIdList = new List<int> { 2023 };
            // backup
            hostModel.SetBackupCardInfoIdList(hostInfoIdList);
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_INIT_HAND_CARD);
            // draw card
            hostModel.DrawInitHandCard();
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_START);
            // 开始对局，对方先行
            hostModel.StartSet(false);
            // 等待player出牌
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            // host出牌，打出攻击牌
            hostModel.ChooseCard(selfHandRowAreaModel.cardList[0]);
            hostModel.ChooseCard(hostModel.enemySinglePlayerAreaModel.woodRowAreaModel.cardList[0]);
            Assert.AreEqual(10, hostModel.selfSinglePlayerAreaModel.GetCurrentPower());
            Assert.AreEqual(0, hostModel.enemySinglePlayerAreaModel.GetCurrentPower());
            Assert.AreEqual(0, hostModel.enemySinglePlayerAreaModel.woodRowAreaModel.cardList.Count);
            Assert.AreEqual(1, hostModel.enemySinglePlayerAreaModel.discardAreaModel.cardList.Count);
            Assert.AreEqual(false, hostModel.IsTurn(true));
            Assert.AreEqual(true, hostModel.IsTurn(false));
        });
        Thread playerThread = new Thread(() => {
            HandRowAreaModel selfHandRowAreaModel = playerModel.selfSinglePlayerAreaModel.handRowAreaModel;
            HandRowAreaModel enemyHandRowAreaModel = playerModel.enemySinglePlayerAreaModel.handRowAreaModel;
            // 普通牌，能力4
            List<int> playerInfoIdList = new List<int> { 2007 };
            playerModel.SetBackupCardInfoIdList(playerInfoIdList);
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_INIT_HAND_CARD);
            playerModel.DrawInitHandCard();
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_START);
            // 开始对局
            playerModel.StartSet(true);
            // player出牌
            playerModel.ChooseCard(selfHandRowAreaModel.cardList[0]);
            // 等待host出牌
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            Assert.AreEqual(0, playerModel.selfSinglePlayerAreaModel.GetCurrentPower());
            Assert.AreEqual(10, playerModel.enemySinglePlayerAreaModel.GetCurrentPower());
            Assert.AreEqual(0, playerModel.selfSinglePlayerAreaModel.woodRowAreaModel.cardList.Count);
            Assert.AreEqual(1, playerModel.selfSinglePlayerAreaModel.discardAreaModel.cardList.Count);
        });
        hostThread.Start();
        playerThread.Start();
        hostThread.Join();
        playerThread.Join();
    }

    // 测试攻击牌技能：攻击技能中断
    [Test]
    public void AttackInterrupt()
    {
        Thread hostThread = new Thread(() => {
            HandRowAreaModel selfHandRowAreaModel = hostModel.selfSinglePlayerAreaModel.handRowAreaModel;
            HandRowAreaModel enemyHandRowAreaModel = hostModel.enemySinglePlayerAreaModel.handRowAreaModel;
            // 攻击牌，攻击力4，能力10。普通牌能力4
            List<int> hostInfoIdList = new List<int> { 2023 };
            // backup
            hostModel.SetBackupCardInfoIdList(hostInfoIdList);
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_INIT_HAND_CARD);
            // draw card
            hostModel.DrawInitHandCard();
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_START);
            // 开始对局，对方先行
            hostModel.StartSet(false);
            // 等待player出牌
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            // host出牌，打出攻击牌
            hostModel.ChooseCard(selfHandRowAreaModel.cardList[0]);
            hostModel.InterruptAction();
            Assert.AreEqual(10, hostModel.selfSinglePlayerAreaModel.GetCurrentPower());
            Assert.AreEqual(5, hostModel.enemySinglePlayerAreaModel.GetCurrentPower());
            Assert.AreEqual(false, hostModel.IsTurn(true));
            Assert.AreEqual(true, hostModel.IsTurn(false));
        });
        Thread playerThread = new Thread(() => {
            HandRowAreaModel selfHandRowAreaModel = playerModel.selfSinglePlayerAreaModel.handRowAreaModel;
            HandRowAreaModel enemyHandRowAreaModel = playerModel.enemySinglePlayerAreaModel.handRowAreaModel;
            // 普通牌，能力5
            List<int> playerInfoIdList = new List<int> { 2004 };
            playerModel.SetBackupCardInfoIdList(playerInfoIdList);
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_INIT_HAND_CARD);
            playerModel.DrawInitHandCard();
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_START);
            // 开始对局
            playerModel.StartSet(true);
            // player出牌
            playerModel.ChooseCard(selfHandRowAreaModel.cardList[0]);
            // 等待host出牌
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            Assert.AreEqual(5, playerModel.selfSinglePlayerAreaModel.GetCurrentPower());
            Assert.AreEqual(10, playerModel.enemySinglePlayerAreaModel.GetCurrentPower());
        });
        hostThread.Start();
        playerThread.Start();
        hostThread.Join();
        playerThread.Join();
    }

    // 测试Tunning技能
    [Test]
    public void Tunning()
    {
        Thread hostThread = new Thread(() => {
            HandRowAreaModel selfHandRowAreaModel = hostModel.selfSinglePlayerAreaModel.handRowAreaModel;
            HandRowAreaModel enemyHandRowAreaModel = hostModel.enemySinglePlayerAreaModel.handRowAreaModel;
            // 攻击牌，攻击力4，能力10
            List<int> hostInfoIdList = new List<int> { 2023 };
            // backup
            hostModel.SetBackupCardInfoIdList(hostInfoIdList);
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_INIT_HAND_CARD);
            // draw card
            hostModel.DrawInitHandCard();
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_START);
            // 开始对局，对方先行
            hostModel.StartSet(false);
            // 等待player出牌
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            // host出牌，打出攻击牌
            hostModel.ChooseCard(selfHandRowAreaModel.cardList[0]);
            hostModel.ChooseCard(hostModel.enemySinglePlayerAreaModel.woodRowAreaModel.cardList[0]);
            // 等待player出牌
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            Assert.AreEqual(10, hostModel.selfSinglePlayerAreaModel.GetCurrentPower());
            Assert.AreEqual(12, hostModel.enemySinglePlayerAreaModel.GetCurrentPower());
        });
        Thread playerThread = new Thread(() => {
            HandRowAreaModel selfHandRowAreaModel = playerModel.selfSinglePlayerAreaModel.handRowAreaModel;
            HandRowAreaModel enemyHandRowAreaModel = playerModel.enemySinglePlayerAreaModel.handRowAreaModel;
            // Tunning牌（7）、普通牌（5）
            List<int> playerInfoIdList = new List<int> { 2001, 2002 };
            playerModel.SetBackupCardInfoIdList(playerInfoIdList);
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_INIT_HAND_CARD);
            playerModel.DrawInitHandCard();
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_START);
            // 开始对局
            playerModel.StartSet(true);
            CardModel normalCard;
            CardModel tunningCard;
            if (selfHandRowAreaModel.cardList[0].cardInfo.ability == CardAbility.Tunning) {
                normalCard = selfHandRowAreaModel.cardList[1];
                tunningCard = selfHandRowAreaModel.cardList[0];
            } else {
                normalCard = selfHandRowAreaModel.cardList[0];
                tunningCard = selfHandRowAreaModel.cardList[1];
            }
            // player出牌
            playerModel.ChooseCard(normalCard);
            // 等待host出牌
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            // player出牌，Tunning
            playerModel.ChooseCard(tunningCard);
            Assert.AreEqual(12, playerModel.selfSinglePlayerAreaModel.GetCurrentPower());
            Assert.AreEqual(10, playerModel.enemySinglePlayerAreaModel.GetCurrentPower());
        });
        hostThread.Start();
        playerThread.Start();
        hostThread.Join();
        playerThread.Join();
    }
}
