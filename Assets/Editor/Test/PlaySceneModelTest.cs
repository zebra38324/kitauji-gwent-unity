using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using System.Linq;
using UnityEngine.TestTools;
using System.Collections;
using System.Threading;

public class PlaySceneModelTest
{
    private FieldInfo allInfoField;

    private PlaySceneModel hostModel;
    private List<ActionEvent> hostRecvList;

    private PlaySceneModel playerModel;
    private List<ActionEvent> playerRecvList;

    [SetUp]
    public void SetUp()
    {
        // 初始化静态卡牌信息列表，避免资源依赖
        allInfoField = typeof(CardGenerator)
            .GetField("allCardInfoList", BindingFlags.Static | BindingFlags.NonPublic)!;
        var infos = new List<CardInfo>
        {
            new CardInfo { chineseName = "普通牌-5", infoId = 1, originPower = 5, cardType = CardType.Normal, badgeType = CardBadgeType.Wood },
            new CardInfo { chineseName = "普通牌-3", infoId = 2, originPower = 3, cardType = CardType.Normal, badgeType = CardBadgeType.Brass },
            new CardInfo { chineseName = "指挥牌", infoId = 3, originPower = 0, cardType = CardType.Leader },
            new CardInfo { chineseName = "指导老师牌", infoId = 4, ability = CardAbility.HornUtil },
            new CardInfo { chineseName = "间谍牌", infoId = 5, originPower = 5, cardType = CardType.Normal, badgeType = CardBadgeType.Wood, ability = CardAbility.Spy },
            new CardInfo { chineseName = "高压牌-5", infoId = 6, originPower = 5, cardType = CardType.Hero, badgeType = CardBadgeType.Brass, ability = CardAbility.Pressure },
        };
        allInfoField.SetValue(null, infos.ToImmutableList());
        hostRecvList = new List<ActionEvent>();
        playerRecvList = new List<ActionEvent>();
    }

    [TearDown]
    public void TearDown()
    {
        hostModel.Release();
        hostModel = null;
        playerModel.Release();
        playerModel = null;
        hostRecvList.Clear();
        playerRecvList.Clear();
    }

    private void InitHostModel()
    {
        hostModel = new PlaySceneModel(
            isHost: true,
            selfName: "host",
            enemyName: "player",
            selfGroup: CardGroup.KumikoFirstYear,
            notify: events => {
                hostRecvList.AddRange(events);
            }
        );
    }

    private void InitPlayerModel()
    {
        playerModel = new PlaySceneModel(
            isHost: false,
            selfName: "player",
            enemyName: "host",
            selfGroup: CardGroup.KumikoFirstYear,
            notify: events => {
                playerRecvList.AddRange(events);
            }
        );
    }

    private void InitModelConn()
    {
        hostModel.battleModel.SendToEnemyFunc += playerModel.battleModel.AddEnemyActionMsg;
        playerModel.battleModel.SendToEnemyFunc += hostModel.battleModel.AddEnemyActionMsg;
    }

    private void SetHostFirst(bool hostFirst)
    {
        FieldInfo randomThreshold = typeof(PlayTracker).GetField("HOST_FIRST_RANDOM_THRESHOLD", BindingFlags.Static | BindingFlags.NonPublic)!;
        if (hostFirst) {
            randomThreshold.SetValue(null, 0);
        } else {
            randomThreshold.SetValue(null, 100);
        }
    }

    // 检测curState，不满足时等待一段时间，超时报错
    private void CheckCurState(PlaySceneModel model, GameState.State expectedState)
    {
        long startTs = KTime.CurrentMill();
        long timeout = 1000; // 1s
        while (model.wholeAreaModel.gameState.curState != expectedState) {
            if (KTime.CurrentMill() - startTs > timeout) {
                break;
            }
            Thread.Sleep(1);
        }
        Assert.AreEqual(expectedState, model.wholeAreaModel.gameState.curState);
    }

    // 需要等待两个线程结束，如果在函数主体内sleep，会导致测试完全“冻结”，因此使用函数主体使用协程，内部使用线程
    // 内部也使用协程，会涉及嵌套协程的问题，不好处理，这测试框架不支持StartCoroutine，只能MoveNext就很抽象
    [UnityTest]
    public IEnumerator Template()
    {
        SetHostFirst(true);
        Thread hostThread = new Thread(() => {
            InitHostModel();
            while (hostModel.battleModel.SendToEnemyFunc == null || playerModel == null || playerModel.battleModel.SendToEnemyFunc == null) {
                Thread.Sleep(1);
            }
            // 以下开始测试逻辑
        });
        Thread playerThread = new Thread(() => {
            InitPlayerModel();
            while (hostModel == null || hostModel.battleModel.SendToEnemyFunc == null || playerModel.battleModel.SendToEnemyFunc == null) {
                Thread.Sleep(1);
            }
            // 以下开始测试逻辑
        });
        hostThread.Start();
        playerThread.Start();
        while (hostThread == null || playerModel == null) {
            yield return null;
        }
        InitModelConn();
        while (hostThread.IsAlive || playerThread.IsAlive) {
            yield return null;
        }
    }

    [UnityTest]
    public IEnumerator SetBackupCardInfoIdList()
    {
        SetHostFirst(true);
        Thread hostThread = new Thread(() => {
            InitHostModel();
            while (hostModel.battleModel.SendToEnemyFunc == null || playerModel == null || playerModel.battleModel.SendToEnemyFunc == null) {
                Thread.Sleep(1);
            }
            // 以下开始测试逻辑
            hostModel.SetBackupCardInfoIdList(new List<int> { 1, 3 });
            CheckCurState(hostModel, GameState.State.WAIT_INIT_HAND_CARD);
            Assert.IsEmpty(hostRecvList);
        });
        Thread playerThread = new Thread(() => {
            InitPlayerModel();
            while (hostModel == null || hostModel.battleModel.SendToEnemyFunc == null || playerModel.battleModel.SendToEnemyFunc == null) {
                Thread.Sleep(1);
            }
            // 以下开始测试逻辑
            playerModel.SetBackupCardInfoIdList(new List<int> { 1, 3 });
            CheckCurState(playerModel, GameState.State.WAIT_INIT_HAND_CARD);
            Assert.IsEmpty(playerRecvList);
        });
        hostThread.Start();
        playerThread.Start();
        while (hostThread == null || playerModel == null) {
            yield return null;
        }
        InitModelConn();
        while (hostThread.IsAlive || playerThread.IsAlive) {
            yield return null;
        }
    }

    [UnityTest]
    public IEnumerator DrawInitHandCard()
    {
        SetHostFirst(true);
        Thread hostThread = new Thread(() => {
            InitHostModel();
            while (hostModel.battleModel.SendToEnemyFunc == null || playerModel == null || playerModel.battleModel.SendToEnemyFunc == null) {
                Thread.Sleep(1);
            }
            // 以下开始测试逻辑
            hostModel.SetBackupCardInfoIdList(new List<int> { 1, 3 });
            CheckCurState(hostModel, GameState.State.WAIT_INIT_HAND_CARD);
            hostModel.DrawInitHandCard();
            Assert.IsEmpty(hostRecvList);
        });
        Thread playerThread = new Thread(() => {
            InitPlayerModel();
            while (hostModel == null || hostModel.battleModel.SendToEnemyFunc == null || playerModel.battleModel.SendToEnemyFunc == null) {
                Thread.Sleep(1);
            }
            // 以下开始测试逻辑
            playerModel.SetBackupCardInfoIdList(new List<int> { 1, 3 });
            CheckCurState(playerModel, GameState.State.WAIT_INIT_HAND_CARD);
            Assert.IsEmpty(playerRecvList);
        });
        hostThread.Start();
        playerThread.Start();
        while (hostThread == null || playerModel == null) {
            yield return null;
        }
        InitModelConn();
        while (hostThread.IsAlive || playerThread.IsAlive) {
            yield return null;
        }
    }

    [UnityTest]
    public IEnumerator ReDrawInitHandCard()
    {
        SetHostFirst(true);
        Thread hostThread = new Thread(() => {
            InitHostModel();
            while (hostModel.battleModel.SendToEnemyFunc == null || playerModel == null || playerModel.battleModel.SendToEnemyFunc == null) {
                Thread.Sleep(1);
            }
            // 以下开始测试逻辑
            hostModel.SetBackupCardInfoIdList(new List<int> { 1, 3 });
            CheckCurState(hostModel, GameState.State.WAIT_INIT_HAND_CARD);
            hostModel.DrawInitHandCard();
            hostModel.ReDrawInitHandCard();
            Assert.AreEqual(1, hostRecvList.Count);
            Assert.AreEqual(ActionEvent.Type.ActionText, hostRecvList[0].type);
            Assert.AreEqual("<color=green>host</color> 完成了初始手牌抽取\n", hostRecvList[0].args[0]);
        });
        Thread playerThread = new Thread(() => {
            InitPlayerModel();
            while (hostModel == null || hostModel.battleModel.SendToEnemyFunc == null || playerModel.battleModel.SendToEnemyFunc == null) {
                Thread.Sleep(1);
            }
            // 以下开始测试逻辑
            playerModel.SetBackupCardInfoIdList(new List<int> { 1, 3 });
            CheckCurState(playerModel, GameState.State.WAIT_INIT_HAND_CARD);
            Assert.GreaterOrEqual(1, playerRecvList.Count); // 由于多线程调度，此处先后顺序不一定
        });
        hostThread.Start();
        playerThread.Start();
        while (hostThread == null || playerModel == null) {
            yield return null;
        }
        InitModelConn();
        while (hostThread.IsAlive || playerThread.IsAlive) {
            yield return null;
        }
    }

    [UnityTest]
    public IEnumerator StartGame()
    {
        SetHostFirst(true);
        Thread hostThread = new Thread(() => {
            InitHostModel();
            while (hostModel.battleModel.SendToEnemyFunc == null || playerModel == null || playerModel.battleModel.SendToEnemyFunc == null) {
                Thread.Sleep(1);
            }
            // 以下开始测试逻辑
            hostModel.SetBackupCardInfoIdList(new List<int> { 1, 3 });
            CheckCurState(hostModel, GameState.State.WAIT_INIT_HAND_CARD);
            hostModel.DrawInitHandCard();
            hostModel.ReDrawInitHandCard();
            CheckCurState(hostModel, GameState.State.WAIT_SELF_ACTION);
            Assert.AreEqual(3, hostRecvList.Count);
            // 这里的先后顺序不一定
            if (((string)(hostRecvList[0].args[0])).Contains("host")) {
                Assert.AreEqual("<color=green>host</color> 完成了初始手牌抽取\n", hostRecvList[0].args[0]);
                Assert.AreEqual("<color=red>player</color> 完成了初始手牌抽取\n", hostRecvList[1].args[0]);
            } else {
                Assert.AreEqual("<color=red>player</color> 完成了初始手牌抽取\n", hostRecvList[0].args[0]);
                Assert.AreEqual("<color=green>host</color> 完成了初始手牌抽取\n", hostRecvList[1].args[0]);
            }
            Assert.AreEqual(ActionEvent.Type.ActionText, hostRecvList[0].type);
            Assert.AreEqual(ActionEvent.Type.ActionText, hostRecvList[1].type);
            Assert.AreEqual(ActionEvent.Type.ActionText, hostRecvList[2].type);
            Assert.AreEqual("新一局开始，<color=green>host</color> 先手\n", hostRecvList[2].args[0]);
        });
        Thread playerThread = new Thread(() => {
            InitPlayerModel();
            while (hostModel == null || hostModel.battleModel.SendToEnemyFunc == null || playerModel.battleModel.SendToEnemyFunc == null) {
                Thread.Sleep(1);
            }
            // 以下开始测试逻辑
            playerModel.SetBackupCardInfoIdList(new List<int> { 1, 3 });
            CheckCurState(playerModel, GameState.State.WAIT_INIT_HAND_CARD);
            playerModel.DrawInitHandCard();
            playerModel.ReDrawInitHandCard();
            CheckCurState(playerModel, GameState.State.WAIT_ENEMY_ACTION);
            Assert.AreEqual(3, playerRecvList.Count);
            // 这里的先后顺序不一定
            if (((string)(playerRecvList[0].args[0])).Contains("host")) {
                Assert.AreEqual("<color=red>host</color> 完成了初始手牌抽取\n", playerRecvList[0].args[0]);
                Assert.AreEqual("<color=green>player</color> 完成了初始手牌抽取\n", playerRecvList[1].args[0]);
            } else {
                Assert.AreEqual("<color=green>player</color> 完成了初始手牌抽取\n", playerRecvList[0].args[0]);
                Assert.AreEqual("<color=red>host</color> 完成了初始手牌抽取\n", playerRecvList[1].args[0]);
            }
            Assert.AreEqual(ActionEvent.Type.ActionText, playerRecvList[0].type);
            Assert.AreEqual(ActionEvent.Type.ActionText, playerRecvList[1].type);
            Assert.AreEqual(ActionEvent.Type.ActionText, playerRecvList[2].type);
            Assert.AreEqual("新一局开始，<color=red>host</color> 先手\n", playerRecvList[2].args[0]);
        });
        hostThread.Start();
        playerThread.Start();
        while (hostThread == null || playerModel == null) {
            yield return null;
        }
        InitModelConn();
        while (hostThread.IsAlive || playerThread.IsAlive) {
            yield return null;
        }
    }

    [UnityTest]
    public IEnumerator Pass()
    {
        SetHostFirst(true);
        Thread hostThread = new Thread(() => {
            InitHostModel();
            while (hostModel.battleModel.SendToEnemyFunc == null || playerModel == null || playerModel.battleModel.SendToEnemyFunc == null) {
                Thread.Sleep(1);
            }
            // 以下开始测试逻辑
            hostModel.SetBackupCardInfoIdList(new List<int> { 1, 3 });
            CheckCurState(hostModel, GameState.State.WAIT_INIT_HAND_CARD);
            hostModel.DrawInitHandCard();
            hostModel.ReDrawInitHandCard();
            CheckCurState(hostModel, GameState.State.WAIT_SELF_ACTION);
            hostModel.Pass();
            int lastIndex = hostRecvList.Count - 1;
            Assert.AreEqual(ActionEvent.Type.ActionText, hostRecvList[lastIndex].type);
            Assert.AreEqual("<color=green>host</color> 放弃跟牌\n", hostRecvList[lastIndex].args[0]);
        });
        Thread playerThread = new Thread(() => {
            InitPlayerModel();
            while (hostModel == null || hostModel.battleModel.SendToEnemyFunc == null || playerModel.battleModel.SendToEnemyFunc == null) {
                Thread.Sleep(1);
            }
            // 以下开始测试逻辑
            playerModel.SetBackupCardInfoIdList(new List<int> { 1, 3 });
            CheckCurState(playerModel, GameState.State.WAIT_INIT_HAND_CARD);
            playerModel.DrawInitHandCard();
            playerModel.ReDrawInitHandCard();
            CheckCurState(playerModel, GameState.State.WAIT_SELF_ACTION);
            int lastIndex = playerRecvList.Count - 1;
            Assert.AreEqual(ActionEvent.Type.ActionText, playerRecvList[lastIndex].type);
            Assert.AreEqual("<color=red>host</color> 放弃跟牌\n", playerRecvList[lastIndex].args[0]);
        });
        hostThread.Start();
        playerThread.Start();
        while (hostThread == null || playerModel == null) {
            yield return null;
        }
        InitModelConn();
        while (hostThread.IsAlive || playerThread.IsAlive) {
            yield return null;
        }
    }

    [UnityTest]
    public IEnumerator ChooseCard()
    {
        SetHostFirst(true);
        Thread hostThread = new Thread(() => {
            InitHostModel();
            while (hostModel.battleModel.SendToEnemyFunc == null || playerModel == null || playerModel.battleModel.SendToEnemyFunc == null) {
                Thread.Sleep(1);
            }
            // 以下开始测试逻辑
            hostModel.SetBackupCardInfoIdList(new List<int> { 1, 3 });
            CheckCurState(hostModel, GameState.State.WAIT_INIT_HAND_CARD);
            hostModel.DrawInitHandCard();
            hostModel.ReDrawInitHandCard();
            CheckCurState(hostModel, GameState.State.WAIT_SELF_ACTION);
            Thread.Sleep(10);
            hostModel.ChooseCard(hostModel.wholeAreaModel.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0]);
            int lastIndex = hostRecvList.Count - 1;
            Assert.AreEqual(ActionEvent.Type.ActionText, hostRecvList[lastIndex].type);
            Assert.AreEqual("<color=green>host</color> 打出卡牌：<b>普通牌-5</b>\n", hostRecvList[lastIndex].args[0]);
        });
        Thread playerThread = new Thread(() => {
            InitPlayerModel();
            while (hostModel == null || hostModel.battleModel.SendToEnemyFunc == null || playerModel.battleModel.SendToEnemyFunc == null) {
                Thread.Sleep(1);
            }
            // 以下开始测试逻辑
            playerModel.SetBackupCardInfoIdList(new List<int> { 1, 3 });
            CheckCurState(playerModel, GameState.State.WAIT_INIT_HAND_CARD);
            playerModel.DrawInitHandCard();
            playerModel.ReDrawInitHandCard();
            CheckCurState(playerModel, GameState.State.WAIT_SELF_ACTION);
            int lastIndex = playerRecvList.Count - 1;
            Assert.AreEqual(ActionEvent.Type.ActionText, playerRecvList[lastIndex].type);
            Assert.AreEqual("<color=red>host</color> 打出卡牌：<b>普通牌-5</b>\n", playerRecvList[lastIndex].args[0]);
        });
        hostThread.Start();
        playerThread.Start();
        while (hostThread == null || playerModel == null) {
            yield return null;
        }
        InitModelConn();
        while (hostThread.IsAlive || playerThread.IsAlive) {
            yield return null;
        }
    }

    [UnityTest]
    public IEnumerator ChooseCard_Spy()
    {
        SetHostFirst(true);
        Thread hostThread = new Thread(() => {
            InitHostModel();
            while (hostModel.battleModel.SendToEnemyFunc == null || playerModel == null || playerModel.battleModel.SendToEnemyFunc == null) {
                Thread.Sleep(1);
            }
            // 以下开始测试逻辑
            hostModel.SetBackupCardInfoIdList(Enumerable.Repeat(5, 13).ToList());
            CheckCurState(hostModel, GameState.State.WAIT_INIT_HAND_CARD);
            hostModel.DrawInitHandCard();
            hostModel.ReDrawInitHandCard();
            CheckCurState(hostModel, GameState.State.WAIT_SELF_ACTION);
            hostModel.ChooseCard(hostModel.wholeAreaModel.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0]);
            int lastIndex = hostRecvList.Count - 1;
            Assert.AreEqual(ActionEvent.Type.ActionText, hostRecvList[lastIndex].type);
            Assert.AreEqual("<color=green>host</color> 抽取了2张牌\n", hostRecvList[lastIndex].args[0]);
        });
        Thread playerThread = new Thread(() => {
            InitPlayerModel();
            while (hostModel == null || hostModel.battleModel.SendToEnemyFunc == null || playerModel.battleModel.SendToEnemyFunc == null) {
                Thread.Sleep(1);
            }
            // 以下开始测试逻辑
            playerModel.SetBackupCardInfoIdList(new List<int> { 1, 3 });
            CheckCurState(playerModel, GameState.State.WAIT_INIT_HAND_CARD);
            playerModel.DrawInitHandCard();
            playerModel.ReDrawInitHandCard();
            CheckCurState(playerModel, GameState.State.WAIT_SELF_ACTION);
            Thread.Sleep(10); // 等待，确保重抽手牌信息达到
            int lastIndex = playerRecvList.Count - 1;
            Assert.AreEqual(ActionEvent.Type.ActionText, playerRecvList[lastIndex].type);
            Assert.AreEqual("<color=red>host</color> 抽取了2张牌\n", playerRecvList[lastIndex].args[0]);
        });
        hostThread.Start();
        playerThread.Start();
        while (hostThread == null || playerModel == null) {
            yield return null;
        }
        InitModelConn();
        while (hostThread.IsAlive || playerThread.IsAlive) {
            yield return null;
        }
    }

    [UnityTest]
    public IEnumerator InterruptAction()
    {
        SetHostFirst(true);
        Thread hostThread = new Thread(() => {
            InitHostModel();
            while (hostModel.battleModel.SendToEnemyFunc == null || playerModel == null || playerModel.battleModel.SendToEnemyFunc == null) {
                Thread.Sleep(1);
            }
            // 以下开始测试逻辑
            hostModel.SetBackupCardInfoIdList(new List<int> { 4 });
            CheckCurState(hostModel, GameState.State.WAIT_INIT_HAND_CARD);
            hostModel.DrawInitHandCard();
            hostModel.ReDrawInitHandCard();
            CheckCurState(hostModel, GameState.State.WAIT_SELF_ACTION);
            hostModel.ChooseCard(hostModel.wholeAreaModel.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0]);
            hostModel.InterruptAction();
            int lastIndex = hostRecvList.Count - 1;
            Assert.AreEqual(ActionEvent.Type.ActionText, hostRecvList[lastIndex].type);
            Assert.AreEqual("<color=green>host</color> 未选择目标\n", hostRecvList[lastIndex].args[0]);
        });
        Thread playerThread = new Thread(() => {
            InitPlayerModel();
            while (hostModel == null || hostModel.battleModel.SendToEnemyFunc == null || playerModel.battleModel.SendToEnemyFunc == null) {
                Thread.Sleep(1);
            }
            // 以下开始测试逻辑
            playerModel.SetBackupCardInfoIdList(new List<int> { 1, 3 });
            CheckCurState(playerModel, GameState.State.WAIT_INIT_HAND_CARD);
            playerModel.DrawInitHandCard();
            playerModel.ReDrawInitHandCard();
            CheckCurState(playerModel, GameState.State.WAIT_SELF_ACTION);
            int lastIndex = playerRecvList.Count - 1;
            Assert.AreEqual(ActionEvent.Type.ActionText, playerRecvList[lastIndex].type);
            Assert.AreEqual("<color=red>host</color> 未选择目标\n", playerRecvList[lastIndex].args[0]);
        });
        hostThread.Start();
        playerThread.Start();
        while (hostThread == null || playerModel == null) {
            yield return null;
        }
        InitModelConn();
        while (hostThread.IsAlive || playerThread.IsAlive) {
            yield return null;
        }
    }

    [UnityTest]
    public IEnumerator ChooseHornUtilArea()
    {
        SetHostFirst(true);
        Thread hostThread = new Thread(() => {
            InitHostModel();
            while (hostModel.battleModel.SendToEnemyFunc == null || playerModel == null || playerModel.battleModel.SendToEnemyFunc == null) {
                Thread.Sleep(1);
            }
            // 以下开始测试逻辑
            hostModel.SetBackupCardInfoIdList(new List<int> { 4 });
            CheckCurState(hostModel, GameState.State.WAIT_INIT_HAND_CARD);
            hostModel.DrawInitHandCard();
            hostModel.ReDrawInitHandCard();
            CheckCurState(hostModel, GameState.State.WAIT_SELF_ACTION);
            hostModel.ChooseCard(hostModel.wholeAreaModel.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0]);
            hostModel.ChooseHornUtilArea(CardBadgeType.Wood);
            int lastIndex = hostRecvList.Count - 1;
            Assert.AreEqual(ActionEvent.Type.Toast, hostRecvList[lastIndex].type);
            Assert.AreEqual("请选择指导老师的目标行", hostRecvList[lastIndex].args[0]);
        });
        Thread playerThread = new Thread(() => {
            InitPlayerModel();
            while (hostModel == null || hostModel.battleModel.SendToEnemyFunc == null || playerModel.battleModel.SendToEnemyFunc == null) {
                Thread.Sleep(1);
            }
            // 以下开始测试逻辑
            playerModel.SetBackupCardInfoIdList(new List<int> { 1, 3 });
            CheckCurState(playerModel, GameState.State.WAIT_INIT_HAND_CARD);
            playerModel.DrawInitHandCard();
            playerModel.ReDrawInitHandCard();
            CheckCurState(playerModel, GameState.State.WAIT_SELF_ACTION);
            int lastIndex = playerRecvList.Count - 1;
            Assert.AreEqual(ActionEvent.Type.ActionText, playerRecvList[lastIndex].type);
            Assert.AreEqual("<color=red>host</color> 打出卡牌：<b>指导老师牌</b>\n", playerRecvList[lastIndex].args[0]);
        });
        hostThread.Start();
        playerThread.Start();
        while (hostThread == null || playerModel == null) {
            yield return null;
        }
        InitModelConn();
        while (hostThread.IsAlive || playerThread.IsAlive) {
            yield return null;
        }
    }

    // 涉及随机数，测试双方状态一致
    [UnityTest]
    public IEnumerator Pressure()
    {
        SetHostFirst(true);
        Thread hostThread = new Thread(() => {
            InitHostModel();
            while (hostModel.battleModel.SendToEnemyFunc == null || playerModel == null || playerModel.battleModel.SendToEnemyFunc == null) {
                Thread.Sleep(1);
            }
            // 以下开始测试逻辑
            hostModel.SetBackupCardInfoIdList(new List<int> { 1, 1, 1, 6 });
            CheckCurState(hostModel, GameState.State.WAIT_INIT_HAND_CARD);
            hostModel.DrawInitHandCard();
            hostModel.ReDrawInitHandCard();
            CheckCurState(hostModel, GameState.State.WAIT_SELF_ACTION);
            hostModel.ChooseCard(hostModel.wholeAreaModel.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList.Find(x => x.cardInfo.infoId == 1));
            CheckCurState(hostModel, GameState.State.WAIT_SELF_ACTION);
            hostModel.ChooseCard(hostModel.wholeAreaModel.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList.Find(x => x.cardInfo.infoId == 1));
            CheckCurState(hostModel, GameState.State.WAIT_SELF_ACTION);
            hostModel.ChooseCard(hostModel.wholeAreaModel.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList.Find(x => x.cardInfo.infoId == 1));
            CheckCurState(hostModel, GameState.State.WAIT_SELF_ACTION);
            hostModel.ChooseCard(hostModel.wholeAreaModel.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0]);
        });
        Thread playerThread = new Thread(() => {
            InitPlayerModel();
            while (hostModel == null || hostModel.battleModel.SendToEnemyFunc == null || playerModel.battleModel.SendToEnemyFunc == null) {
                Thread.Sleep(1);
            }
            // 以下开始测试逻辑
            playerModel.SetBackupCardInfoIdList(new List<int> { 1, 1, 1 });
            CheckCurState(playerModel, GameState.State.WAIT_INIT_HAND_CARD);
            playerModel.DrawInitHandCard();
            playerModel.ReDrawInitHandCard();
            CheckCurState(playerModel, GameState.State.WAIT_SELF_ACTION);
            playerModel.ChooseCard(playerModel.wholeAreaModel.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0]);
            CheckCurState(playerModel, GameState.State.WAIT_SELF_ACTION);
            playerModel.ChooseCard(playerModel.wholeAreaModel.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0]);
            CheckCurState(playerModel, GameState.State.WAIT_SELF_ACTION);
            playerModel.ChooseCard(playerModel.wholeAreaModel.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0]);
            CheckCurState(playerModel, GameState.State.WAIT_SELF_ACTION);
            // check
            var hostRowList = hostModel.wholeAreaModel.selfSinglePlayerAreaModel.battleRowAreaList[(int)CardBadgeType.Wood].cardListModel.cardList;
            var playerRowList = playerModel.wholeAreaModel.enemySinglePlayerAreaModel.battleRowAreaList[(int)CardBadgeType.Wood].cardListModel.cardList;
            Assert.AreEqual(hostRowList.Count, playerRowList.Count);
            for (int i = 0; i < hostRowList.Count; i++) {
                Assert.AreEqual(hostRowList[i].currentPower, playerRowList[i].currentPower);
            }
        });
        hostThread.Start();
        playerThread.Start();
        while (hostThread == null || playerModel == null) {
            yield return null;
        }
        InitModelConn();
        while (hostThread.IsAlive || playerThread.IsAlive) {
            yield return null;
        }
    }

    // 涉及随机数，测试双方状态一致
    [UnityTest]
    public IEnumerator K3Ability()
    {
        SetHostFirst(true);
        Thread hostThread = new Thread(() => {
            InitHostModel();
            while (hostModel.battleModel.SendToEnemyFunc == null || playerModel == null || playerModel.battleModel.SendToEnemyFunc == null) {
                Thread.Sleep(1);
            }
            // 以下开始测试逻辑
            hostModel.SetBackupCardInfoIdList(new List<int> { 1, 1, 1 });
            CheckCurState(hostModel, GameState.State.WAIT_INIT_HAND_CARD);
            hostModel.DrawInitHandCard();
            hostModel.ReDrawInitHandCard();
            CheckCurState(hostModel, GameState.State.WAIT_SELF_ACTION);
            hostModel.ChooseCard(hostModel.wholeAreaModel.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0]);
            CheckCurState(hostModel, GameState.State.WAIT_SELF_ACTION);
            hostModel.ChooseCard(hostModel.wholeAreaModel.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0]);
            CheckCurState(hostModel, GameState.State.WAIT_SELF_ACTION);
            hostModel.ChooseCard(hostModel.wholeAreaModel.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0]);
            CheckCurState(hostModel, GameState.State.WAIT_SELF_ACTION);
            // check
            var hostHostRowList = hostModel.wholeAreaModel.selfSinglePlayerAreaModel.battleRowAreaList[(int)CardBadgeType.Wood].cardListModel.cardList;
            var hostPlayerRowList = hostModel.wholeAreaModel.enemySinglePlayerAreaModel.battleRowAreaList[(int)CardBadgeType.Wood].cardListModel.cardList;
            var playerHostRowList = playerModel.wholeAreaModel.enemySinglePlayerAreaModel.battleRowAreaList[(int)CardBadgeType.Wood].cardListModel.cardList;
            var playerPlayerRowList = playerModel.wholeAreaModel.selfSinglePlayerAreaModel.battleRowAreaList[(int)CardBadgeType.Wood].cardListModel.cardList;
            Assert.AreEqual(hostHostRowList.Count, playerHostRowList.Count);
            Assert.AreEqual(hostPlayerRowList.Count, playerPlayerRowList.Count);
            Assert.AreEqual(hostHostRowList[0].cardInfo.id, playerHostRowList[0].cardInfo.id);
            Assert.AreEqual(hostPlayerRowList[0].cardInfo.id, playerPlayerRowList[0].cardInfo.id);
        });
        Thread playerThread = new Thread(() => {
            InitPlayerModel();
            while (hostModel == null || hostModel.battleModel.SendToEnemyFunc == null || playerModel.battleModel.SendToEnemyFunc == null) {
                Thread.Sleep(1);
            }
            // 以下开始测试逻辑
            playerModel.SetBackupCardInfoIdList(new List<int> { 1, 1, 1 });
            CheckCurState(playerModel, GameState.State.WAIT_INIT_HAND_CARD);
            playerModel.DrawInitHandCard();
            playerModel.ReDrawInitHandCard();
            CheckCurState(playerModel, GameState.State.WAIT_ENEMY_ACTION);
            CheckCurState(playerModel, GameState.State.WAIT_SELF_ACTION);
            playerModel.ChooseCard(playerModel.wholeAreaModel.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0]);
            CheckCurState(playerModel, GameState.State.WAIT_SELF_ACTION);
            playerModel.ChooseCard(playerModel.wholeAreaModel.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0]);
            CheckCurState(playerModel, GameState.State.WAIT_SELF_ACTION);
            playerModel.ChooseCard(playerModel.wholeAreaModel.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0]);
        });
        hostThread.Start();
        playerThread.Start();
        while (hostThread == null || playerModel == null) {
            yield return null;
        }
        InitModelConn();
        while (hostThread.IsAlive || playerThread.IsAlive) {
            yield return null;
        }
    }
}
