using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.TestTools;

public class PlaySceneModelTest
{
    private static string TAG = "PlaySceneModelTest";

    private PlaySceneModel hostModel; // 以下测试都以host为self的视角

    private PlaySceneModel playerModel;

    private Dictionary<AudioManager.SFXType, int> hostSfxPlayCount;

    private Dictionary<AudioManager.SFXType, int> playerSfxPlayCount;

    [SetUp]
    public void SetUp()
    {
        KLog.I(TAG, "SetUp: " + TestContext.CurrentContext.Test.Name);
        // 测试用，先把CardGenerator的资源加载了
        CardGenerator cardGenerator = new CardGenerator();
        cardGenerator.GetCard(2001);
        // 初始化
        hostModel = new PlaySceneModel();
        playerModel = new PlaySceneModel(false);
        hostModel.battleModel.SendToEnemyFunc += playerModel.battleModel.AddEnemyActionMsg;
        playerModel.battleModel.SendToEnemyFunc += hostModel.battleModel.AddEnemyActionMsg;

        hostSfxPlayCount = new Dictionary<AudioManager.SFXType, int>();
        playerSfxPlayCount = new Dictionary<AudioManager.SFXType, int>();
        foreach (AudioManager.SFXType type in Enum.GetValues(typeof(AudioManager.SFXType))) {
            hostSfxPlayCount[type] = 0;
            playerSfxPlayCount[type] = 0;
        }
        hostModel.SfxCallback += (AudioManager.SFXType type) => {
            hostSfxPlayCount[type] += 1;
        };
        playerModel.SfxCallback += (AudioManager.SFXType type) => {
            playerSfxPlayCount[type] += 1;
        };
    }

    [TearDown]
    public void Teardown()
    {
        KLog.I(TAG, "Teardown: " + TestContext.CurrentContext.Test.Name);
        ResetHostFirstRandom();
        hostModel.Release();
        playerModel.Release();
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

    private void ResetHostFirstRandom()
    {
        PlayStateTracker.HOST_FIRST_RANDOM_MAX = 100;
        PlayStateTracker.HOST_FIRST_RANDOM_MIN = 0;
    }

    // 设置是否host先手
    private void SetIsHostFirst(bool isHostFirst)
    {
        ResetHostFirstRandom();
        if (isHostFirst) {
            PlayStateTracker.HOST_FIRST_RANDOM_MIN = PlayStateTracker.HOST_FIRST_RANDOM_THRESHOLD;
        } else {
            PlayStateTracker.HOST_FIRST_RANDOM_MAX = PlayStateTracker.HOST_FIRST_RANDOM_THRESHOLD;
        }
    }

    // 测试双方打出普通牌的一局流程
    [UnityTest]
    public IEnumerator Normal()
    {
        Assert.AreEqual(false, hostModel.hasEnemyUpdate);
        Assert.AreEqual(false, playerModel.hasEnemyUpdate);
        // host先手
        SetIsHostFirst(true);
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
            hostModel.ChooseCard(hostModel.selfSinglePlayerAreaModel.initHandRowAreaModel.cardList[0]);
            hostModel.ReDrawInitHandCard();
            Assert.AreEqual(false, hostModel.EnableChooseCard(true));
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            // 开始对局
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
            playerModel.ReDrawInitHandCard();
            // 开始对局
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
        });
        hostThread.Start();
        playerThread.Start();
        while (hostThread.IsAlive) {
            yield return null;
        }
        while (playerThread.IsAlive) {
            yield return null;
        }
    }

    // 测试双方打出间谍牌
    [UnityTest]
    public IEnumerator Spy()
    {
        // host先手
        SetIsHostFirst(true);
        Thread hostThread = new Thread(() => {
            HandRowAreaModel selfHandRowAreaModel = hostModel.selfSinglePlayerAreaModel.handRowAreaModel;
            HandRowAreaModel enemyHandRowAreaModel = hostModel.enemySinglePlayerAreaModel.handRowAreaModel;
            // 铜管间谍牌，能力4
            List<int> hostInfoIdList = Enumerable.Repeat(2030, 13).ToList();
            // backup
            hostModel.SetBackupCardInfoIdList(hostInfoIdList);
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_INIT_HAND_CARD);
            // draw card
            hostModel.DrawInitHandCard();
            hostModel.ReDrawInitHandCard();
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            // 开始对局
            // host出牌
            Assert.AreEqual(true, hostModel.IsTurn(true));
            Assert.AreEqual(false, hostModel.IsTurn(false));
            List<CardModel> tempList = new List<CardModel>(hostModel.selfSinglePlayerAreaModel.backupCardList);
            CardModel selectedCard = selfHandRowAreaModel.cardList[0];
            hostModel.ChooseCard(selectedCard);
            CardModel newCard = tempList.Find(card => !hostModel.selfSinglePlayerAreaModel.backupCardList.Contains(card));
            Assert.AreEqual(9 + 2, selfHandRowAreaModel.cardList.Count);
            Assert.AreEqual(10, enemyHandRowAreaModel.cardList.Count);
            Assert.AreEqual(0, hostModel.selfSinglePlayerAreaModel.GetCurrentPower());
            Assert.AreEqual(4, hostModel.enemySinglePlayerAreaModel.GetCurrentPower());
            // 等待player出牌
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            // host打出新抽的牌
            hostModel.ChooseCard(newCard);
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            Assert.AreEqual(8, playerModel.selfSinglePlayerAreaModel.GetCurrentPower());
            Assert.AreEqual(8, playerModel.enemySinglePlayerAreaModel.GetCurrentPower());
        });
        Thread playerThread = new Thread(() => {
            HandRowAreaModel selfHandRowAreaModel = playerModel.selfSinglePlayerAreaModel.handRowAreaModel;
            HandRowAreaModel enemyHandRowAreaModel = playerModel.enemySinglePlayerAreaModel.handRowAreaModel;
            // 铜管间谍牌，能力4
            List<int> playerInfoIdList = Enumerable.Repeat(2030, 13).ToList();
            playerModel.SetBackupCardInfoIdList(playerInfoIdList);
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_INIT_HAND_CARD);
            playerModel.DrawInitHandCard();
            playerModel.ReDrawInitHandCard();
            // 开始对局
            // 等待host出牌
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            // player出牌
            Assert.AreEqual(true, playerModel.IsTurn(true)); // playerModel视角
            Assert.AreEqual(false, playerModel.IsTurn(false));
            List<CardModel> tempList = new List<CardModel>(playerModel.selfSinglePlayerAreaModel.backupCardList);
            CardModel selectedCard = selfHandRowAreaModel.cardList[0];
            playerModel.ChooseCard(selectedCard);
            CardModel newCard = tempList.Find(card => !playerModel.selfSinglePlayerAreaModel.backupCardList.Contains(card));
            Assert.AreEqual(9 + 2, selfHandRowAreaModel.cardList.Count);
            Assert.AreEqual(9 + 2, enemyHandRowAreaModel.cardList.Count);
            Assert.AreEqual(4, playerModel.selfSinglePlayerAreaModel.GetCurrentPower());
            Assert.AreEqual(4, playerModel.enemySinglePlayerAreaModel.GetCurrentPower());
            // 等待host
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            // player打出新抽的牌
            playerModel.ChooseCard(newCard);
            Assert.AreEqual(8, playerModel.selfSinglePlayerAreaModel.GetCurrentPower());
            Assert.AreEqual(8, playerModel.enemySinglePlayerAreaModel.GetCurrentPower());
        });
        hostThread.Start();
        playerThread.Start();
        while (hostThread.IsAlive) {
            yield return null;
        }
        while (playerThread.IsAlive) {
            yield return null;
        }
    }

    // 测试伞击技能。主要测试已在BattleRowAreaModelTest中完成，此处简单测试整体流程
    [UnityTest]
    public IEnumerator ScorchWood()
    {
        // player先手
        SetIsHostFirst(false);
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
            hostModel.ReDrawInitHandCard();
            CardModel normalCard;
            CardModel scorchCard;
            if (selfHandRowAreaModel.cardList[0].cardInfo.ability == CardAbility.ScorchWood) {
                normalCard = selfHandRowAreaModel.cardList[1];
                scorchCard = selfHandRowAreaModel.cardList[0];
            } else {
                normalCard = selfHandRowAreaModel.cardList[0];
                scorchCard = selfHandRowAreaModel.cardList[1];
            }
            // 开始对局，对方先出牌
            // 等待player出牌
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            // host出牌
            hostModel.ChooseCard(normalCard);
            // 等待player出牌
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            // host出牌
            hostModel.ChooseCard(scorchCard); // 打出伞击牌，移除对面能力为7的牌
            Assert.AreEqual(1, hostSfxPlayCount[AudioManager.SFXType.Scorch]);
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
            playerModel.ReDrawInitHandCard();
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            // 开始对局
            // player出牌
            playerModel.ChooseCard(selfHandRowAreaModel.cardList[0]);
            // 等待host出牌
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            // player出牌
            playerModel.ChooseCard(selfHandRowAreaModel.cardList[0]);
            // 等待host出牌
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            Assert.AreEqual(1, playerSfxPlayCount[AudioManager.SFXType.Scorch]);
            Assert.AreEqual(5, playerModel.selfSinglePlayerAreaModel.GetCurrentPower());
            Assert.AreEqual(12, playerModel.enemySinglePlayerAreaModel.GetCurrentPower());
            Assert.AreEqual(1, playerModel.selfSinglePlayerAreaModel.discardAreaModel.cardList.Count);
        });
        hostThread.Start();
        playerThread.Start();
        while (hostThread.IsAlive) {
            yield return null;
        }
        while (playerThread.IsAlive) {
            yield return null;
        }
    }

    // 测试复活牌技能：无复活目标
    [UnityTest]
    public IEnumerator MedicNoTarget()
    {
        // host先手
        SetIsHostFirst(true);
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
            hostModel.ReDrawInitHandCard();
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            // 开始对局
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
            playerModel.ReDrawInitHandCard();
            // 开始对局
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
        while (hostThread.IsAlive) {
            yield return null;
        }
        while (playerThread.IsAlive) {
            yield return null;
        }
    }

    // 测试复活牌技能：复活一张普通牌
    [UnityTest]
    public IEnumerator MedicNormal()
    {
        // host先手
        SetIsHostFirst(true);
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
            hostModel.ReDrawInitHandCard();
            CardModel normalCard;
            CardModel medicCard;
            if (selfHandRowAreaModel.cardList[0].cardInfo.ability == CardAbility.Medic) {
                normalCard = selfHandRowAreaModel.cardList[1];
                medicCard = selfHandRowAreaModel.cardList[0];
            } else {
                normalCard = selfHandRowAreaModel.cardList[0];
                medicCard = selfHandRowAreaModel.cardList[1];
            }
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            // 开始对局
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
            playerModel.ReDrawInitHandCard();
            // 开始对局
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
        while (hostThread.IsAlive) {
            yield return null;
        }
        while (playerThread.IsAlive) {
            yield return null;
        }
    }

    // 测试复活牌技能：复活一张复活牌
    [UnityTest]
    public IEnumerator MedicMedic()
    {
        // host先手
        SetIsHostFirst(true);
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
            hostModel.ReDrawInitHandCard();
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            // 开始对局
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
            playerModel.ReDrawInitHandCard();
            // 开始对局
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
        while (hostThread.IsAlive) {
            yield return null;
        }
        while (playerThread.IsAlive) {
            yield return null;
        }
    }
    
    // 测试复活牌技能：中止复活技能
    [UnityTest]
    public IEnumerator MedicInterrupt()
    {
        // host先手
        SetIsHostFirst(true);
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
            hostModel.ReDrawInitHandCard();
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            // 开始对局
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
            playerModel.ReDrawInitHandCard();
            // 开始对局
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
        while (hostThread.IsAlive) {
            yield return null;
        }
        while (playerThread.IsAlive) {
            yield return null;
        }
    }

    // 测试复活牌技能：指挥牌带复活技能
    [UnityTest]
    public IEnumerator MedicLeader()
    {
        // host先手
        SetIsHostFirst(true);
        Thread hostThread = new Thread(() => {
            HandRowAreaModel selfHandRowAreaModel = hostModel.selfSinglePlayerAreaModel.handRowAreaModel;
            HandRowAreaModel enemyHandRowAreaModel = hostModel.enemySinglePlayerAreaModel.handRowAreaModel;
            // 指挥牌带复活，还有随便一张牌
            List<int> hostInfoIdList = new List<int> { 1080, 1030 };
            // backup
            hostModel.SetBackupCardInfoIdList(hostInfoIdList);
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_INIT_HAND_CARD);
            // draw card
            hostModel.DrawInitHandCard();
            hostModel.ReDrawInitHandCard();
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            // 开始对局
            // 测试用，给弃牌区加一张普通牌，能力5
            hostModel.selfSinglePlayerAreaModel.discardAreaModel.AddCard(TestGenCards.GetCard(2043, 31));
            // host出牌，打出复活牌
            CardModel leaderCard = hostModel.selfSinglePlayerAreaModel.leaderCardAreaModel.cardList[0];
            hostModel.ChooseCard(leaderCard);
            hostModel.ChooseCard(hostModel.selfSinglePlayerAreaModel.discardAreaModel.cardList[0]); // 复活这张牌
            Assert.AreEqual(CardLocation.None, leaderCard.cardLocation);
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
            playerModel.ReDrawInitHandCard();
            // 开始对局
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
        while (hostThread.IsAlive) {
            yield return null;
        }
        while (playerThread.IsAlive) {
            yield return null;
        }
    }

    // 测试攻击牌技能：无可攻击目标
    [UnityTest]
    public IEnumerator AttckNoTarget()
    {
        // player先手
        SetIsHostFirst(false);
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
            hostModel.ReDrawInitHandCard();
            // 开始对局，对方先行
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
            playerModel.ReDrawInitHandCard();
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            // 开始对局
            // player出牌
            playerModel.ChooseCard(selfHandRowAreaModel.cardList[0]);
            // 等待host出牌
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            Assert.AreEqual(10, playerModel.selfSinglePlayerAreaModel.GetCurrentPower());
            Assert.AreEqual(10, playerModel.enemySinglePlayerAreaModel.GetCurrentPower());
        });
        hostThread.Start();
        playerThread.Start();
        while (hostThread.IsAlive) {
            yield return null;
        }
        while (playerThread.IsAlive) {
            yield return null;
        }
    }

    // 测试攻击牌技能：攻击普通牌
    [UnityTest]
    public IEnumerator AttackNormal()
    {
        // player先手
        SetIsHostFirst(false);
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
            hostModel.ReDrawInitHandCard();
            CardModel normalCard;
            CardModel attackCard;
            if (selfHandRowAreaModel.cardList[0].cardInfo.ability == CardAbility.Attack) {
                normalCard = selfHandRowAreaModel.cardList[1];
                attackCard = selfHandRowAreaModel.cardList[0];
            } else {
                normalCard = selfHandRowAreaModel.cardList[0];
                attackCard = selfHandRowAreaModel.cardList[1];
            }
            // 开始对局，对方先行
            // 等待player出牌
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            // host出牌，打出攻击牌
            hostModel.ChooseCard(attackCard);
            hostModel.ChooseCard(normalCard); // 此时点击普通牌，无事发生
            hostModel.ChooseCard(hostModel.enemySinglePlayerAreaModel.woodRowAreaModel.cardList[0]); // 选择攻击目标
            Assert.AreEqual(0, hostSfxPlayCount[AudioManager.SFXType.Scorch]);
            Assert.AreEqual(1, hostSfxPlayCount[AudioManager.SFXType.Attack]);
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
            playerModel.ReDrawInitHandCard();
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            // 开始对局
            // player出牌
            playerModel.ChooseCard(selfHandRowAreaModel.cardList[0]);
            // 等待host出牌
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            Assert.AreEqual(0, playerSfxPlayCount[AudioManager.SFXType.Scorch]);
            Assert.AreEqual(1, playerSfxPlayCount[AudioManager.SFXType.Attack]);
            Assert.AreEqual(1, playerModel.selfSinglePlayerAreaModel.GetCurrentPower());
            Assert.AreEqual(10, playerModel.enemySinglePlayerAreaModel.GetCurrentPower());
        });
        hostThread.Start();
        playerThread.Start();
        while (hostThread.IsAlive) {
            yield return null;
        }
        while (playerThread.IsAlive) {
            yield return null;
        }
    }

    // 测试攻击牌技能：攻击导致卡牌被移除
    [UnityTest]
    public IEnumerator AttackDead()
    {
        // player先手
        SetIsHostFirst(false);
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
            hostModel.ReDrawInitHandCard();
            // 开始对局，对方先行
            // 等待player出牌
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            // host出牌，打出攻击牌
            hostModel.ChooseCard(selfHandRowAreaModel.cardList[0]);
            hostModel.ChooseCard(hostModel.enemySinglePlayerAreaModel.woodRowAreaModel.cardList[0]);
            Assert.AreEqual(1, hostSfxPlayCount[AudioManager.SFXType.Scorch]);
            Assert.AreEqual(0, hostSfxPlayCount[AudioManager.SFXType.Attack]);
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
            playerModel.ReDrawInitHandCard();
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            // 开始对局
            // player出牌
            playerModel.ChooseCard(selfHandRowAreaModel.cardList[0]);
            // 等待host出牌
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            Assert.AreEqual(1, playerSfxPlayCount[AudioManager.SFXType.Scorch]);
            Assert.AreEqual(0, playerSfxPlayCount[AudioManager.SFXType.Attack]);
            Assert.AreEqual(0, playerModel.selfSinglePlayerAreaModel.GetCurrentPower());
            Assert.AreEqual(10, playerModel.enemySinglePlayerAreaModel.GetCurrentPower());
            Assert.AreEqual(0, playerModel.selfSinglePlayerAreaModel.woodRowAreaModel.cardList.Count);
            Assert.AreEqual(1, playerModel.selfSinglePlayerAreaModel.discardAreaModel.cardList.Count);
        });
        hostThread.Start();
        playerThread.Start();
        while (hostThread.IsAlive) {
            yield return null;
        }
        while (playerThread.IsAlive) {
            yield return null;
        }
    }

    // 测试攻击牌技能：攻击技能中断
    [UnityTest]
    public IEnumerator AttackInterrupt()
    {
        // player先手
        SetIsHostFirst(false);
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
            hostModel.ReDrawInitHandCard();
            // 开始对局，对方先行
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
            playerModel.ReDrawInitHandCard();
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            // 开始对局
            // player出牌
            playerModel.ChooseCard(selfHandRowAreaModel.cardList[0]);
            // 等待host出牌
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            Assert.AreEqual(5, playerModel.selfSinglePlayerAreaModel.GetCurrentPower());
            Assert.AreEqual(10, playerModel.enemySinglePlayerAreaModel.GetCurrentPower());
        });
        hostThread.Start();
        playerThread.Start();
        while (hostThread.IsAlive) {
            yield return null;
        }
        while (playerThread.IsAlive) {
            yield return null;
        }
    }

    // 测试Tunning技能
    [UnityTest]
    public IEnumerator Tunning()
    {
        // player先手
        SetIsHostFirst(false);
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
            hostModel.ReDrawInitHandCard();
            // 开始对局，对方先行
            // 等待player出牌
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            // host出牌，打出攻击牌
            hostModel.ChooseCard(selfHandRowAreaModel.cardList[0]);
            hostModel.ChooseCard(hostModel.enemySinglePlayerAreaModel.woodRowAreaModel.cardList[0]);
            // 等待player出牌
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            Assert.AreEqual(1, hostSfxPlayCount[AudioManager.SFXType.Tunning]);
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
            playerModel.ReDrawInitHandCard();
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            // 开始对局
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
            Assert.AreEqual(1, playerSfxPlayCount[AudioManager.SFXType.Tunning]);
            Assert.AreEqual(12, playerModel.selfSinglePlayerAreaModel.GetCurrentPower());
            Assert.AreEqual(10, playerModel.enemySinglePlayerAreaModel.GetCurrentPower());
        });
        hostThread.Start();
        playerThread.Start();
        while (hostThread.IsAlive) {
            yield return null;
        }
        while (playerThread.IsAlive) {
            yield return null;
        }
    }

    // 测试单局结算
    [UnityTest]
    public IEnumerator SetFinish()
    {
        // host先手
        SetIsHostFirst(true);
        Thread hostThread = new Thread(() => {
            HandRowAreaModel selfHandRowAreaModel = hostModel.selfSinglePlayerAreaModel.handRowAreaModel;
            HandRowAreaModel enemyHandRowAreaModel = hostModel.enemySinglePlayerAreaModel.handRowAreaModel;
            // Tunning牌（7）
            List<int> hostInfoIdList = new List<int> { 2001 };
            // backup
            hostModel.SetBackupCardInfoIdList(hostInfoIdList);
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_INIT_HAND_CARD);
            // draw card
            hostModel.DrawInitHandCard();
            hostModel.ReDrawInitHandCard();
            // 开始对局
            // host出牌
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            hostModel.ChooseCard(selfHandRowAreaModel.cardList[0]);
            // 等待player出牌
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            // host pass
            hostModel.Pass();
            Thread.Sleep(100);
            // 等待player pass。host赢第一局，第二局host先手
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            Assert.AreEqual(1, hostSfxPlayCount[AudioManager.SFXType.SetFinish]);
            Assert.AreEqual(1, hostModel.tracker.setRecordList[0].result);
            Assert.AreEqual(7, hostModel.tracker.setRecordList[0].selfScore);
            Assert.AreEqual(5, hostModel.tracker.setRecordList[0].enemyScore);
            Assert.AreEqual(true, hostModel.tracker.setRecordList[1].selfFirst);
        });
        Thread playerThread = new Thread(() => {
            HandRowAreaModel selfHandRowAreaModel = playerModel.selfSinglePlayerAreaModel.handRowAreaModel;
            HandRowAreaModel enemyHandRowAreaModel = playerModel.enemySinglePlayerAreaModel.handRowAreaModel;
            // 普通牌（5）
            List<int> playerInfoIdList = new List<int> { 2002 };
            playerModel.SetBackupCardInfoIdList(playerInfoIdList);
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_INIT_HAND_CARD);
            playerModel.DrawInitHandCard();
            playerModel.ReDrawInitHandCard();
            // 开始对局
            // 等待host出牌
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            // player出牌
            playerModel.ChooseCard(selfHandRowAreaModel.cardList[0]);
            // 等待host pass
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            // player pass
            playerModel.Pass();
            Thread.Sleep(100);
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_ENEMY_ACTION);
            Assert.AreEqual(1, playerSfxPlayCount[AudioManager.SFXType.SetFinish]);
            Assert.AreEqual(-1, playerModel.tracker.setRecordList[0].result);
            Assert.AreEqual(5, playerModel.tracker.setRecordList[0].selfScore);
            Assert.AreEqual(7, playerModel.tracker.setRecordList[0].enemyScore);
            Assert.AreEqual(false, playerModel.tracker.setRecordList[1].selfFirst);
        });
        hostThread.Start();
        playerThread.Start();
        while (hostThread.IsAlive) {
            yield return null;
        }
        while (playerThread.IsAlive) {
            yield return null;
        }
    }

    // 测试decoy技能：无目标
    [UnityTest]
    public IEnumerator DecoyNoTarget()
    {
        // host先手
        SetIsHostFirst(true);
        Thread hostThread = new Thread(() => {
            HandRowAreaModel selfHandRowAreaModel = hostModel.selfSinglePlayerAreaModel.handRowAreaModel;
            HandRowAreaModel enemyHandRowAreaModel = hostModel.enemySinglePlayerAreaModel.handRowAreaModel;
            // 英雄牌（10），大号君
            List<int> hostInfoIdList = new List<int> { 2005, 5001 };
            // backup
            hostModel.SetBackupCardInfoIdList(hostInfoIdList);
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_INIT_HAND_CARD);
            // draw card
            hostModel.DrawInitHandCard();
            hostModel.ReDrawInitHandCard();
            CardModel roleCard;
            CardModel decoyCard;
            if (selfHandRowAreaModel.cardList[0].cardInfo.ability == CardAbility.Decoy) {
                roleCard = selfHandRowAreaModel.cardList[1];
                decoyCard = selfHandRowAreaModel.cardList[0];
            } else {
                roleCard = selfHandRowAreaModel.cardList[0];
                decoyCard = selfHandRowAreaModel.cardList[1];
            }
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            // 开始对局
            // host出牌，打出英雄牌
            hostModel.ChooseCard(roleCard);
            // 等待player出牌
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            // host出牌，打出decoy牌，但无目标，相当于打出一张虚空牌
            hostModel.ChooseCard(decoyCard);
            Assert.AreEqual(CardLocation.None, decoyCard.cardLocation);
            Assert.AreEqual(PlayStateTracker.ActionState.None, hostModel.tracker.actionState);
            Assert.AreEqual(PlayStateTracker.State.WAIT_ENEMY_ACTION, hostModel.tracker.curState);
            Assert.AreEqual(10, hostModel.selfSinglePlayerAreaModel.GetCurrentPower());
            Assert.AreEqual(5, hostModel.enemySinglePlayerAreaModel.GetCurrentPower());
        });
        Thread playerThread = new Thread(() => {
            HandRowAreaModel selfHandRowAreaModel = playerModel.selfSinglePlayerAreaModel.handRowAreaModel;
            HandRowAreaModel enemyHandRowAreaModel = playerModel.enemySinglePlayerAreaModel.handRowAreaModel;
            // 能力5，非英雄牌
            List<int> playerInfoIdList = new List<int> { 2002 };
            playerModel.SetBackupCardInfoIdList(playerInfoIdList);
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_INIT_HAND_CARD);
            playerModel.DrawInitHandCard();
            playerModel.ReDrawInitHandCard();
            // 开始对局
            // 等待host出牌
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            // player出牌，打出普通牌
            playerModel.ChooseCard(selfHandRowAreaModel.cardList[0]);
            // 等待host出牌
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            Assert.AreEqual(5, playerModel.selfSinglePlayerAreaModel.GetCurrentPower());
            Assert.AreEqual(10, playerModel.enemySinglePlayerAreaModel.GetCurrentPower());
        });
        hostThread.Start();
        playerThread.Start();
        while (hostThread.IsAlive) {
            yield return null;
        }
        while (playerThread.IsAlive) {
            yield return null;
        }
    }

    // 测试decoy技能：撤回普通牌
    [UnityTest]
    public IEnumerator DecoyNormal()
    {
        // host先手
        SetIsHostFirst(true);
        Thread hostThread = new Thread(() => {
            HandRowAreaModel selfHandRowAreaModel = hostModel.selfSinglePlayerAreaModel.handRowAreaModel;
            HandRowAreaModel enemyHandRowAreaModel = hostModel.enemySinglePlayerAreaModel.handRowAreaModel;
            // 普通牌（5），大号君
            List<int> hostInfoIdList = new List<int> { 2002, 5001 };
            // backup
            hostModel.SetBackupCardInfoIdList(hostInfoIdList);
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_INIT_HAND_CARD);
            // draw card
            hostModel.DrawInitHandCard();
            hostModel.ReDrawInitHandCard();
            CardModel roleCard;
            CardModel decoyCard;
            if (selfHandRowAreaModel.cardList[0].cardInfo.ability == CardAbility.Decoy) {
                roleCard = selfHandRowAreaModel.cardList[1];
                decoyCard = selfHandRowAreaModel.cardList[0];
            } else {
                roleCard = selfHandRowAreaModel.cardList[0];
                decoyCard = selfHandRowAreaModel.cardList[1];
            }
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            // 开始对局
            // host出牌，打出英雄牌
            hostModel.ChooseCard(roleCard);
            // 等待player出牌
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            // host出牌，打出decoy牌
            hostModel.ChooseCard(decoyCard);
            hostModel.ChooseCard(roleCard);
            Assert.AreEqual(1, selfHandRowAreaModel.cardList.Count);
            Assert.AreEqual(CardLocation.HandArea, roleCard.cardLocation);
            Assert.AreEqual(CardLocation.BattleArea, decoyCard.cardLocation);
            Assert.AreEqual(PlayStateTracker.ActionState.None, hostModel.tracker.actionState);
            Assert.AreEqual(PlayStateTracker.State.WAIT_ENEMY_ACTION, hostModel.tracker.curState);
            Assert.AreEqual(0, hostModel.selfSinglePlayerAreaModel.GetCurrentPower());
            Assert.AreEqual(5, hostModel.enemySinglePlayerAreaModel.GetCurrentPower());
            // 等待player pass
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            // host pass
            hostModel.Pass();
            Thread.Sleep(100);
            Assert.AreEqual(0, hostModel.selfSinglePlayerAreaModel.discardAreaModel.cardList.Count); // decoy不进入弃牌区
        });
        Thread playerThread = new Thread(() => {
            HandRowAreaModel selfHandRowAreaModel = playerModel.selfSinglePlayerAreaModel.handRowAreaModel;
            HandRowAreaModel enemyHandRowAreaModel = playerModel.enemySinglePlayerAreaModel.handRowAreaModel;
            // 能力5，非英雄牌
            List<int> playerInfoIdList = new List<int> { 2002 };
            playerModel.SetBackupCardInfoIdList(playerInfoIdList);
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_INIT_HAND_CARD);
            playerModel.DrawInitHandCard();
            playerModel.ReDrawInitHandCard();
            // 开始对局
            // 等待host出牌
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            // player出牌，打出普通牌
            playerModel.ChooseCard(selfHandRowAreaModel.cardList[0]);
            // 等待host出牌
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            Assert.AreEqual(1, enemyHandRowAreaModel.cardList.Count);
            Assert.AreEqual(5, playerModel.selfSinglePlayerAreaModel.GetCurrentPower());
            Assert.AreEqual(0, playerModel.enemySinglePlayerAreaModel.GetCurrentPower());
            // player pass
            playerModel.Pass();
            Thread.Sleep(100);
            Assert.AreEqual(0, playerModel.enemySinglePlayerAreaModel.discardAreaModel.cardList.Count); // decoy不进入弃牌区
        });
        hostThread.Start();
        playerThread.Start();
        while (hostThread.IsAlive) {
            yield return null;
        }
        while (playerThread.IsAlive) {
            yield return null;
        }
    }

    // 测试decoy技能中断
    [UnityTest]
    public IEnumerator DecoyInterrupt()
    {
        // host先手
        SetIsHostFirst(true);
        Thread hostThread = new Thread(() => {
            HandRowAreaModel selfHandRowAreaModel = hostModel.selfSinglePlayerAreaModel.handRowAreaModel;
            HandRowAreaModel enemyHandRowAreaModel = hostModel.enemySinglePlayerAreaModel.handRowAreaModel;
            // 普通牌（5），大号君
            List<int> hostInfoIdList = new List<int> { 2002, 5001 };
            // backup
            hostModel.SetBackupCardInfoIdList(hostInfoIdList);
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_INIT_HAND_CARD);
            // draw card
            hostModel.DrawInitHandCard();
            hostModel.ReDrawInitHandCard();
            CardModel roleCard;
            CardModel decoyCard;
            if (selfHandRowAreaModel.cardList[0].cardInfo.ability == CardAbility.Decoy) {
                roleCard = selfHandRowAreaModel.cardList[1];
                decoyCard = selfHandRowAreaModel.cardList[0];
            } else {
                roleCard = selfHandRowAreaModel.cardList[0];
                decoyCard = selfHandRowAreaModel.cardList[1];
            }
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            // 开始对局
            // host出牌，打出英雄牌
            hostModel.ChooseCard(roleCard);
            // 等待player出牌
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            // host出牌，打出decoy牌
            hostModel.ChooseCard(decoyCard);
            hostModel.InterruptAction();
            Assert.AreEqual(0, selfHandRowAreaModel.cardList.Count);
            Assert.AreEqual(CardLocation.BattleArea, roleCard.cardLocation);
            Assert.AreEqual(CardLocation.None, decoyCard.cardLocation);
            Assert.AreEqual(PlayStateTracker.ActionState.None, hostModel.tracker.actionState);
            Assert.AreEqual(PlayStateTracker.State.WAIT_ENEMY_ACTION, hostModel.tracker.curState);
            Assert.AreEqual(5, hostModel.selfSinglePlayerAreaModel.GetCurrentPower());
            Assert.AreEqual(5, hostModel.enemySinglePlayerAreaModel.GetCurrentPower());
        });
        Thread playerThread = new Thread(() => {
            HandRowAreaModel selfHandRowAreaModel = playerModel.selfSinglePlayerAreaModel.handRowAreaModel;
            HandRowAreaModel enemyHandRowAreaModel = playerModel.enemySinglePlayerAreaModel.handRowAreaModel;
            // 能力5，非英雄牌
            List<int> playerInfoIdList = new List<int> { 2002 };
            playerModel.SetBackupCardInfoIdList(playerInfoIdList);
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_INIT_HAND_CARD);
            playerModel.DrawInitHandCard();
            playerModel.ReDrawInitHandCard();
            // 开始对局
            // 等待host出牌
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            // player出牌，打出普通牌
            playerModel.ChooseCard(selfHandRowAreaModel.cardList[0]);
            // 等待host出牌
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            Assert.AreEqual(0, enemyHandRowAreaModel.cardList.Count);
            Assert.AreEqual(5, playerModel.selfSinglePlayerAreaModel.GetCurrentPower());
            Assert.AreEqual(5, playerModel.enemySinglePlayerAreaModel.GetCurrentPower());
        });
        hostThread.Start();
        playerThread.Start();
        while (hostThread.IsAlive) {
            yield return null;
        }
        while (playerThread.IsAlive) {
            yield return null;
        }
    }

    // 测试退部技能：无目标
    [UnityTest]
    public IEnumerator ScorchNoTarget()
    {
        // host先手
        SetIsHostFirst(true);
        Thread hostThread = new Thread(() => {
            HandRowAreaModel selfHandRowAreaModel = hostModel.selfSinglePlayerAreaModel.handRowAreaModel;
            HandRowAreaModel enemyHandRowAreaModel = hostModel.enemySinglePlayerAreaModel.handRowAreaModel;
            // 英雄牌（10），退部
            List<int> hostInfoIdList = new List<int> { 2005, 5002 };
            // backup
            hostModel.SetBackupCardInfoIdList(hostInfoIdList);
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_INIT_HAND_CARD);
            // draw card
            hostModel.DrawInitHandCard();
            hostModel.ReDrawInitHandCard();
            CardModel roleCard;
            CardModel scorchCard;
            if (selfHandRowAreaModel.cardList[0].cardInfo.ability == CardAbility.Scorch) {
                roleCard = selfHandRowAreaModel.cardList[1];
                scorchCard = selfHandRowAreaModel.cardList[0];
            } else {
                roleCard = selfHandRowAreaModel.cardList[0];
                scorchCard = selfHandRowAreaModel.cardList[1];
            }
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            // 开始对局
            // host出牌
            hostModel.ChooseCard(roleCard);
            // 等待player出牌
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            // host出牌，打出scorch牌，但无目标，相当于打出一张虚空牌
            hostModel.ChooseCard(scorchCard);
            Assert.AreEqual(0, hostSfxPlayCount[AudioManager.SFXType.Scorch]);
            Assert.AreEqual(CardLocation.None, scorchCard.cardLocation);
            Assert.AreEqual(10, hostModel.selfSinglePlayerAreaModel.GetCurrentPower());
            Assert.AreEqual(10, hostModel.enemySinglePlayerAreaModel.GetCurrentPower());
        });
        Thread playerThread = new Thread(() => {
            HandRowAreaModel selfHandRowAreaModel = playerModel.selfSinglePlayerAreaModel.handRowAreaModel;
            HandRowAreaModel enemyHandRowAreaModel = playerModel.enemySinglePlayerAreaModel.handRowAreaModel;
            // 英雄牌（10）
            List<int> playerInfoIdList = new List<int> { 2005 };
            playerModel.SetBackupCardInfoIdList(playerInfoIdList);
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_INIT_HAND_CARD);
            playerModel.DrawInitHandCard();
            playerModel.ReDrawInitHandCard();
            // 开始对局
            // 等待host出牌
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            // player出牌
            playerModel.ChooseCard(selfHandRowAreaModel.cardList[0]);
            // 等待host出牌
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            Assert.AreEqual(0, playerSfxPlayCount[AudioManager.SFXType.Scorch]);
            Assert.AreEqual(10, playerModel.selfSinglePlayerAreaModel.GetCurrentPower());
            Assert.AreEqual(10, playerModel.enemySinglePlayerAreaModel.GetCurrentPower());
        });
        hostThread.Start();
        playerThread.Start();
        while (hostThread.IsAlive) {
            yield return null;
        }
        while (playerThread.IsAlive) {
            yield return null;
        }
    }

    // 测试退部技能：移除卡牌
    [UnityTest]
    public IEnumerator ScorchNormal()
    {
        // host先手
        SetIsHostFirst(true);
        Thread hostThread = new Thread(() => {
            HandRowAreaModel selfHandRowAreaModel = hostModel.selfSinglePlayerAreaModel.handRowAreaModel;
            HandRowAreaModel enemyHandRowAreaModel = hostModel.enemySinglePlayerAreaModel.handRowAreaModel;
            // 普通牌（5），退部
            List<int> hostInfoIdList = new List<int> { 2002, 5002 };
            // backup
            hostModel.SetBackupCardInfoIdList(hostInfoIdList);
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_INIT_HAND_CARD);
            // draw card
            hostModel.DrawInitHandCard();
            hostModel.ReDrawInitHandCard();
            CardModel roleCard;
            CardModel scorchCard;
            if (selfHandRowAreaModel.cardList[0].cardInfo.ability == CardAbility.Scorch) {
                roleCard = selfHandRowAreaModel.cardList[1];
                scorchCard = selfHandRowAreaModel.cardList[0];
            } else {
                roleCard = selfHandRowAreaModel.cardList[0];
                scorchCard = selfHandRowAreaModel.cardList[1];
            }
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            // 开始对局
            // host出牌
            hostModel.ChooseCard(roleCard);
            // 等待player出牌
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            // host出牌，打出scorch牌
            hostModel.ChooseCard(scorchCard);
            Assert.AreEqual(1, hostSfxPlayCount[AudioManager.SFXType.Scorch]);
            Assert.AreEqual(CardLocation.None, scorchCard.cardLocation);
            Assert.AreEqual(CardLocation.DiscardArea, roleCard.cardLocation);
            Assert.AreEqual(0, hostModel.selfSinglePlayerAreaModel.GetCurrentPower());
            Assert.AreEqual(10, hostModel.enemySinglePlayerAreaModel.GetCurrentPower());
        });
        Thread playerThread = new Thread(() => {
            HandRowAreaModel selfHandRowAreaModel = playerModel.selfSinglePlayerAreaModel.handRowAreaModel;
            HandRowAreaModel enemyHandRowAreaModel = playerModel.enemySinglePlayerAreaModel.handRowAreaModel;
            // 英雄牌（10）
            List<int> playerInfoIdList = new List<int> { 2005 };
            playerModel.SetBackupCardInfoIdList(playerInfoIdList);
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_INIT_HAND_CARD);
            playerModel.DrawInitHandCard();
            playerModel.ReDrawInitHandCard();
            // 开始对局
            // 等待host出牌
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            // player出牌
            playerModel.ChooseCard(selfHandRowAreaModel.cardList[0]);
            // 等待host出牌
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            Assert.AreEqual(1, playerSfxPlayCount[AudioManager.SFXType.Scorch]);
            Assert.AreEqual(10, playerModel.selfSinglePlayerAreaModel.GetCurrentPower());
            Assert.AreEqual(0, playerModel.enemySinglePlayerAreaModel.GetCurrentPower());
        });
        hostThread.Start();
        playerThread.Start();
        while (hostThread.IsAlive) {
            yield return null;
        }
        while (playerThread.IsAlive) {
            yield return null;
        }
    }

    // 测试退部技能：角色牌带退部技能
    [UnityTest]
    public IEnumerator ScorchRole()
    {
        // host先手
        SetIsHostFirst(true);
        Thread hostThread = new Thread(() => {
            HandRowAreaModel selfHandRowAreaModel = hostModel.selfSinglePlayerAreaModel.handRowAreaModel;
            HandRowAreaModel enemyHandRowAreaModel = hostModel.enemySinglePlayerAreaModel.handRowAreaModel;
            // 退部角色牌（5），普通牌（5）
            List<int> hostInfoIdList = new List<int> { 1013, 1014 };
            // backup
            hostModel.SetBackupCardInfoIdList(hostInfoIdList);
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_INIT_HAND_CARD);
            // draw card
            hostModel.DrawInitHandCard();
            hostModel.ReDrawInitHandCard();
            CardModel normalCard;
            CardModel scorchCard;
            if (selfHandRowAreaModel.cardList[0].cardInfo.ability == CardAbility.Scorch) {
                normalCard = selfHandRowAreaModel.cardList[1];
                scorchCard = selfHandRowAreaModel.cardList[0];
            } else {
                normalCard = selfHandRowAreaModel.cardList[0];
                scorchCard = selfHandRowAreaModel.cardList[1];
            }
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            // 开始对局
            // host出牌
            hostModel.ChooseCard(normalCard);
            // 等待player出牌
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            // host出牌，打出scorch牌
            hostModel.ChooseCard(scorchCard);
            Assert.AreEqual(1, hostSfxPlayCount[AudioManager.SFXType.Scorch]);
            Assert.AreEqual(CardLocation.BattleArea, scorchCard.cardLocation);
            Assert.AreEqual(CardLocation.DiscardArea, normalCard.cardLocation);
            Assert.AreEqual(5, hostModel.selfSinglePlayerAreaModel.GetCurrentPower());
            Assert.AreEqual(10, hostModel.enemySinglePlayerAreaModel.GetCurrentPower());
        });
        Thread playerThread = new Thread(() => {
            HandRowAreaModel selfHandRowAreaModel = playerModel.selfSinglePlayerAreaModel.handRowAreaModel;
            HandRowAreaModel enemyHandRowAreaModel = playerModel.enemySinglePlayerAreaModel.handRowAreaModel;
            // 英雄牌（10）
            List<int> playerInfoIdList = new List<int> { 2005 };
            playerModel.SetBackupCardInfoIdList(playerInfoIdList);
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_INIT_HAND_CARD);
            playerModel.DrawInitHandCard();
            playerModel.ReDrawInitHandCard();
            // 开始对局
            // 等待host出牌
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            // player出牌
            playerModel.ChooseCard(selfHandRowAreaModel.cardList[0]);
            // 等待host出牌
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            Assert.AreEqual(1, playerSfxPlayCount[AudioManager.SFXType.Scorch]);
            Assert.AreEqual(10, playerModel.selfSinglePlayerAreaModel.GetCurrentPower());
            Assert.AreEqual(5, playerModel.enemySinglePlayerAreaModel.GetCurrentPower());
        });
        hostThread.Start();
        playerThread.Start();
        while (hostThread.IsAlive) {
            yield return null;
        }
        while (playerThread.IsAlive) {
            yield return null;
        }
    }

    // 测试天气牌
    [UnityTest]
    public IEnumerator Weather()
    {
        // host先手
        SetIsHostFirst(true);
        Thread hostThread = new Thread(() => {
            HandRowAreaModel selfHandRowAreaModel = hostModel.selfSinglePlayerAreaModel.handRowAreaModel;
            HandRowAreaModel enemyHandRowAreaModel = hostModel.enemySinglePlayerAreaModel.handRowAreaModel;
            // 普通木管牌（5），sunfes
            List<int> hostInfoIdList = new List<int> { 2002, 5003 };
            // backup
            hostModel.SetBackupCardInfoIdList(hostInfoIdList);
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_INIT_HAND_CARD);
            // draw card
            hostModel.DrawInitHandCard();
            hostModel.ReDrawInitHandCard();
            CardModel roleCard;
            CardModel sunfesCard;
            if (selfHandRowAreaModel.cardList[0].cardInfo.ability == CardAbility.SunFes) {
                roleCard = selfHandRowAreaModel.cardList[1];
                sunfesCard = selfHandRowAreaModel.cardList[0];
            } else {
                roleCard = selfHandRowAreaModel.cardList[0];
                sunfesCard = selfHandRowAreaModel.cardList[1];
            }
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            // 开始对局
            // host出牌
            hostModel.ChooseCard(roleCard);
            // 等待player出牌
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            // host出牌，打出天气牌
            hostModel.ChooseCard(sunfesCard);
            Assert.AreEqual(CardLocation.WeatherCardArea, sunfesCard.cardLocation);
            Assert.AreEqual(1, hostModel.selfSinglePlayerAreaModel.GetCurrentPower());
            Assert.AreEqual(10, hostModel.enemySinglePlayerAreaModel.GetCurrentPower());
        });
        Thread playerThread = new Thread(() => {
            HandRowAreaModel selfHandRowAreaModel = playerModel.selfSinglePlayerAreaModel.handRowAreaModel;
            HandRowAreaModel enemyHandRowAreaModel = playerModel.enemySinglePlayerAreaModel.handRowAreaModel;
            // 木管英雄牌（10）
            List<int> playerInfoIdList = new List<int> { 2005 };
            playerModel.SetBackupCardInfoIdList(playerInfoIdList);
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_INIT_HAND_CARD);
            playerModel.DrawInitHandCard();
            playerModel.ReDrawInitHandCard();
            // 开始对局
            // 等待host出牌
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            // player出牌
            playerModel.ChooseCard(selfHandRowAreaModel.cardList[0]);
            // 等待host出牌
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            Assert.AreEqual(10, playerModel.selfSinglePlayerAreaModel.GetCurrentPower());
            Assert.AreEqual(1, playerModel.enemySinglePlayerAreaModel.GetCurrentPower());
        });
        hostThread.Start();
        playerThread.Start();
        while (hostThread.IsAlive) {
            yield return null;
        }
        while (playerThread.IsAlive) {
            yield return null;
        }
    }

    // 测试天气牌导致卡牌被移除
    [UnityTest]
    public IEnumerator WeatherDead()
    {
        // host先手
        SetIsHostFirst(true);
        Thread hostThread = new Thread(() => {
            HandRowAreaModel selfHandRowAreaModel = hostModel.selfSinglePlayerAreaModel.handRowAreaModel;
            HandRowAreaModel enemyHandRowAreaModel = hostModel.enemySinglePlayerAreaModel.handRowAreaModel;
            // 普通木管牌（5），sunfes
            List<int> hostInfoIdList = new List<int> { 2002, 5003 };
            // backup
            hostModel.SetBackupCardInfoIdList(hostInfoIdList);
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_INIT_HAND_CARD);
            // draw card
            hostModel.DrawInitHandCard();
            hostModel.ReDrawInitHandCard();
            CardModel roleCard;
            CardModel sunfesCard;
            if (selfHandRowAreaModel.cardList[0].cardInfo.ability == CardAbility.SunFes) {
                roleCard = selfHandRowAreaModel.cardList[1];
                sunfesCard = selfHandRowAreaModel.cardList[0];
            } else {
                roleCard = selfHandRowAreaModel.cardList[0];
                sunfesCard = selfHandRowAreaModel.cardList[1];
            }
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            // 开始对局
            // host出牌
            hostModel.ChooseCard(roleCard);
            // 等待player出牌
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            // host出牌，打出天气牌
            hostModel.ChooseCard(sunfesCard);
            Assert.AreEqual(1, hostSfxPlayCount[AudioManager.SFXType.Scorch]);
            Assert.AreEqual(1, hostSfxPlayCount[AudioManager.SFXType.Attack]);
            Assert.AreEqual(CardLocation.WeatherCardArea, sunfesCard.cardLocation);
            Assert.AreEqual(CardLocation.DiscardArea, roleCard.cardLocation);
            Assert.AreEqual(0, hostModel.selfSinglePlayerAreaModel.GetCurrentPower());
            Assert.AreEqual(10, hostModel.enemySinglePlayerAreaModel.GetCurrentPower());
        });
        Thread playerThread = new Thread(() => {
            HandRowAreaModel selfHandRowAreaModel = playerModel.selfSinglePlayerAreaModel.handRowAreaModel;
            HandRowAreaModel enemyHandRowAreaModel = playerModel.enemySinglePlayerAreaModel.handRowAreaModel;
            // 攻击4英雄牌（10）
            List<int> playerInfoIdList = new List<int> { 2023 };
            playerModel.SetBackupCardInfoIdList(playerInfoIdList);
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_INIT_HAND_CARD);
            playerModel.DrawInitHandCard();
            playerModel.ReDrawInitHandCard();
            // 开始对局
            // 等待host出牌
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            // player出牌
            playerModel.ChooseCard(selfHandRowAreaModel.cardList[0]);
            playerModel.ChooseCard(playerModel.enemySinglePlayerAreaModel.woodRowAreaModel.cardList[0]); // 攻击木管牌
            // 等待host出牌
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            Assert.AreEqual(1, playerSfxPlayCount[AudioManager.SFXType.Scorch]);
            Assert.AreEqual(1, playerSfxPlayCount[AudioManager.SFXType.Attack]);
            Assert.AreEqual(10, playerModel.selfSinglePlayerAreaModel.GetCurrentPower());
            Assert.AreEqual(0, playerModel.enemySinglePlayerAreaModel.GetCurrentPower());
        });
        hostThread.Start();
        playerThread.Start();
        while (hostThread.IsAlive) {
            yield return null;
        }
        while (playerThread.IsAlive) {
            yield return null;
        }
    }

    // 测试天气牌：单局结束后需要清除
    [UnityTest]
    public IEnumerator WeatherSetFinish()
    {
        // host 先手
        SetIsHostFirst(true);
        Thread hostThread = new Thread(() => {
            HandRowAreaModel selfHandRowAreaModel = hostModel.selfSinglePlayerAreaModel.handRowAreaModel;
            HandRowAreaModel enemyHandRowAreaModel = hostModel.enemySinglePlayerAreaModel.handRowAreaModel;
            // 普通木管牌（5），sunfes
            List<int> hostInfoIdList = new List<int> { 2002, 5003 };
            // backup
            hostModel.SetBackupCardInfoIdList(hostInfoIdList);
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_INIT_HAND_CARD);
            // draw card
            hostModel.DrawInitHandCard();
            hostModel.ReDrawInitHandCard();
            CardModel roleCard;
            CardModel sunfesCard;
            if (selfHandRowAreaModel.cardList[0].cardInfo.ability == CardAbility.SunFes) {
                roleCard = selfHandRowAreaModel.cardList[1];
                sunfesCard = selfHandRowAreaModel.cardList[0];
            } else {
                roleCard = selfHandRowAreaModel.cardList[0];
                sunfesCard = selfHandRowAreaModel.cardList[1];
            }
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            // 开始对局
            // host出牌，打出天气牌
            hostModel.ChooseCard(sunfesCard);
            // 等待player pass与出牌
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            hostModel.Pass();
            // host出牌
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            hostModel.ChooseCard(roleCard);
            Assert.AreEqual(CardLocation.None, sunfesCard.cardLocation);
            Assert.AreEqual(5, hostModel.selfSinglePlayerAreaModel.GetCurrentPower());
            Assert.AreEqual(10, hostModel.enemySinglePlayerAreaModel.GetCurrentPower());
        });
        Thread playerThread = new Thread(() => {
            HandRowAreaModel selfHandRowAreaModel = playerModel.selfSinglePlayerAreaModel.handRowAreaModel;
            HandRowAreaModel enemyHandRowAreaModel = playerModel.enemySinglePlayerAreaModel.handRowAreaModel;
            // 木管英雄牌（10）
            List<int> playerInfoIdList = new List<int> { 2005 };
            playerModel.SetBackupCardInfoIdList(playerInfoIdList);
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_INIT_HAND_CARD);
            playerModel.DrawInitHandCard();
            playerModel.ReDrawInitHandCard();
            // 开始对局
            // 等待host出牌
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            playerModel.Pass();
            // player出牌
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            playerModel.ChooseCard(selfHandRowAreaModel.cardList[0]);
            // 等待host出牌
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            Assert.AreEqual(10, playerModel.selfSinglePlayerAreaModel.GetCurrentPower());
            Assert.AreEqual(5, playerModel.enemySinglePlayerAreaModel.GetCurrentPower());
        });
        hostThread.Start();
        playerThread.Start();
        while (hostThread.IsAlive) {
            yield return null;
        }
        while (playerThread.IsAlive) {
            yield return null;
        }
    }

    // 测试清除天气
    [UnityTest]
    public IEnumerator WeatherClear()
    {
        // host 先手
        SetIsHostFirst(true);
        Thread hostThread = new Thread(() => {
            HandRowAreaModel selfHandRowAreaModel = hostModel.selfSinglePlayerAreaModel.handRowAreaModel;
            HandRowAreaModel enemyHandRowAreaModel = hostModel.enemySinglePlayerAreaModel.handRowAreaModel;
            // 普通木管牌（5），sunfes
            List<int> hostInfoIdList = new List<int> { 2002, 5003 };
            // backup
            hostModel.SetBackupCardInfoIdList(hostInfoIdList);
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_INIT_HAND_CARD);
            // draw card
            hostModel.DrawInitHandCard();
            hostModel.ReDrawInitHandCard();
            CardModel roleCard;
            CardModel sunfesCard;
            if (selfHandRowAreaModel.cardList[0].cardInfo.ability == CardAbility.SunFes) {
                roleCard = selfHandRowAreaModel.cardList[1];
                sunfesCard = selfHandRowAreaModel.cardList[0];
            } else {
                roleCard = selfHandRowAreaModel.cardList[0];
                sunfesCard = selfHandRowAreaModel.cardList[1];
            }
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            // 开始对局
            // host出牌，打出天气牌
            hostModel.ChooseCard(sunfesCard);
            // 等待player出牌
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            // host出牌
            hostModel.ChooseCard(roleCard);
            Assert.AreEqual(CardLocation.None, sunfesCard.cardLocation);
            Assert.AreEqual(5, hostModel.selfSinglePlayerAreaModel.GetCurrentPower());
            Assert.AreEqual(0, hostModel.enemySinglePlayerAreaModel.GetCurrentPower());
        });
        Thread playerThread = new Thread(() => {
            HandRowAreaModel selfHandRowAreaModel = playerModel.selfSinglePlayerAreaModel.handRowAreaModel;
            HandRowAreaModel enemyHandRowAreaModel = playerModel.enemySinglePlayerAreaModel.handRowAreaModel;
            // 清除天气牌
            List<int> playerInfoIdList = new List<int> { 5006 };
            playerModel.SetBackupCardInfoIdList(playerInfoIdList);
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_INIT_HAND_CARD);
            playerModel.DrawInitHandCard();
            playerModel.ReDrawInitHandCard();
            // 开始对局
            // player出牌
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            playerModel.ChooseCard(selfHandRowAreaModel.cardList[0]);
            // 等待host出牌
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            Assert.AreEqual(0, playerModel.selfSinglePlayerAreaModel.GetCurrentPower());
            Assert.AreEqual(5, playerModel.enemySinglePlayerAreaModel.GetCurrentPower());
        });
        hostThread.Start();
        playerThread.Start();
        while (hostThread.IsAlive) {
            yield return null;
        }
        while (playerThread.IsAlive) {
            yield return null;
        }
    }

    // 测试指导老师
    [UnityTest]
    public IEnumerator HornUtil()
    {
        // host先手
        SetIsHostFirst(true);
        Thread hostThread = new Thread(() => {
            HandRowAreaModel selfHandRowAreaModel = hostModel.selfSinglePlayerAreaModel.handRowAreaModel;
            HandRowAreaModel enemyHandRowAreaModel = hostModel.enemySinglePlayerAreaModel.handRowAreaModel;
            // 普通木管牌（5），horn util
            List<int> hostInfoIdList = new List<int> { 2002, 5008 };
            // backup
            hostModel.SetBackupCardInfoIdList(hostInfoIdList);
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_INIT_HAND_CARD);
            // draw card
            hostModel.DrawInitHandCard();
            hostModel.ReDrawInitHandCard();
            CardModel roleCard;
            CardModel hornUtilCard;
            if (selfHandRowAreaModel.cardList[0].cardInfo.ability == CardAbility.HornUtil) {
                roleCard = selfHandRowAreaModel.cardList[1];
                hornUtilCard = selfHandRowAreaModel.cardList[0];
            } else {
                roleCard = selfHandRowAreaModel.cardList[0];
                hornUtilCard = selfHandRowAreaModel.cardList[1];
            }
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            // 开始对局
            // host出牌
            hostModel.ChooseCard(roleCard);
            // 等待player出牌
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            // host出牌
            hostModel.ChooseCard(hornUtilCard);
            hostModel.ChooseHornUtilArea(hostModel.selfSinglePlayerAreaModel.woodRowAreaModel);
            Assert.AreEqual(CardLocation.BattleArea, hornUtilCard.cardLocation);
            Assert.AreEqual(10, hostModel.selfSinglePlayerAreaModel.GetCurrentPower());
            Assert.AreEqual(10, hostModel.enemySinglePlayerAreaModel.GetCurrentPower());
        });
        Thread playerThread = new Thread(() => {
            HandRowAreaModel selfHandRowAreaModel = playerModel.selfSinglePlayerAreaModel.handRowAreaModel;
            HandRowAreaModel enemyHandRowAreaModel = playerModel.enemySinglePlayerAreaModel.handRowAreaModel;
            // 木管英雄牌（10）
            List<int> playerInfoIdList = new List<int> { 2005 };
            playerModel.SetBackupCardInfoIdList(playerInfoIdList);
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_INIT_HAND_CARD);
            playerModel.DrawInitHandCard();
            playerModel.ReDrawInitHandCard();
            // 开始对局
            // 等待host出牌
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            // player出牌
            playerModel.ChooseCard(selfHandRowAreaModel.cardList[0]);
            // 等待host出牌
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            Assert.AreEqual(10, playerModel.selfSinglePlayerAreaModel.GetCurrentPower());
            Assert.AreEqual(10, playerModel.enemySinglePlayerAreaModel.GetCurrentPower());
        });
        hostThread.Start();
        playerThread.Start();
        while (hostThread.IsAlive) {
            yield return null;
        }
        while (playerThread.IsAlive) {
            yield return null;
        }
    }

    // 测试指导老师：单局结束后需要清除
    [UnityTest]
    public IEnumerator HornUtilSetFinish()
    {
        // host先手
        SetIsHostFirst(true);
        Thread hostThread = new Thread(() => {
            HandRowAreaModel selfHandRowAreaModel = hostModel.selfSinglePlayerAreaModel.handRowAreaModel;
            HandRowAreaModel enemyHandRowAreaModel = hostModel.enemySinglePlayerAreaModel.handRowAreaModel;
            // 普通木管牌（5），horn util
            List<int> hostInfoIdList = new List<int> { 2002, 5008 };
            // backup
            hostModel.SetBackupCardInfoIdList(hostInfoIdList);
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_INIT_HAND_CARD);
            // draw card
            hostModel.DrawInitHandCard();
            hostModel.ReDrawInitHandCard();
            CardModel roleCard;
            CardModel hornUtilCard;
            if (selfHandRowAreaModel.cardList[0].cardInfo.ability == CardAbility.HornUtil) {
                roleCard = selfHandRowAreaModel.cardList[1];
                hornUtilCard = selfHandRowAreaModel.cardList[0];
            } else {
                roleCard = selfHandRowAreaModel.cardList[0];
                hornUtilCard = selfHandRowAreaModel.cardList[1];
            }
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            // 开始对局
            // host出牌
            hostModel.ChooseCard(hornUtilCard);
            hostModel.ChooseHornUtilArea(hostModel.selfSinglePlayerAreaModel.woodRowAreaModel);
            // host pass
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            hostModel.Pass();
            // host出牌
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            hostModel.ChooseCard(roleCard);
            Assert.AreEqual(CardLocation.None, hornUtilCard.cardLocation);
            Assert.AreEqual(5, hostModel.selfSinglePlayerAreaModel.GetCurrentPower());
            Assert.AreEqual(10, hostModel.enemySinglePlayerAreaModel.GetCurrentPower());
        });
        Thread playerThread = new Thread(() => {
            HandRowAreaModel selfHandRowAreaModel = playerModel.selfSinglePlayerAreaModel.handRowAreaModel;
            HandRowAreaModel enemyHandRowAreaModel = playerModel.enemySinglePlayerAreaModel.handRowAreaModel;
            // 木管英雄牌（10）
            List<int> playerInfoIdList = new List<int> { 2005 };
            playerModel.SetBackupCardInfoIdList(playerInfoIdList);
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_INIT_HAND_CARD);
            playerModel.DrawInitHandCard();
            playerModel.ReDrawInitHandCard();
            // 开始对局
            // 等待host出牌
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            // player pass
            playerModel.Pass();
            // player出牌
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            playerModel.ChooseCard(selfHandRowAreaModel.cardList[0]);
            // 等待host出牌
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            Assert.AreEqual(10, playerModel.selfSinglePlayerAreaModel.GetCurrentPower());
            Assert.AreEqual(5, playerModel.enemySinglePlayerAreaModel.GetCurrentPower());
        });
        hostThread.Start();
        playerThread.Start();
        while (hostThread.IsAlive) {
            yield return null;
        }
        while (playerThread.IsAlive) {
            yield return null;
        }
    }

    // 测试指导老师，技能流程中断
    [UnityTest]
    public IEnumerator HornUtilInterrupt()
    {
        // host先手
        SetIsHostFirst(true);
        Thread hostThread = new Thread(() => {
            HandRowAreaModel selfHandRowAreaModel = hostModel.selfSinglePlayerAreaModel.handRowAreaModel;
            HandRowAreaModel enemyHandRowAreaModel = hostModel.enemySinglePlayerAreaModel.handRowAreaModel;
            // 普通木管牌（5），horn util
            List<int> hostInfoIdList = new List<int> { 2002, 5008 };
            // backup
            hostModel.SetBackupCardInfoIdList(hostInfoIdList);
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_INIT_HAND_CARD);
            // draw card
            hostModel.DrawInitHandCard();
            hostModel.ReDrawInitHandCard();
            CardModel roleCard;
            CardModel hornUtilCard;
            if (selfHandRowAreaModel.cardList[0].cardInfo.ability == CardAbility.HornUtil) {
                roleCard = selfHandRowAreaModel.cardList[1];
                hornUtilCard = selfHandRowAreaModel.cardList[0];
            } else {
                roleCard = selfHandRowAreaModel.cardList[0];
                hornUtilCard = selfHandRowAreaModel.cardList[1];
            }
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            // 开始对局
            // host出牌
            hostModel.ChooseCard(roleCard);
            // 等待player出牌
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            // host出牌
            hostModel.ChooseCard(hornUtilCard);
            hostModel.InterruptAction();
            Assert.AreEqual(CardLocation.None, hornUtilCard.cardLocation);
            Assert.AreEqual(PlayStateTracker.State.WAIT_ENEMY_ACTION, hostModel.tracker.curState);
            Assert.AreEqual(PlayStateTracker.ActionState.None, hostModel.tracker.actionState);
            Assert.AreEqual(5, hostModel.selfSinglePlayerAreaModel.GetCurrentPower());
            Assert.AreEqual(10, hostModel.enemySinglePlayerAreaModel.GetCurrentPower());
        });
        Thread playerThread = new Thread(() => {
            HandRowAreaModel selfHandRowAreaModel = playerModel.selfSinglePlayerAreaModel.handRowAreaModel;
            HandRowAreaModel enemyHandRowAreaModel = playerModel.enemySinglePlayerAreaModel.handRowAreaModel;
            // 木管英雄牌（10）
            List<int> playerInfoIdList = new List<int> { 2005 };
            playerModel.SetBackupCardInfoIdList(playerInfoIdList);
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_INIT_HAND_CARD);
            playerModel.DrawInitHandCard();
            playerModel.ReDrawInitHandCard();
            // 开始对局
            // 等待host出牌
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            // player出牌
            playerModel.ChooseCard(selfHandRowAreaModel.cardList[0]);
            // 等待host出牌
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            Assert.AreEqual(10, playerModel.selfSinglePlayerAreaModel.GetCurrentPower());
            Assert.AreEqual(5, playerModel.enemySinglePlayerAreaModel.GetCurrentPower());
        });
        hostThread.Start();
        playerThread.Start();
        while (hostThread.IsAlive) {
            yield return null;
        }
        while (playerThread.IsAlive) {
            yield return null;
        }
    }

    // 测试指挥牌
    [UnityTest]
    public IEnumerator Leader()
    {
        // host先手
        SetIsHostFirst(true);
        Thread hostThread = new Thread(() => {
            HandRowAreaModel selfHandRowAreaModel = hostModel.selfSinglePlayerAreaModel.handRowAreaModel;
            HandRowAreaModel enemyHandRowAreaModel = hostModel.enemySinglePlayerAreaModel.handRowAreaModel;
            // 普通木管牌（5），sunfes指挥牌
            List<int> hostInfoIdList = new List<int> { 2002, 5010 };
            // backup
            hostModel.SetBackupCardInfoIdList(hostInfoIdList);
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_INIT_HAND_CARD);
            // draw card
            hostModel.DrawInitHandCard();
            hostModel.ReDrawInitHandCard();
            CardModel roleCard = selfHandRowAreaModel.cardList[0];
            CardModel leaderCard = hostModel.selfSinglePlayerAreaModel.leaderCardAreaModel.cardList[0];
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            Assert.AreEqual(CardLocation.LeaderCardArea, leaderCard.cardLocation);
            // 开始对局
            // host出牌
            hostModel.ChooseCard(roleCard);
            // 等待player出牌
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            // host出牌，打出天气牌
            hostModel.ChooseCard(leaderCard);
            Assert.AreEqual(CardLocation.WeatherCardArea, leaderCard.cardLocation);
            Assert.AreEqual(1, hostModel.selfSinglePlayerAreaModel.GetCurrentPower());
            Assert.AreEqual(10, hostModel.enemySinglePlayerAreaModel.GetCurrentPower());
        });
        Thread playerThread = new Thread(() => {
            HandRowAreaModel selfHandRowAreaModel = playerModel.selfSinglePlayerAreaModel.handRowAreaModel;
            HandRowAreaModel enemyHandRowAreaModel = playerModel.enemySinglePlayerAreaModel.handRowAreaModel;
            // 木管英雄牌（10）
            List<int> playerInfoIdList = new List<int> { 2005 };
            playerModel.SetBackupCardInfoIdList(playerInfoIdList);
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_INIT_HAND_CARD);
            playerModel.DrawInitHandCard();
            playerModel.ReDrawInitHandCard();
            // 开始对局
            // 等待host出牌
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            // player出牌
            playerModel.ChooseCard(selfHandRowAreaModel.cardList[0]);
            // 等待host出牌
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            Assert.AreEqual(10, playerModel.selfSinglePlayerAreaModel.GetCurrentPower());
            Assert.AreEqual(1, playerModel.enemySinglePlayerAreaModel.GetCurrentPower());
        });
        hostThread.Start();
        playerThread.Start();
        while (hostThread.IsAlive) {
            yield return null;
        }
        while (playerThread.IsAlive) {
            yield return null;
        }
    }

    // 测试lip技能常规场景
    [UnityTest]
    public IEnumerator LipNormal()
    {
        // player先手
        SetIsHostFirst(false);
        Thread hostThread = new Thread(() => {
            HandRowAreaModel selfHandRowAreaModel = hostModel.selfSinglePlayerAreaModel.handRowAreaModel;
            HandRowAreaModel enemyHandRowAreaModel = hostModel.enemySinglePlayerAreaModel.handRowAreaModel;
            // Lip牌，能力6
            List<int> hostInfoIdList = new List<int> { 1009 };
            // backup
            hostModel.SetBackupCardInfoIdList(hostInfoIdList);
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_INIT_HAND_CARD);
            // draw card
            hostModel.DrawInitHandCard();
            hostModel.ReDrawInitHandCard();
            // 开始对局，对方先行
            // 等待player出牌
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            // host出牌，打出lip牌
            hostModel.ChooseCard(selfHandRowAreaModel.cardList[0]);
            Assert.AreEqual(0, hostSfxPlayCount[AudioManager.SFXType.Scorch]);
            Assert.AreEqual(1, hostSfxPlayCount[AudioManager.SFXType.Attack]);
            Assert.AreEqual(6, hostModel.selfSinglePlayerAreaModel.GetCurrentPower());
            Assert.AreEqual(3, hostModel.enemySinglePlayerAreaModel.GetCurrentPower());
        });
        Thread playerThread = new Thread(() => {
            HandRowAreaModel selfHandRowAreaModel = playerModel.selfSinglePlayerAreaModel.handRowAreaModel;
            HandRowAreaModel enemyHandRowAreaModel = playerModel.enemySinglePlayerAreaModel.handRowAreaModel;
            // 木管男，能力5
            List<int> playerInfoIdList = new List<int> { 2004 };
            playerModel.SetBackupCardInfoIdList(playerInfoIdList);
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_INIT_HAND_CARD);
            playerModel.DrawInitHandCard();
            playerModel.ReDrawInitHandCard();
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            // 开始对局
            // player出牌
            playerModel.ChooseCard(selfHandRowAreaModel.cardList[0]);
            // 等待host出牌
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            Assert.AreEqual(0, playerSfxPlayCount[AudioManager.SFXType.Scorch]);
            Assert.AreEqual(1, hostSfxPlayCount[AudioManager.SFXType.Attack]);
            Assert.AreEqual(3, playerModel.selfSinglePlayerAreaModel.GetCurrentPower());
            Assert.AreEqual(6, playerModel.enemySinglePlayerAreaModel.GetCurrentPower());
        });
        hostThread.Start();
        playerThread.Start();
        while (hostThread.IsAlive) {
            yield return null;
        }
        while (playerThread.IsAlive) {
            yield return null;
        }
    }

    // 测试lip技能导致卡牌移除
    [UnityTest]
    public IEnumerator LipDead()
    {
        // host先手
        SetIsHostFirst(true);
        Thread hostThread = new Thread(() => {
            HandRowAreaModel selfHandRowAreaModel = hostModel.selfSinglePlayerAreaModel.handRowAreaModel;
            HandRowAreaModel enemyHandRowAreaModel = hostModel.enemySinglePlayerAreaModel.handRowAreaModel;
            // Lip牌，能力6。sunfes
            List<int> hostInfoIdList = new List<int> { 1009, 5003 };
            // backup
            hostModel.SetBackupCardInfoIdList(hostInfoIdList);
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_INIT_HAND_CARD);
            // draw card
            hostModel.DrawInitHandCard();
            hostModel.ReDrawInitHandCard();
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            CardModel lipCard;
            CardModel sunfesCard;
            if (selfHandRowAreaModel.cardList[0].cardInfo.ability == CardAbility.SunFes) {
                lipCard = selfHandRowAreaModel.cardList[1];
                sunfesCard = selfHandRowAreaModel.cardList[0];
            } else {
                lipCard = selfHandRowAreaModel.cardList[0];
                sunfesCard = selfHandRowAreaModel.cardList[1];
            }
            // 开始对局
            // host出牌，打出sunfes
            hostModel.ChooseCard(sunfesCard);
            // 等待player出牌
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            // host出牌，打出lip牌
            hostModel.ChooseCard(lipCard);
            Assert.AreEqual(1, hostSfxPlayCount[AudioManager.SFXType.Scorch]);
            Assert.AreEqual(0, hostSfxPlayCount[AudioManager.SFXType.Attack]);
            Assert.AreEqual(1, hostModel.selfSinglePlayerAreaModel.GetCurrentPower());
            Assert.AreEqual(0, hostModel.enemySinglePlayerAreaModel.GetCurrentPower());
        });
        Thread playerThread = new Thread(() => {
            HandRowAreaModel selfHandRowAreaModel = playerModel.selfSinglePlayerAreaModel.handRowAreaModel;
            HandRowAreaModel enemyHandRowAreaModel = playerModel.enemySinglePlayerAreaModel.handRowAreaModel;
            // 木管男，能力5
            List<int> playerInfoIdList = new List<int> { 2004 };
            playerModel.SetBackupCardInfoIdList(playerInfoIdList);
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_INIT_HAND_CARD);
            playerModel.DrawInitHandCard();
            playerModel.ReDrawInitHandCard();
            // 开始对局
            // 等待host出牌
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            // player出牌
            playerModel.ChooseCard(selfHandRowAreaModel.cardList[0]);
            // 等待host出牌
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            Assert.AreEqual(1, playerSfxPlayCount[AudioManager.SFXType.Scorch]);
            Assert.AreEqual(0, hostSfxPlayCount[AudioManager.SFXType.Attack]);
            Assert.AreEqual(0, playerModel.selfSinglePlayerAreaModel.GetCurrentPower());
            Assert.AreEqual(1, playerModel.enemySinglePlayerAreaModel.GetCurrentPower());
        });
        hostThread.Start();
        playerThread.Start();
        while (hostThread.IsAlive) {
            yield return null;
        }
        while (playerThread.IsAlive) {
            yield return null;
        }
    }

    // 测试Guard牌技能
    [UnityTest]
    public IEnumerator Guard()
    {
        // player先手
        SetIsHostFirst(false);
        Thread hostThread = new Thread(() => {
            HandRowAreaModel selfHandRowAreaModel = hostModel.selfSinglePlayerAreaModel.handRowAreaModel;
            HandRowAreaModel enemyHandRowAreaModel = hostModel.enemySinglePlayerAreaModel.handRowAreaModel;
            // Guard牌，能力4
            List<int> hostInfoIdList = new List<int> { 1044, 1044 };
            // backup
            hostModel.SetBackupCardInfoIdList(hostInfoIdList);
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_INIT_HAND_CARD);
            // draw card
            hostModel.DrawInitHandCard();
            hostModel.ReDrawInitHandCard();
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            // 开始对局
            // 等待player出牌
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            // host出牌，打出Guard牌
            hostModel.ChooseCard(selfHandRowAreaModel.cardList[0]); // 守卫目标不在场，无法攻击
            Assert.AreEqual(0, hostSfxPlayCount[AudioManager.SFXType.Scorch]);
            Assert.AreEqual(0, hostSfxPlayCount[AudioManager.SFXType.Attack]);
            Assert.AreEqual(false, hostModel.IsTurn(true));
            Assert.AreEqual(true, hostModel.IsTurn(false));
            // 等待player出牌
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            // host出牌，打出Guard牌
            hostModel.ChooseCard(selfHandRowAreaModel.cardList[0]);
            hostModel.ChooseCard(hostModel.enemySinglePlayerAreaModel.woodRowAreaModel.cardList[0]); // 选择攻击目标
            Assert.AreEqual(0, hostSfxPlayCount[AudioManager.SFXType.Scorch]);
            Assert.AreEqual(1, hostSfxPlayCount[AudioManager.SFXType.Attack]);
            Assert.AreEqual(8, hostModel.selfSinglePlayerAreaModel.GetCurrentPower());
            Assert.AreEqual(9, hostModel.enemySinglePlayerAreaModel.GetCurrentPower());
        });
        Thread playerThread = new Thread(() => {
            HandRowAreaModel selfHandRowAreaModel = playerModel.selfSinglePlayerAreaModel.handRowAreaModel;
            HandRowAreaModel enemyHandRowAreaModel = playerModel.enemySinglePlayerAreaModel.handRowAreaModel;
            // 普通牌，能力6。香织，能力7
            List<int> playerInfoIdList = new List<int> { 1001, 1023 };
            playerModel.SetBackupCardInfoIdList(playerInfoIdList);
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_INIT_HAND_CARD);
            playerModel.DrawInitHandCard();
            playerModel.ReDrawInitHandCard();
            CardModel normalCard;
            CardModel relatedCard;
            if (selfHandRowAreaModel.cardList[0].cardInfo.infoId == 1023) {
                normalCard = selfHandRowAreaModel.cardList[1];
                relatedCard = selfHandRowAreaModel.cardList[0];
            } else {
                normalCard = selfHandRowAreaModel.cardList[0];
                relatedCard = selfHandRowAreaModel.cardList[1];
            }
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            // 开始对局
            // player出牌
            playerModel.ChooseCard(normalCard);
            // 等待host出牌
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            // player出牌，出relatedCard
            playerModel.ChooseCard(relatedCard);
            // 等待host出牌
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            Assert.AreEqual(0, playerSfxPlayCount[AudioManager.SFXType.Scorch]);
            Assert.AreEqual(1, playerSfxPlayCount[AudioManager.SFXType.Attack]);
            Assert.AreEqual(9, playerModel.selfSinglePlayerAreaModel.GetCurrentPower());
            Assert.AreEqual(8, playerModel.enemySinglePlayerAreaModel.GetCurrentPower());
        });
        hostThread.Start();
        playerThread.Start();
        while (hostThread.IsAlive) {
            yield return null;
        }
        while (playerThread.IsAlive) {
            yield return null;
        }
    }


    // 测试monaka技能
    [UnityTest]
    public IEnumerator Monaka()
    {
        // host先手
        SetIsHostFirst(true);
        Thread hostThread = new Thread(() => {
            HandRowAreaModel selfHandRowAreaModel = hostModel.selfSinglePlayerAreaModel.handRowAreaModel;
            HandRowAreaModel enemyHandRowAreaModel = hostModel.enemySinglePlayerAreaModel.handRowAreaModel;
            // brass monaka牌（2）
            List<int> hostInfoIdList = new List<int> { 1045, 1045, 1045 };
            // backup
            hostModel.SetBackupCardInfoIdList(hostInfoIdList);
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_INIT_HAND_CARD);
            // draw card
            hostModel.DrawInitHandCard();
            hostModel.ReDrawInitHandCard();
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            // 开始对局
            // host出牌，打出monaka牌，但无目标
            CardModel firstCard = selfHandRowAreaModel.cardList[0];
            hostModel.ChooseCard(firstCard);
            Assert.AreEqual(PlayStateTracker.State.WAIT_ENEMY_ACTION, hostModel.tracker.curState);
            // 等待player出牌
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            // host出牌，打出monaka牌
            hostModel.ChooseCard(selfHandRowAreaModel.cardList[0]);
            hostModel.ChooseCard(firstCard);
            Assert.AreEqual(PlayStateTracker.ActionState.None, hostModel.tracker.actionState);
            Assert.AreEqual(PlayStateTracker.State.WAIT_ENEMY_ACTION, hostModel.tracker.curState);
            Assert.AreEqual(6, hostModel.selfSinglePlayerAreaModel.GetCurrentPower());
            Assert.AreEqual(5, hostModel.enemySinglePlayerAreaModel.GetCurrentPower());
            // 等待player出牌
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            // host出牌，打出monaka牌，但是中断
            hostModel.ChooseCard(selfHandRowAreaModel.cardList[0]);
            hostModel.InterruptAction();
            Assert.AreEqual(PlayStateTracker.ActionState.None, hostModel.tracker.actionState);
            Assert.AreEqual(PlayStateTracker.State.WAIT_ENEMY_ACTION, hostModel.tracker.curState);
            Assert.AreEqual(8, hostModel.selfSinglePlayerAreaModel.GetCurrentPower());
            Assert.AreEqual(10, hostModel.enemySinglePlayerAreaModel.GetCurrentPower());
        });
        Thread playerThread = new Thread(() => {
            HandRowAreaModel selfHandRowAreaModel = playerModel.selfSinglePlayerAreaModel.handRowAreaModel;
            HandRowAreaModel enemyHandRowAreaModel = playerModel.enemySinglePlayerAreaModel.handRowAreaModel;
            // 能力5，非英雄牌
            List<int> playerInfoIdList = new List<int> { 2002, 2002 };
            playerModel.SetBackupCardInfoIdList(playerInfoIdList);
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_INIT_HAND_CARD);
            playerModel.DrawInitHandCard();
            playerModel.ReDrawInitHandCard();
            // 开始对局
            // 等待host出牌
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            // player出牌，打出普通牌
            playerModel.ChooseCard(selfHandRowAreaModel.cardList[0]);
            // 等待host出牌
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            Assert.AreEqual(5, playerModel.selfSinglePlayerAreaModel.GetCurrentPower());
            Assert.AreEqual(6, playerModel.enemySinglePlayerAreaModel.GetCurrentPower());
            // player出牌，打出普通牌
            playerModel.ChooseCard(selfHandRowAreaModel.cardList[0]);
            // 等待host出牌
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            Assert.AreEqual(10, playerModel.selfSinglePlayerAreaModel.GetCurrentPower());
            Assert.AreEqual(8, playerModel.enemySinglePlayerAreaModel.GetCurrentPower());
        });
        hostThread.Start();
        playerThread.Start();
        while (hostThread.IsAlive) {
            yield return null;
        }
        while (playerThread.IsAlive) {
            yield return null;
        }
    }

    // 测试久一年技能
    [UnityTest]
    public IEnumerator K1CardGroupAbility()
    {
        // host先手
        SetIsHostFirst(true);
        Thread hostThread = new Thread(() => {
            HandRowAreaModel selfHandRowAreaModel = hostModel.selfSinglePlayerAreaModel.handRowAreaModel;
            HandRowAreaModel enemyHandRowAreaModel = hostModel.enemySinglePlayerAreaModel.handRowAreaModel;
            // 普通牌（7）
            List<int> hostInfoIdList = Enumerable.Repeat(1002, 13).ToList();
            // backup
            hostModel.SetBackupCardInfoIdList(hostInfoIdList);
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_INIT_HAND_CARD);
            // draw card
            hostModel.DrawInitHandCard();
            hostModel.ReDrawInitHandCard();
            // 开始对局
            // host出牌
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            hostModel.ChooseCard(selfHandRowAreaModel.cardList[0]);
            // 等待player出牌
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            // host pass
            hostModel.Pass();
            Thread.Sleep(100);
            // 等待player pass。host赢第一局，第二局host先手
            CheckCurState(hostModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            Assert.AreEqual(1, hostSfxPlayCount[AudioManager.SFXType.SetFinish]);
            Assert.AreEqual(1, hostModel.tracker.setRecordList[0].result);
            Assert.AreEqual(true, hostModel.tracker.setRecordList[1].selfFirst);
            // 第一局赢，抽一张牌
            Assert.AreEqual(2, hostModel.selfSinglePlayerAreaModel.backupCardList.Count);
            Assert.AreEqual(3, hostModel.enemySinglePlayerAreaModel.backupCardList.Count);
        });
        Thread playerThread = new Thread(() => {
            HandRowAreaModel selfHandRowAreaModel = playerModel.selfSinglePlayerAreaModel.handRowAreaModel;
            HandRowAreaModel enemyHandRowAreaModel = playerModel.enemySinglePlayerAreaModel.handRowAreaModel;
            // 普通牌（6）
            List<int> playerInfoIdList = Enumerable.Repeat(1001, 13).ToList();
            playerModel.SetBackupCardInfoIdList(playerInfoIdList);
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_INIT_HAND_CARD);
            playerModel.DrawInitHandCard();
            playerModel.ReDrawInitHandCard();
            // 开始对局
            // 等待host出牌
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            // player出牌
            playerModel.ChooseCard(selfHandRowAreaModel.cardList[0]);
            // 等待host pass
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_SELF_ACTION);
            // player pass
            playerModel.Pass();
            Thread.Sleep(100);
            CheckCurState(playerModel, PlayStateTracker.State.WAIT_ENEMY_ACTION);
            Assert.AreEqual(1, playerSfxPlayCount[AudioManager.SFXType.SetFinish]);
            Assert.AreEqual(-1, playerModel.tracker.setRecordList[0].result);
            Assert.AreEqual(false, playerModel.tracker.setRecordList[1].selfFirst);
            // 第一局没赢，不抽牌
            Assert.AreEqual(3, playerModel.selfSinglePlayerAreaModel.backupCardList.Count);
            Assert.AreEqual(2, playerModel.enemySinglePlayerAreaModel.backupCardList.Count);
        });
        hostThread.Start();
        playerThread.Start();
        while (hostThread.IsAlive) {
            yield return null;
        }
        while (playerThread.IsAlive) {
            yield return null;
        }
    }
}
