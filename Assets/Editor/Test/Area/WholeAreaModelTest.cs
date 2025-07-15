using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Collections.Immutable;
using System.Reflection;
using System.Linq;

public class WholeAreaModelTest
{
    private FieldInfo allInfoField;

    private List<int> allInfoIdList;

    private List<int> allIdList;

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
            new CardInfo { chineseName = "指挥牌", infoId = 3, originPower = 0, cardType = CardType.Leader }
        };
        allInfoField.SetValue(null, infos.ToImmutableList());
        GenInfoIdAndIdList();
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

    private void AppendAllCardInfoList(List<CardInfo> cardInfoList)
    {
        cardInfoList.AddRange((ImmutableList<CardInfo>)(allInfoField.GetValue(null)!));
        allInfoField.SetValue(null, cardInfoList.ToImmutableList());
        GenInfoIdAndIdList();
    }

    private void GenInfoIdAndIdList()
    {
        allInfoIdList = new List<int>();
        allIdList = new List<int>();
        foreach (CardInfo info in (ImmutableList<CardInfo>)(allInfoField.GetValue(null)!)) {
            allInfoIdList.Add(info.infoId);
            allIdList.Add(info.infoId * 10 + 3);
        }
    }

    [Test]
    public void Constructor_InitializesDefaults()
    {
        var model = new WholeAreaModel(true, "S", "E", CardGroup.KumikoSecondYear);
        Assert.IsNotNull(model.playTracker);
        Assert.IsNotNull(model.gameState);
        Assert.IsNotNull(model.selfSinglePlayerAreaModel);
        Assert.IsNotNull(model.enemySinglePlayerAreaModel);
        Assert.IsNotNull(model.weatherAreaModel);
        Assert.IsEmpty(model.actionEventList);
        Assert.AreEqual(GameState.State.WAIT_BACKUP_INFO, model.gameState.curState);
        Assert.AreEqual(true, model.playTracker.isHost);
        Assert.AreEqual("S", model.playTracker.selfPlayerInfo.name);
        Assert.AreEqual("E", model.playTracker.enemyPlayerInfo.name);
        Assert.AreEqual(CardGroup.KumikoSecondYear, model.playTracker.selfPlayerInfo.cardGroup);
    }

    [Test]
    public void SelfInit_Host_HostFirst()
    {
        SetHostFirst(true);
        var model = new WholeAreaModel(true, "S", "E", CardGroup.KumikoSecondYear);
        var updated = model.SelfInit(allInfoIdList);
        Assert.AreEqual(2, updated.selfSinglePlayerAreaModel.handCardAreaModel.backupCardList.Count);
        Assert.AreEqual(1, updated.selfSinglePlayerAreaModel.handCardAreaModel.leaderCardListModel.cardList.Count);
        Assert.IsNotEmpty(updated.actionEventList);
        Assert.AreEqual(ActionEvent.Type.BattleMsg, updated.actionEventList[0].type);
        Assert.AreEqual(BattleModel.ActionType.Init, updated.actionEventList[0].args[0]);
        Assert.AreEqual(CardGroup.KumikoSecondYear, updated.actionEventList[0].args[1]);
        Assert.AreEqual(allInfoIdList, updated.actionEventList[0].args[2]);
        CollectionAssert.AreEquivalent(new List<int> { 11, 21, 31 }, ((List<int>)(updated.actionEventList[0].args[3])));
        Assert.AreEqual(true, updated.actionEventList[0].args[4]);
        Assert.AreEqual(GameState.State.WAIT_BACKUP_INFO, updated.gameState.curState);
        Assert.AreEqual(true, updated.playTracker.setRecordList[0].selfFirst);
    }

    [Test]
    public void SelfInit_Host_NotHostFirst()
    {
        SetHostFirst(false);
        var model = new WholeAreaModel(true, "S", "E", CardGroup.KumikoSecondYear);
        var updated = model.SelfInit(allInfoIdList);
        Assert.AreEqual(false, updated.actionEventList[0].args[4]);
        Assert.AreEqual(false, updated.playTracker.setRecordList[0].selfFirst);
    }

    [Test]
    public void SelfInit_Player()
    {
        var model = new WholeAreaModel(false, "S", "E", CardGroup.KumikoSecondYear);
        var updated = model.SelfInit(allInfoIdList);
        Assert.AreEqual(2, updated.selfSinglePlayerAreaModel.handCardAreaModel.backupCardList.Count);
        Assert.AreEqual(1, updated.selfSinglePlayerAreaModel.handCardAreaModel.leaderCardListModel.cardList.Count);
        Assert.IsNotEmpty(updated.actionEventList);
        Assert.AreEqual(ActionEvent.Type.BattleMsg, updated.actionEventList[0].type);
        Assert.AreEqual(BattleModel.ActionType.Init, updated.actionEventList[0].args[0]);
        Assert.AreEqual(CardGroup.KumikoSecondYear, updated.actionEventList[0].args[1]);
        Assert.AreEqual(allInfoIdList, updated.actionEventList[0].args[2]);
        CollectionAssert.AreEquivalent(new List<int> { 12, 22, 32 }, ((List<int>)(updated.actionEventList[0].args[3])));
        Assert.AreEqual(GameState.State.WAIT_BACKUP_INFO, updated.gameState.curState);
    }

    [Test]
    public void EnemyInit_Host()
    {
        SetHostFirst(false);
        var model = new WholeAreaModel(true, "S", "E", CardGroup.KumikoSecondYear);
        var updated = model.EnemyInit(allInfoIdList, allIdList, CardGroup.KumikoThirdYear, false);
        Assert.AreEqual(2, updated.enemySinglePlayerAreaModel.handCardAreaModel.backupCardList.Count);
        Assert.AreEqual(1, updated.enemySinglePlayerAreaModel.handCardAreaModel.leaderCardListModel.cardList.Count);
        Assert.AreEqual(CardGroup.KumikoThirdYear, updated.playTracker.enemyPlayerInfo.cardGroup);
        Assert.AreEqual(GameState.State.WAIT_BACKUP_INFO, updated.gameState.curState);
    }

    [Test]
    public void EnemyInit_NotHost()
    {
        var model = new WholeAreaModel(false, "S", "E", CardGroup.KumikoSecondYear);
        var updated = model.EnemyInit(allInfoIdList, allIdList, CardGroup.KumikoThirdYear, false);
        Assert.AreEqual(CardGroup.KumikoThirdYear, updated.playTracker.enemyPlayerInfo.cardGroup);
        Assert.AreEqual(GameState.State.WAIT_BACKUP_INFO, updated.gameState.curState);
        Assert.AreEqual(true, updated.playTracker.setRecordList[0].selfFirst);
    }

    [Test]
    public void Init_SelfEnemy()
    {
        var model = new WholeAreaModel(true, "S", "E", CardGroup.KumikoFirstYear);
        var updated = model.SelfInit(allInfoIdList).EnemyInit(allInfoIdList, allIdList, CardGroup.KumikoThirdYear, false);
        Assert.AreEqual(GameState.State.WAIT_INIT_HAND_CARD, updated.gameState.curState);
    }

    [Test]
    public void Init_EnemySelf()
    {
        var model = new WholeAreaModel(true, "S", "E", CardGroup.KumikoFirstYear);
        var updated = model.EnemyInit(allInfoIdList, allIdList, CardGroup.KumikoThirdYear, false).SelfInit(allInfoIdList);
        Assert.AreEqual(GameState.State.WAIT_INIT_HAND_CARD, updated.gameState.curState);
    }

    [Test]
    public void SelfDrawInitHandCard()
    {
        var model = new WholeAreaModel(true, "S", "E", CardGroup.KumikoFirstYear);
        var updated = model.SelfInit(allInfoIdList).EnemyInit(allInfoIdList, allIdList, CardGroup.KumikoThirdYear, false);
        updated = updated.SelfDrawInitHandCard();
        Assert.AreEqual(2, updated.selfSinglePlayerAreaModel.handCardAreaModel.initHandCardListModel.cardList.Count);
        Assert.AreEqual(0, updated.selfSinglePlayerAreaModel.handCardAreaModel.backupCardList.Count);
    }

    [Test]
    public void SelfReDrawInitHandCard()
    {
        var model = new WholeAreaModel(true, "S", "E", CardGroup.KumikoFirstYear);
        var updated = model.SelfInit(allInfoIdList)
            .EnemyInit(allInfoIdList, allIdList, CardGroup.KumikoThirdYear, false)
            .SelfDrawInitHandCard();
        updated = updated with {
            actionEventList = ImmutableList<ActionEvent>.Empty
        };
        updated = updated.ChooseCard(updated.selfSinglePlayerAreaModel.handCardAreaModel.initHandCardListModel.cardList[0], true);
        Assert.AreEqual(0, updated.actionEventList.Count);
        updated = updated with {
            actionEventList = ImmutableList<ActionEvent>.Empty
        };
        updated = updated.SelfReDrawInitHandCard();
        Assert.AreEqual(GameState.State.WAIT_INIT_HAND_CARD, updated.gameState.curState);
        Assert.AreEqual(0, updated.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList.Count);
        Assert.AreEqual(2, updated.actionEventList.Count);
        Assert.AreEqual(ActionEvent.Type.BattleMsg, updated.actionEventList[0].type);
        Assert.AreEqual(BattleModel.ActionType.DrawHandCard, updated.actionEventList[0].args[0]);
        CollectionAssert.AreEquivalent(new List<int> { 11, 21 }, ((List<int>)(updated.actionEventList[0].args[1])));
        Assert.AreEqual(2, ((List<int>)(updated.actionEventList[0].args[1])).Count);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[1].type);
        Assert.AreEqual("<color=green>S</color> 完成了初始手牌抽取\n", updated.actionEventList[1].args[0]);
    }

    [Test]
    public void EnemyDrawInitHandCard()
    {
        var model = new WholeAreaModel(true, "S", "E", CardGroup.KumikoFirstYear);
        var updated = model.SelfInit(allInfoIdList)
            .EnemyInit(allInfoIdList, allIdList, CardGroup.KumikoThirdYear, false);
        updated = updated with {
            actionEventList = ImmutableList<ActionEvent>.Empty
        };
        updated = updated.EnemyDrawInitHandCard(new List<int> { 13, 23 });
        Assert.AreEqual(GameState.State.WAIT_INIT_HAND_CARD, updated.gameState.curState);
        Assert.AreEqual(2, updated.enemySinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList.Count);
        Assert.AreEqual(1, updated.actionEventList.Count);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[0].type);
        Assert.AreEqual("<color=red>E</color> 完成了初始手牌抽取\n", updated.actionEventList[0].args[0]);
    }

    [Test]
    public void TryStartGame_SelfEnemy()
    {
        SetHostFirst(true);
        var model = new WholeAreaModel(true, "S", "E", CardGroup.KumikoFirstYear);
        var updated = model.SelfInit(allInfoIdList)
            .EnemyInit(allInfoIdList, allIdList, CardGroup.KumikoThirdYear, false)
            .SelfDrawInitHandCard()
            .SelfReDrawInitHandCard();
        updated = updated with {
            actionEventList = ImmutableList<ActionEvent>.Empty
        };
        updated = updated.EnemyDrawInitHandCard(new List<int> { 13, 23 });
        Assert.AreEqual(GameState.State.WAIT_SELF_ACTION, updated.gameState.curState);
        Assert.AreEqual(2, updated.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList.Count);
        Assert.AreEqual(2, updated.enemySinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList.Count);
        Assert.AreEqual(2, updated.actionEventList.Count);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[0].type);
        Assert.AreEqual("<color=red>E</color> 完成了初始手牌抽取\n", updated.actionEventList[0].args[0]);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[1].type);
        Assert.AreEqual("新一局开始，<color=green>S</color> 先手\n", updated.actionEventList[1].args[0]);
    }

    [Test]
    public void TryStartGame_EnemySelf()
    {
        SetHostFirst(false);
        var model = new WholeAreaModel(true, "S", "E", CardGroup.KumikoFirstYear);
        var updated = model.SelfInit(allInfoIdList)
            .EnemyInit(allInfoIdList, allIdList, CardGroup.KumikoThirdYear, false)
            .EnemyDrawInitHandCard(new List<int> { 13, 23 })
            .SelfDrawInitHandCard();
        updated = updated with {
            actionEventList = ImmutableList<ActionEvent>.Empty
        };
        updated = updated.SelfReDrawInitHandCard();
        Assert.AreEqual(GameState.State.WAIT_ENEMY_ACTION, updated.gameState.curState);
        Assert.AreEqual(2, updated.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList.Count);
        Assert.AreEqual(2, updated.enemySinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList.Count);
        Assert.AreEqual(3, updated.actionEventList.Count);
        Assert.AreEqual(ActionEvent.Type.BattleMsg, updated.actionEventList[0].type);
        Assert.AreEqual(BattleModel.ActionType.DrawHandCard, updated.actionEventList[0].args[0]);
        CollectionAssert.AreEquivalent(new List<int> { 11, 21 }, ((List<int>)(updated.actionEventList[0].args[1])));
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[1].type);
        Assert.AreEqual("<color=green>S</color> 完成了初始手牌抽取\n", updated.actionEventList[1].args[0]);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[2].type);
        Assert.AreEqual("新一局开始，<color=red>E</color> 先手\n", updated.actionEventList[2].args[0]);
    }

    [Test]
    public void ChooseCard_Normal_Self()
    {
        SetHostFirst(true);
        var model = new WholeAreaModel(true, "S", "E", CardGroup.KumikoFirstYear);
        var infoList = new List<int> { 1, 3 };
        var idList = new List<int> { 13, 33 };
        var updated = model.SelfInit(infoList)
            .EnemyInit(infoList, idList, CardGroup.KumikoThirdYear, false)
            .SelfDrawInitHandCard()
            .SelfReDrawInitHandCard()
            .EnemyDrawInitHandCard(new List<int> { 13 });
        updated = updated with {
            actionEventList = ImmutableList<ActionEvent>.Empty
        };
        var card = updated.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0];
        updated = updated.ChooseCard(card, true);
        Assert.AreEqual(GameState.State.WAIT_ENEMY_ACTION, updated.gameState.curState);
        Assert.AreEqual(5, updated.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(0, updated.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(2, updated.actionEventList.Count);
        Assert.AreEqual(ActionEvent.Type.BattleMsg, updated.actionEventList[0].type);
        Assert.AreEqual(BattleModel.ActionType.ChooseCard, updated.actionEventList[0].args[0]);
        Assert.AreEqual(card.cardInfo.id, updated.actionEventList[0].args[1]);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[1].type);
        Assert.AreEqual(string.Format("<color=green>S</color> 打出卡牌：<b>{0}</b>\n", card.cardInfo.chineseName), updated.actionEventList[1].args[0]);
    }

    [Test]
    public void ChooseCard_Normal_Enemy()
    {
        SetHostFirst(false);
        var model = new WholeAreaModel(true, "S", "E", CardGroup.KumikoFirstYear);
        var infoList = new List<int> { 1, 3 };
        var idList = new List<int> { 13, 33 };
        var updated = model.SelfInit(infoList)
            .EnemyInit(infoList, idList, CardGroup.KumikoThirdYear, false)
            .SelfDrawInitHandCard()
            .SelfReDrawInitHandCard()
            .EnemyDrawInitHandCard(new List<int> { 13 });
        updated = updated with {
            actionEventList = ImmutableList<ActionEvent>.Empty
        };
        var card = updated.enemySinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0];
        updated = updated.ChooseCard(card, false);
        Assert.AreEqual(GameState.State.WAIT_SELF_ACTION, updated.gameState.curState);
        Assert.AreEqual(0, updated.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(5, updated.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(1, updated.actionEventList.Count);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[0].type);
        Assert.AreEqual(string.Format("<color=red>E</color> 打出卡牌：<b>{0}</b>\n", card.cardInfo.chineseName), updated.actionEventList[0].args[0]);
    }

    [Test]
    public void ChooseCard_Spy_Self()
    {
        SetHostFirst(true);
        AppendAllCardInfoList(new List<CardInfo> {
            new CardInfo { chineseName = "间谍牌", infoId = 6, originPower = 5, ability = CardAbility.Spy, badgeType = CardBadgeType.Wood },
        });
        var model = new WholeAreaModel(true, "S", "E", CardGroup.KumikoFirstYear);
        var infoList = Enumerable.Repeat(6, 13).ToList();
        var idList = Enumerable.Range(63, 13).ToList();
        var updated = model.SelfInit(infoList)
            .EnemyInit(infoList, idList, CardGroup.KumikoThirdYear, false)
            .SelfDrawInitHandCard()
            .SelfReDrawInitHandCard()
            .EnemyDrawInitHandCard(Enumerable.Range(63, 10).ToList());
        updated = updated with {
            actionEventList = ImmutableList<ActionEvent>.Empty
        };
        var card = updated.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0];
        updated = updated.ChooseCard(card, true);
        Assert.AreEqual(GameState.State.WAIT_ENEMY_ACTION, updated.gameState.curState);
        Assert.AreEqual(0, updated.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(5, updated.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(11, updated.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList.Count);
        Assert.AreEqual(1, updated.selfSinglePlayerAreaModel.handCardAreaModel.backupCardList.Count);
        Assert.AreEqual(4, updated.actionEventList.Count);
        Assert.AreEqual(ActionEvent.Type.BattleMsg, updated.actionEventList[0].type);
        Assert.AreEqual(BattleModel.ActionType.ChooseCard, updated.actionEventList[0].args[0]);
        Assert.AreEqual(card.cardInfo.id, updated.actionEventList[0].args[1]);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[1].type);
        Assert.AreEqual(string.Format("<color=green>S</color> 打出卡牌：<b>{0}</b>\n", card.cardInfo.chineseName), updated.actionEventList[1].args[0]);
        Assert.AreEqual(ActionEvent.Type.BattleMsg, updated.actionEventList[2].type);
        Assert.AreEqual(BattleModel.ActionType.DrawHandCard, updated.actionEventList[2].args[0]);
        Assert.IsEmpty(((List<int>)(updated.actionEventList[2].args[1])).Except(Enumerable.Range(1, 13).Select(i => i * 10 + 1).ToList()));
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[3].type);
        Assert.AreEqual("<color=green>S</color> 抽取了2张牌\n", updated.actionEventList[3].args[0]);
    }

    [Test]
    public void ChooseCard_Spy_Enemy()
    {
        SetHostFirst(false);
        AppendAllCardInfoList(new List<CardInfo> {
            new CardInfo { chineseName = "间谍牌", infoId = 6, originPower = 5, ability = CardAbility.Spy, badgeType = CardBadgeType.Wood },
        });
        var model = new WholeAreaModel(true, "S", "E", CardGroup.KumikoFirstYear);
        var infoList = Enumerable.Repeat(6, 13).ToList();
        var idList = Enumerable.Range(63, 13).ToList();
        var updated = model.SelfInit(infoList)
            .EnemyInit(infoList, idList, CardGroup.KumikoThirdYear, false)
            .SelfDrawInitHandCard()
            .SelfReDrawInitHandCard()
            .EnemyDrawInitHandCard(Enumerable.Range(63, 10).ToList());
        updated = updated with {
            actionEventList = ImmutableList<ActionEvent>.Empty
        };
        var card = updated.enemySinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0];
        updated = updated.ChooseCard(card, false);
        Assert.AreEqual(GameState.State.WAIT_SELF_ACTION, updated.gameState.curState);
        Assert.AreEqual(5, updated.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(0, updated.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(9, updated.enemySinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList.Count); // enemy不会直接抽牌
        Assert.AreEqual(3, updated.enemySinglePlayerAreaModel.handCardAreaModel.backupCardList.Count);
        Assert.AreEqual(1, updated.actionEventList.Count);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[0].type);
        Assert.AreEqual(string.Format("<color=red>E</color> 打出卡牌：<b>{0}</b>\n", card.cardInfo.chineseName), updated.actionEventList[0].args[0]);
        var drawIdList = updated.enemySinglePlayerAreaModel.handCardAreaModel.backupCardList.GetRange(0, 2).Select(o => o.cardInfo.id).ToList();
        updated = updated with {
            actionEventList = ImmutableList<ActionEvent>.Empty
        };
        updated = updated.EnemyDrawHandCard(drawIdList);
        Assert.AreEqual(1, updated.actionEventList.Count);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[0].type);
        Assert.AreEqual("<color=red>E</color> 抽取了2张牌\n", updated.actionEventList[0].args[0]);
    }

    [Test]
    public void ChooseCard_Attack_NoTarget_Self()
    {
        SetHostFirst(true);
        AppendAllCardInfoList(new List<CardInfo> {
            new CardInfo { chineseName = "攻击牌", infoId = 6, originPower = 5, ability = CardAbility.Attack, badgeType = CardBadgeType.Wood },
        });
        var model = new WholeAreaModel(true, "S", "E", CardGroup.KumikoFirstYear);
        var infoList = new List<int> { 6 };
        var idList = new List<int> { 63 };
        var updated = model.SelfInit(infoList)
            .EnemyInit(infoList, idList, CardGroup.KumikoThirdYear, false)
            .SelfDrawInitHandCard()
            .SelfReDrawInitHandCard()
            .EnemyDrawInitHandCard(idList);
        updated = updated with {
            actionEventList = ImmutableList<ActionEvent>.Empty
        };
        updated = updated.ChooseCard(updated.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0], true);
        Assert.AreEqual(GameState.State.WAIT_ENEMY_ACTION, updated.gameState.curState);
        Assert.AreEqual(GameState.ActionState.None, updated.gameState.actionState);
        Assert.AreEqual(5, updated.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(0, updated.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(3, updated.actionEventList.Count);
        Assert.AreEqual(ActionEvent.Type.BattleMsg, updated.actionEventList[0].type);
        Assert.AreEqual(BattleModel.ActionType.ChooseCard, updated.actionEventList[0].args[0]);
        Assert.AreEqual(11, updated.actionEventList[0].args[1]);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[1].type);
        Assert.AreEqual("<color=green>S</color> 打出卡牌：<b>攻击牌</b>\n", updated.actionEventList[1].args[0]);
        Assert.AreEqual(ActionEvent.Type.Toast, updated.actionEventList[2].type);
        Assert.AreEqual("无可攻击目标", updated.actionEventList[2].args[0]);
    }

    [Test]
    public void ChooseCard_Attack_NoTarget_Enemy()
    {
        SetHostFirst(false);
        AppendAllCardInfoList(new List<CardInfo> {
            new CardInfo { chineseName = "攻击牌", infoId = 6, originPower = 5, ability = CardAbility.Attack, badgeType = CardBadgeType.Wood },
        });
        var model = new WholeAreaModel(true, "S", "E", CardGroup.KumikoFirstYear);
        var infoList = new List<int> { 6 };
        var idList = new List<int> { 63 };
        var updated = model.SelfInit(infoList)
            .EnemyInit(infoList, idList, CardGroup.KumikoThirdYear, false)
            .SelfDrawInitHandCard()
            .SelfReDrawInitHandCard()
            .EnemyDrawInitHandCard(idList);
        updated = updated with {
            actionEventList = ImmutableList<ActionEvent>.Empty
        };
        updated = updated.ChooseCard(updated.enemySinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0], false);
        Assert.AreEqual(GameState.State.WAIT_SELF_ACTION, updated.gameState.curState);
        Assert.AreEqual(GameState.ActionState.None, updated.gameState.actionState);
        Assert.AreEqual(0, updated.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(5, updated.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(1, updated.actionEventList.Count);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[0].type);
        Assert.AreEqual("<color=red>E</color> 打出卡牌：<b>攻击牌</b>\n", updated.actionEventList[0].args[0]);
    }

    [Test]
    public void ChooseCard_Attack_Normal_Self()
    {
        SetHostFirst(false);
        AppendAllCardInfoList(new List<CardInfo> {
            new CardInfo { chineseName = "攻击牌", infoId = 6, originPower = 5, ability = CardAbility.Attack, badgeType = CardBadgeType.Wood, attackNum = 2 },
        });
        var model = new WholeAreaModel(true, "S", "E", CardGroup.KumikoFirstYear);
        var updated = model.SelfInit(new List<int> { 6 })
            .EnemyInit(new List<int> { 1 }, new List<int> { 13 }, CardGroup.KumikoThirdYear, false)
            .SelfDrawInitHandCard()
            .SelfReDrawInitHandCard()
            .EnemyDrawInitHandCard(new List<int> { 13 });
        updated = updated.ChooseCard(updated.enemySinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0], false);
        updated = updated with {
            actionEventList = ImmutableList<ActionEvent>.Empty
        };
        updated = updated.ChooseCard(updated.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0], true);
        Assert.AreEqual(GameState.State.WAIT_SELF_ACTION, updated.gameState.curState);
        Assert.AreEqual(GameState.ActionState.ATTACKING, updated.gameState.actionState);
        Assert.AreEqual(5, updated.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(5, updated.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(3, updated.actionEventList.Count);
        Assert.AreEqual(ActionEvent.Type.BattleMsg, updated.actionEventList[0].type);
        Assert.AreEqual(BattleModel.ActionType.ChooseCard, updated.actionEventList[0].args[0]);
        Assert.AreEqual(11, updated.actionEventList[0].args[1]);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[1].type);
        Assert.AreEqual("<color=green>S</color> 打出卡牌：<b>攻击牌</b>\n", updated.actionEventList[1].args[0]);
        Assert.AreEqual(ActionEvent.Type.Toast, updated.actionEventList[2].type);
        Assert.AreEqual("请选择攻击目标", updated.actionEventList[2].args[0]);
        updated = updated with {
            actionEventList = ImmutableList<ActionEvent>.Empty
        };
        updated = updated.ChooseCard(updated.enemySinglePlayerAreaModel.battleRowAreaList[(int)CardBadgeType.Wood].cardListModel.cardList[0], true);
        Assert.AreEqual(GameState.State.WAIT_ENEMY_ACTION, updated.gameState.curState);
        Assert.AreEqual(GameState.ActionState.None, updated.gameState.actionState);
        Assert.AreEqual(5, updated.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(3, updated.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(3, updated.actionEventList.Count);
        Assert.AreEqual(ActionEvent.Type.BattleMsg, updated.actionEventList[0].type);
        Assert.AreEqual(BattleModel.ActionType.ChooseCard, updated.actionEventList[0].args[0]);
        Assert.AreEqual(13, updated.actionEventList[0].args[1]);
        Assert.AreEqual(ActionEvent.Type.Sfx, updated.actionEventList[1].type);
        Assert.AreEqual(AudioManager.SFXType.Attack, updated.actionEventList[1].args[0]);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[2].type);
        Assert.AreEqual("施放<b>投掷</b>技能，攻击卡牌：<b>普通牌-5</b>\n", updated.actionEventList[2].args[0]);
    }

    [Test]
    public void ChooseCard_Attack_Normal_Enemy()
    {
        SetHostFirst(true);
        AppendAllCardInfoList(new List<CardInfo> {
            new CardInfo { chineseName = "攻击牌", infoId = 6, originPower = 5, ability = CardAbility.Attack, badgeType = CardBadgeType.Wood, attackNum = 2 },
        });
        var model = new WholeAreaModel(true, "S", "E", CardGroup.KumikoFirstYear);
        var updated = model.SelfInit(new List<int> { 1 })
            .EnemyInit(new List<int> { 6 }, new List<int> { 63 }, CardGroup.KumikoThirdYear, false)
            .SelfDrawInitHandCard()
            .SelfReDrawInitHandCard()
            .EnemyDrawInitHandCard(new List<int> { 63 });
        updated = updated.ChooseCard(updated.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0], true);
        updated = updated with {
            actionEventList = ImmutableList<ActionEvent>.Empty
        };
        updated = updated.ChooseCard(updated.enemySinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0], false);
        Assert.AreEqual(GameState.State.WAIT_ENEMY_ACTION, updated.gameState.curState);
        Assert.AreEqual(GameState.ActionState.ATTACKING, updated.gameState.actionState);
        Assert.AreEqual(5, updated.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(5, updated.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(1, updated.actionEventList.Count);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[0].type);
        Assert.AreEqual("<color=red>E</color> 打出卡牌：<b>攻击牌</b>\n", updated.actionEventList[0].args[0]);
        updated = updated with {
            actionEventList = ImmutableList<ActionEvent>.Empty
        };
        updated = updated.ChooseCard(updated.selfSinglePlayerAreaModel.battleRowAreaList[(int)CardBadgeType.Wood].cardListModel.cardList[0], false);
        Assert.AreEqual(GameState.State.WAIT_SELF_ACTION, updated.gameState.curState);
        Assert.AreEqual(GameState.ActionState.None, updated.gameState.actionState);
        Assert.AreEqual(3, updated.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(5, updated.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(2, updated.actionEventList.Count);
        Assert.AreEqual(ActionEvent.Type.Sfx, updated.actionEventList[0].type);
        Assert.AreEqual(AudioManager.SFXType.Attack, updated.actionEventList[0].args[0]);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[1].type);
        Assert.AreEqual("施放<b>投掷</b>技能，攻击卡牌：<b>普通牌-5</b>\n", updated.actionEventList[1].args[0]);
    }

    // 攻击导致退部
    [Test]
    public void ChooseCard_Attack_Dead_Self()
    {
        SetHostFirst(false);
        AppendAllCardInfoList(new List<CardInfo> {
            new CardInfo { chineseName = "攻击牌", infoId = 6, originPower = 5, ability = CardAbility.Attack, badgeType = CardBadgeType.Wood, attackNum = 4 },
            new CardInfo { chineseName = "普通牌-4", infoId = 7, originPower = 4, ability = CardAbility.None, badgeType = CardBadgeType.Wood },
        });
        var model = new WholeAreaModel(true, "S", "E", CardGroup.KumikoFirstYear);
        var updated = model.SelfInit(new List<int> { 6 })
            .EnemyInit(new List<int> { 7 }, new List<int> { 73 }, CardGroup.KumikoThirdYear, false)
            .SelfDrawInitHandCard()
            .SelfReDrawInitHandCard()
            .EnemyDrawInitHandCard(new List<int> { 73 });
        updated = updated.ChooseCard(updated.enemySinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0], false);
        updated = updated.ChooseCard(updated.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0], true);
        updated = updated with {
            actionEventList = ImmutableList<ActionEvent>.Empty
        };
        updated = updated.ChooseCard(updated.enemySinglePlayerAreaModel.battleRowAreaList[(int)CardBadgeType.Wood].cardListModel.cardList[0], true);
        Assert.AreEqual(GameState.State.WAIT_ENEMY_ACTION, updated.gameState.curState);
        Assert.AreEqual(GameState.ActionState.None, updated.gameState.actionState);
        Assert.AreEqual(5, updated.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(0, updated.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(1, updated.enemySinglePlayerAreaModel.discardAreaModel.cardListModel.cardList.Count);
        Assert.AreEqual(3, updated.actionEventList.Count);
        Assert.AreEqual(ActionEvent.Type.BattleMsg, updated.actionEventList[0].type);
        Assert.AreEqual(73, updated.actionEventList[0].args[1]);
        Assert.AreEqual(BattleModel.ActionType.ChooseCard, updated.actionEventList[0].args[0]);
        Assert.AreEqual(ActionEvent.Type.Sfx, updated.actionEventList[1].type);
        Assert.AreEqual(AudioManager.SFXType.Scorch, updated.actionEventList[1].args[0]);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[2].type);
        Assert.AreEqual("施放<b>投掷</b>技能，移除卡牌：<b>普通牌-4</b>\n", updated.actionEventList[2].args[0]);
    }

    [Test]
    public void ChooseCard_Attack_Dead_Enemy()
    {
        SetHostFirst(true);
        AppendAllCardInfoList(new List<CardInfo> {
            new CardInfo { chineseName = "攻击牌", infoId = 6, originPower = 5, ability = CardAbility.Attack, badgeType = CardBadgeType.Wood, attackNum = 4 },
            new CardInfo { chineseName = "普通牌-4", infoId = 7, originPower = 4, ability = CardAbility.None, badgeType = CardBadgeType.Wood },
        });
        var model = new WholeAreaModel(true, "S", "E", CardGroup.KumikoFirstYear);
        var updated = model.SelfInit(new List<int> { 7 })
            .EnemyInit(new List<int> { 6 }, new List<int> { 63 }, CardGroup.KumikoThirdYear, false)
            .SelfDrawInitHandCard()
            .SelfReDrawInitHandCard()
            .EnemyDrawInitHandCard(new List<int> { 63 });
        updated = updated.ChooseCard(updated.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0], true);
        updated = updated.ChooseCard(updated.enemySinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0], false);
        updated = updated with {
            actionEventList = ImmutableList<ActionEvent>.Empty
        };
        updated = updated.ChooseCard(updated.selfSinglePlayerAreaModel.battleRowAreaList[(int)CardBadgeType.Wood].cardListModel.cardList[0], false);
        Assert.AreEqual(GameState.State.WAIT_SELF_ACTION, updated.gameState.curState);
        Assert.AreEqual(GameState.ActionState.None, updated.gameState.actionState);
        Assert.AreEqual(0, updated.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(5, updated.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(1, updated.selfSinglePlayerAreaModel.discardAreaModel.cardListModel.cardList.Count);
        Assert.AreEqual(2, updated.actionEventList.Count);
        Assert.AreEqual(ActionEvent.Type.Sfx, updated.actionEventList[0].type);
        Assert.AreEqual(AudioManager.SFXType.Scorch, updated.actionEventList[0].args[0]);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[1].type);
        Assert.AreEqual("施放<b>投掷</b>技能，移除卡牌：<b>普通牌-4</b>\n", updated.actionEventList[1].args[0]);
    }

    // 攻击技能中断
    [Test]
    public void ChooseCard_Attack_Interrupt_Self()
    {
        SetHostFirst(false);
        AppendAllCardInfoList(new List<CardInfo> {
            new CardInfo { chineseName = "攻击牌", infoId = 6, originPower = 5, ability = CardAbility.Attack, badgeType = CardBadgeType.Wood, attackNum = 2 },
        });
        var model = new WholeAreaModel(true, "S", "E", CardGroup.KumikoFirstYear);
        var updated = model.SelfInit(new List<int> { 6 })
            .EnemyInit(new List<int> { 1 }, new List<int> { 13 }, CardGroup.KumikoThirdYear, false)
            .SelfDrawInitHandCard()
            .SelfReDrawInitHandCard()
            .EnemyDrawInitHandCard(new List<int> { 13 });
        updated = updated.ChooseCard(updated.enemySinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0], false);
        updated = updated.ChooseCard(updated.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0], true);
        updated = updated with {
            actionEventList = ImmutableList<ActionEvent>.Empty
        };
        updated = updated.InterruptAction(true);
        Assert.AreEqual(GameState.State.WAIT_ENEMY_ACTION, updated.gameState.curState);
        Assert.AreEqual(GameState.ActionState.None, updated.gameState.actionState);
        Assert.AreEqual(5, updated.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(5, updated.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(2, updated.actionEventList.Count);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[0].type);
        Assert.AreEqual("<color=green>S</color> 未选择目标\n", updated.actionEventList[0].args[0]);
        Assert.AreEqual(ActionEvent.Type.BattleMsg, updated.actionEventList[1].type);
        Assert.AreEqual(BattleModel.ActionType.InterruptAction, updated.actionEventList[1].args[0]);
    }

    [Test]
    public void ChooseCard_Attack_Interrupt_Enemy()
    {
        SetHostFirst(true);
        AppendAllCardInfoList(new List<CardInfo> {
            new CardInfo { chineseName = "攻击牌", infoId = 6, originPower = 5, ability = CardAbility.Attack, badgeType = CardBadgeType.Wood, attackNum = 2 },
        });
        var model = new WholeAreaModel(true, "S", "E", CardGroup.KumikoFirstYear);
        var updated = model.SelfInit(new List<int> { 1 })
            .EnemyInit(new List<int> { 6 }, new List<int> { 63 }, CardGroup.KumikoThirdYear, false)
            .SelfDrawInitHandCard()
            .SelfReDrawInitHandCard()
            .EnemyDrawInitHandCard(new List<int> { 63 });
        updated = updated.ChooseCard(updated.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0], true);
        updated = updated.ChooseCard(updated.enemySinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0], false);
        updated = updated with {
            actionEventList = ImmutableList<ActionEvent>.Empty
        };
        updated = updated.InterruptAction(false);
        Assert.AreEqual(GameState.State.WAIT_SELF_ACTION, updated.gameState.curState);
        Assert.AreEqual(GameState.ActionState.None, updated.gameState.actionState);
        Assert.AreEqual(5, updated.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(5, updated.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(1, updated.actionEventList.Count);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[0].type);
        Assert.AreEqual("<color=red>E</color> 未选择目标\n", updated.actionEventList[0].args[0]);
    }

    [Test]
    public void ChooseCard_Tunning_Self()
    {
        SetHostFirst(true);
        AppendAllCardInfoList(new List<CardInfo> {
            new CardInfo { chineseName = "攻击牌", infoId = 6, originPower = 5, ability = CardAbility.Attack, badgeType = CardBadgeType.Wood, attackNum = 2 },
            new CardInfo { chineseName = "调音牌", infoId = 7, originPower = 5, ability = CardAbility.Tunning, badgeType = CardBadgeType.Wood },
        });
        var model = new WholeAreaModel(true, "S", "E", CardGroup.KumikoFirstYear);
        var updated = model.SelfInit(new List<int> { 1, 7 })
            .EnemyInit(new List<int> { 6 }, new List<int> { 63 }, CardGroup.KumikoThirdYear, false)
            .SelfDrawInitHandCard()
            .SelfReDrawInitHandCard()
            .EnemyDrawInitHandCard(new List<int> { 63 });
        int normalIndex = updated.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0].cardInfo.ability == CardAbility.Tunning ? 1 : 0;
        updated = updated.ChooseCard(updated.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[normalIndex], true);
        updated = updated.ChooseCard(updated.enemySinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0], false);
        updated = updated.ChooseCard(updated.selfSinglePlayerAreaModel.battleRowAreaList[(int)CardBadgeType.Wood].cardListModel.cardList[0], false);
        updated = updated with {
            actionEventList = ImmutableList<ActionEvent>.Empty
        };
        CardModel card = updated.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0];
        updated = updated.ChooseCard(card, true);
        Assert.AreEqual(GameState.State.WAIT_ENEMY_ACTION, updated.gameState.curState);
        Assert.AreEqual(10, updated.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(5, updated.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(3, updated.actionEventList.Count);
        Assert.AreEqual(ActionEvent.Type.BattleMsg, updated.actionEventList[0].type);
        Assert.AreEqual(BattleModel.ActionType.ChooseCard, updated.actionEventList[0].args[0]);
        Assert.AreEqual(card.cardInfo.id, updated.actionEventList[0].args[1]);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[1].type);
        Assert.AreEqual("<color=green>S</color> 打出卡牌：<b>调音牌</b>\n", updated.actionEventList[1].args[0]);
        Assert.AreEqual(ActionEvent.Type.Sfx, updated.actionEventList[2].type);
        Assert.AreEqual(AudioManager.SFXType.Tunning, updated.actionEventList[2].args[0]);
    }

    [Test]
    public void ChooseCard_Tunning_Enemy()
    {
        SetHostFirst(false);
        AppendAllCardInfoList(new List<CardInfo> {
            new CardInfo { chineseName = "攻击牌", infoId = 6, originPower = 5, ability = CardAbility.Attack, badgeType = CardBadgeType.Wood, attackNum = 2 },
            new CardInfo { chineseName = "调音牌", infoId = 7, originPower = 5, ability = CardAbility.Tunning, badgeType = CardBadgeType.Wood },
        });
        var model = new WholeAreaModel(true, "S", "E", CardGroup.KumikoFirstYear);
        var updated = model.SelfInit(new List<int> { 6 })
            .EnemyInit(new List<int> { 1, 7 }, new List<int> { 13, 73 }, CardGroup.KumikoThirdYear, false)
            .SelfDrawInitHandCard()
            .SelfReDrawInitHandCard()
            .EnemyDrawInitHandCard(new List<int> { 13, 73 });
        int normalIndex = updated.enemySinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0].cardInfo.ability == CardAbility.Tunning ? 1 : 0;
        updated = updated.ChooseCard(updated.enemySinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[normalIndex], false);
        updated = updated.ChooseCard(updated.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0], true);
        updated = updated.ChooseCard(updated.enemySinglePlayerAreaModel.battleRowAreaList[(int)CardBadgeType.Wood].cardListModel.cardList[0], true);
        updated = updated with {
            actionEventList = ImmutableList<ActionEvent>.Empty
        };
        updated = updated.ChooseCard(updated.enemySinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0], false);
        Assert.AreEqual(GameState.State.WAIT_SELF_ACTION, updated.gameState.curState);
        Assert.AreEqual(5, updated.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(10, updated.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(2, updated.actionEventList.Count);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[0].type);
        Assert.AreEqual("<color=red>E</color> 打出卡牌：<b>调音牌</b>\n", updated.actionEventList[0].args[0]);
        Assert.AreEqual(ActionEvent.Type.Sfx, updated.actionEventList[1].type);
        Assert.AreEqual(AudioManager.SFXType.Tunning, updated.actionEventList[1].args[0]);
    }

    // 伞击 无目标
    [Test]
    public void ChooseCard_ScorchWood_NoTarget_Self()
    {
        SetHostFirst(false);
        AppendAllCardInfoList(new List<CardInfo> {
            new CardInfo { chineseName = "伞击牌", infoId = 6, originPower = 5, ability = CardAbility.ScorchWood, badgeType = CardBadgeType.Wood },
        });
        var model = new WholeAreaModel(true, "S", "E", CardGroup.KumikoFirstYear);
        var updated = model.SelfInit(new List<int> { 6 })
            .EnemyInit(new List<int> { 1 }, new List<int> { 13 }, CardGroup.KumikoThirdYear, false)
            .SelfDrawInitHandCard()
            .SelfReDrawInitHandCard()
            .EnemyDrawInitHandCard(new List<int> { 13 });
        updated = updated.ChooseCard(updated.enemySinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0], false);
        updated = updated with {
            actionEventList = ImmutableList<ActionEvent>.Empty
        };
        CardModel card = updated.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0];
        updated = updated.ChooseCard(card, true);
        Assert.AreEqual(GameState.State.WAIT_ENEMY_ACTION, updated.gameState.curState);
        Assert.AreEqual(5, updated.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(5, updated.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(3, updated.actionEventList.Count);
        Assert.AreEqual(ActionEvent.Type.BattleMsg, updated.actionEventList[0].type);
        Assert.AreEqual(BattleModel.ActionType.ChooseCard, updated.actionEventList[0].args[0]);
        Assert.AreEqual(card.cardInfo.id, updated.actionEventList[0].args[1]);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[1].type);
        Assert.AreEqual("<color=green>S</color> 打出卡牌：<b>伞击牌</b>\n", updated.actionEventList[1].args[0]);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[2].type);
        Assert.AreEqual("施放<b>伞击</b>技能，无退部目标\n", updated.actionEventList[2].args[0]);
    }

    [Test]
    public void ChooseCard_ScorchWood_NoTarget_Enemy()
    {
        SetHostFirst(true);
        AppendAllCardInfoList(new List<CardInfo> {
            new CardInfo { chineseName = "伞击牌", infoId = 6, originPower = 5, ability = CardAbility.ScorchWood, badgeType = CardBadgeType.Wood },
        });
        var model = new WholeAreaModel(true, "S", "E", CardGroup.KumikoFirstYear);
        var updated = model.SelfInit(new List<int> { 1 })
            .EnemyInit(new List<int> { 6 }, new List<int> { 63 }, CardGroup.KumikoThirdYear, false)
            .SelfDrawInitHandCard()
            .SelfReDrawInitHandCard()
            .EnemyDrawInitHandCard(new List<int> { 63 });
        updated = updated.ChooseCard(updated.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0], true);
        updated = updated with {
            actionEventList = ImmutableList<ActionEvent>.Empty
        };
        updated = updated.ChooseCard(updated.enemySinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0], false);
        Assert.AreEqual(GameState.State.WAIT_SELF_ACTION, updated.gameState.curState);
        Assert.AreEqual(5, updated.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(5, updated.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(2, updated.actionEventList.Count);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[0].type);
        Assert.AreEqual("<color=red>E</color> 打出卡牌：<b>伞击牌</b>\n", updated.actionEventList[0].args[0]);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[1].type);
        Assert.AreEqual("施放<b>伞击</b>技能，无退部目标\n", updated.actionEventList[1].args[0]);
    }

    // 伞击 退部
    [Test]
    public void ChooseCard_ScorchWood_Normal_Self()
    {
        SetHostFirst(false);
        AppendAllCardInfoList(new List<CardInfo> {
            new CardInfo { chineseName = "伞击牌", infoId = 6, originPower = 5, ability = CardAbility.ScorchWood, badgeType = CardBadgeType.Wood },
            new CardInfo { chineseName = "普通牌-11", infoId = 7, originPower = 11, ability = CardAbility.None, badgeType = CardBadgeType.Wood },
        });
        var model = new WholeAreaModel(true, "S", "E", CardGroup.KumikoFirstYear);
        var updated = model.SelfInit(new List<int> { 6 })
            .EnemyInit(new List<int> { 7 }, new List<int> { 73 }, CardGroup.KumikoThirdYear, false)
            .SelfDrawInitHandCard()
            .SelfReDrawInitHandCard()
            .EnemyDrawInitHandCard(new List<int> { 73 });
        updated = updated.ChooseCard(updated.enemySinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0], false);
        updated = updated with {
            actionEventList = ImmutableList<ActionEvent>.Empty
        };
        CardModel card = updated.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0];
        updated = updated.ChooseCard(card, true);
        Assert.AreEqual(GameState.State.WAIT_ENEMY_ACTION, updated.gameState.curState);
        Assert.AreEqual(5, updated.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(0, updated.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(1, updated.enemySinglePlayerAreaModel.discardAreaModel.cardListModel.cardList.Count);
        Assert.AreEqual(4, updated.actionEventList.Count);
        Assert.AreEqual(ActionEvent.Type.BattleMsg, updated.actionEventList[0].type);
        Assert.AreEqual(BattleModel.ActionType.ChooseCard, updated.actionEventList[0].args[0]);
        Assert.AreEqual(card.cardInfo.id, updated.actionEventList[0].args[1]);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[1].type);
        Assert.AreEqual("<color=green>S</color> 打出卡牌：<b>伞击牌</b>\n", updated.actionEventList[1].args[0]);
        Assert.AreEqual(ActionEvent.Type.Sfx, updated.actionEventList[2].type);
        Assert.AreEqual(AudioManager.SFXType.Scorch, updated.actionEventList[2].args[0]);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[3].type);
        Assert.AreEqual("施放<b>伞击</b>技能，移除卡牌：<b>普通牌-11</b>\n", updated.actionEventList[3].args[0]);
    }

    [Test]
    public void ChooseCard_ScorchWood_Normal_Enemy()
    {
        SetHostFirst(true);
        AppendAllCardInfoList(new List<CardInfo> {
            new CardInfo { chineseName = "伞击牌", infoId = 6, originPower = 5, ability = CardAbility.ScorchWood, badgeType = CardBadgeType.Wood },
            new CardInfo { chineseName = "普通牌-11", infoId = 7, originPower = 11, ability = CardAbility.None, badgeType = CardBadgeType.Wood },
        });
        var model = new WholeAreaModel(true, "S", "E", CardGroup.KumikoFirstYear);
        var updated = model.SelfInit(new List<int> { 7 })
            .EnemyInit(new List<int> { 6 }, new List<int> { 63 }, CardGroup.KumikoThirdYear, false)
            .SelfDrawInitHandCard()
            .SelfReDrawInitHandCard()
            .EnemyDrawInitHandCard(new List<int> { 63 });
        updated = updated.ChooseCard(updated.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0], true);
        updated = updated with {
            actionEventList = ImmutableList<ActionEvent>.Empty
        };
        updated = updated.ChooseCard(updated.enemySinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0], false);
        Assert.AreEqual(GameState.State.WAIT_SELF_ACTION, updated.gameState.curState);
        Assert.AreEqual(0, updated.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(5, updated.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(1, updated.selfSinglePlayerAreaModel.discardAreaModel.cardListModel.cardList.Count);
        Assert.AreEqual(3, updated.actionEventList.Count);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[0].type);
        Assert.AreEqual("<color=red>E</color> 打出卡牌：<b>伞击牌</b>\n", updated.actionEventList[0].args[0]);
        Assert.AreEqual(ActionEvent.Type.Sfx, updated.actionEventList[1].type);
        Assert.AreEqual(AudioManager.SFXType.Scorch, updated.actionEventList[1].args[0]);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[2].type);
        Assert.AreEqual("施放<b>伞击</b>技能，移除卡牌：<b>普通牌-11</b>\n", updated.actionEventList[2].args[0]);
    }

    // 复活 无目标
    [Test]
    public void ChooseCard_Medic_NoTarget_Self()
    {
        SetHostFirst(true);
        AppendAllCardInfoList(new List<CardInfo> {
            new CardInfo { chineseName = "复活牌", infoId = 6, originPower = 5, ability = CardAbility.Medic, badgeType = CardBadgeType.Wood },
        });
        var model = new WholeAreaModel(true, "S", "E", CardGroup.KumikoFirstYear);
        var updated = model.SelfInit(new List<int> { 6 })
            .EnemyInit(new List<int> { 1 }, new List<int> { 13 }, CardGroup.KumikoThirdYear, false)
            .SelfDrawInitHandCard()
            .SelfReDrawInitHandCard()
            .EnemyDrawInitHandCard(new List<int> { 13 });
        updated = updated with {
            actionEventList = ImmutableList<ActionEvent>.Empty
        };
        CardModel card = updated.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0];
        updated = updated.ChooseCard(card, true);
        Assert.AreEqual(GameState.State.WAIT_ENEMY_ACTION, updated.gameState.curState);
        Assert.AreEqual(GameState.ActionState.None, updated.gameState.actionState);
        Assert.AreEqual(5, updated.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(0, updated.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(3, updated.actionEventList.Count);
        Assert.AreEqual(ActionEvent.Type.BattleMsg, updated.actionEventList[0].type);
        Assert.AreEqual(BattleModel.ActionType.ChooseCard, updated.actionEventList[0].args[0]);
        Assert.AreEqual(card.cardInfo.id, updated.actionEventList[0].args[1]);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[1].type);
        Assert.AreEqual("<color=green>S</color> 打出卡牌：<b>复活牌</b>\n", updated.actionEventList[1].args[0]);
        Assert.AreEqual(ActionEvent.Type.Toast, updated.actionEventList[2].type);
        Assert.AreEqual("无可复活目标", updated.actionEventList[2].args[0]);
    }

    [Test]
    public void ChooseCard_Medic_NoTarget_Enemy()
    {
        SetHostFirst(false);
        AppendAllCardInfoList(new List<CardInfo> {
            new CardInfo { chineseName = "复活牌", infoId = 6, originPower = 5, ability = CardAbility.Medic, badgeType = CardBadgeType.Wood },
        });
        var model = new WholeAreaModel(true, "S", "E", CardGroup.KumikoFirstYear);
        var updated = model.SelfInit(new List<int> { 1 })
            .EnemyInit(new List<int> { 6 }, new List<int> { 63 }, CardGroup.KumikoThirdYear, false)
            .SelfDrawInitHandCard()
            .SelfReDrawInitHandCard()
            .EnemyDrawInitHandCard(new List<int> { 63 });
        updated = updated with {
            actionEventList = ImmutableList<ActionEvent>.Empty
        };
        updated = updated.ChooseCard(updated.enemySinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0], false);
        Assert.AreEqual(GameState.State.WAIT_SELF_ACTION, updated.gameState.curState);
        Assert.AreEqual(0, updated.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(5, updated.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(1, updated.actionEventList.Count);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[0].type);
        Assert.AreEqual("<color=red>E</color> 打出卡牌：<b>复活牌</b>\n", updated.actionEventList[0].args[0]);
    }

    // 复活 普通牌
    [Test]
    public void ChooseCard_Medic_Normal_Self()
    {
        SetHostFirst(true);
        AppendAllCardInfoList(new List<CardInfo> {
            new CardInfo { chineseName = "复活牌", infoId = 6, originPower = 5, ability = CardAbility.Medic, badgeType = CardBadgeType.Wood },
            new CardInfo { chineseName = "普通牌-4", infoId = 7, originPower = 4, badgeType = CardBadgeType.Wood },
            new CardInfo { chineseName = "攻击牌", infoId = 8, originPower = 5, ability = CardAbility.Attack, badgeType = CardBadgeType.Wood, attackNum = 4 },
        });
        var model = new WholeAreaModel(true, "S", "E", CardGroup.KumikoFirstYear);
        var updated = model.SelfInit(new List<int> { 6, 7 })
            .EnemyInit(new List<int> { 8 }, new List<int> { 83 }, CardGroup.KumikoThirdYear, false)
            .SelfDrawInitHandCard()
            .SelfReDrawInitHandCard()
            .EnemyDrawInitHandCard(new List<int> { 83 });
        int normalIndex = updated.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0].cardInfo.infoId == 7 ? 0 : 1;
        updated = updated.ChooseCard(updated.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[normalIndex], true);
        updated = updated.ChooseCard(updated.enemySinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0], false);
        updated = updated.ChooseCard(updated.selfSinglePlayerAreaModel.battleRowAreaList[(int)CardBadgeType.Wood].cardListModel.cardList[0], false);
        updated = updated with {
            actionEventList = ImmutableList<ActionEvent>.Empty
        };
        var card = updated.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0];
        updated = updated.ChooseCard(card, true);
        Assert.AreEqual(GameState.State.WAIT_SELF_ACTION, updated.gameState.curState);
        Assert.AreEqual(GameState.ActionState.MEDICING, updated.gameState.actionState);
        Assert.AreEqual(5, updated.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(5, updated.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(3, updated.actionEventList.Count);
        Assert.AreEqual(ActionEvent.Type.BattleMsg, updated.actionEventList[0].type);
        Assert.AreEqual(BattleModel.ActionType.ChooseCard, updated.actionEventList[0].args[0]);
        Assert.AreEqual(card.cardInfo.id, updated.actionEventList[0].args[1]);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[1].type);
        Assert.AreEqual("<color=green>S</color> 打出卡牌：<b>复活牌</b>\n", updated.actionEventList[1].args[0]);
        Assert.AreEqual(ActionEvent.Type.Toast, updated.actionEventList[2].type);
        Assert.AreEqual("请选择复活目标", updated.actionEventList[2].args[0]);
        updated = updated with {
            actionEventList = ImmutableList<ActionEvent>.Empty
        };
        card = updated.selfSinglePlayerAreaModel.discardAreaModel.cardListModel.cardList[0];
        updated = updated.ChooseCard(card, true);
        Assert.AreEqual(GameState.State.WAIT_ENEMY_ACTION, updated.gameState.curState);
        Assert.AreEqual(GameState.ActionState.None, updated.gameState.actionState);
        Assert.AreEqual(9, updated.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(5, updated.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(2, updated.actionEventList.Count);
        Assert.AreEqual(ActionEvent.Type.BattleMsg, updated.actionEventList[0].type);
        Assert.AreEqual(BattleModel.ActionType.ChooseCard, updated.actionEventList[0].args[0]);
        Assert.AreEqual(card.cardInfo.id, updated.actionEventList[0].args[1]);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[1].type);
        Assert.AreEqual("<color=green>S</color> 打出卡牌：<b>普通牌-4</b>\n", updated.actionEventList[1].args[0]);
    }

    [Test]
    public void ChooseCard_Medic_Normal_Enemy()
    {
        SetHostFirst(false);
        AppendAllCardInfoList(new List<CardInfo> {
            new CardInfo { chineseName = "复活牌", infoId = 6, originPower = 5, ability = CardAbility.Medic, badgeType = CardBadgeType.Wood },
            new CardInfo { chineseName = "普通牌-4", infoId = 7, originPower = 4, badgeType = CardBadgeType.Wood },
            new CardInfo { chineseName = "攻击牌", infoId = 8, originPower = 5, ability = CardAbility.Attack, badgeType = CardBadgeType.Wood, attackNum = 4 },
        });
        var model = new WholeAreaModel(true, "S", "E", CardGroup.KumikoFirstYear);
        var updated = model.SelfInit(new List<int> { 8 })
            .EnemyInit(new List<int> { 6, 7 }, new List<int> { 63, 73 }, CardGroup.KumikoThirdYear, false)
            .SelfDrawInitHandCard()
            .SelfReDrawInitHandCard()
            .EnemyDrawInitHandCard(new List<int> { 63, 73 });
        int normalIndex = updated.enemySinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0].cardInfo.infoId == 7 ? 0 : 1;
        updated = updated.ChooseCard(updated.enemySinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[normalIndex], false);
        updated = updated.ChooseCard(updated.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0], true);
        updated = updated.ChooseCard(updated.enemySinglePlayerAreaModel.battleRowAreaList[(int)CardBadgeType.Wood].cardListModel.cardList[0], true);
        updated = updated with {
            actionEventList = ImmutableList<ActionEvent>.Empty
        };
        var card = updated.enemySinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0];
        updated = updated.ChooseCard(card, false);
        Assert.AreEqual(GameState.State.WAIT_ENEMY_ACTION, updated.gameState.curState);
        Assert.AreEqual(GameState.ActionState.MEDICING, updated.gameState.actionState);
        Assert.AreEqual(5, updated.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(5, updated.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(1, updated.actionEventList.Count);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[0].type);
        Assert.AreEqual("<color=red>E</color> 打出卡牌：<b>复活牌</b>\n", updated.actionEventList[0].args[0]);
        updated = updated with {
            actionEventList = ImmutableList<ActionEvent>.Empty
        };
        card = updated.enemySinglePlayerAreaModel.discardAreaModel.cardListModel.cardList[0];
        updated = updated.ChooseCard(card, false);
        Assert.AreEqual(GameState.State.WAIT_SELF_ACTION, updated.gameState.curState);
        Assert.AreEqual(GameState.ActionState.None, updated.gameState.actionState);
        Assert.AreEqual(5, updated.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(9, updated.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(1, updated.actionEventList.Count);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[0].type);
        Assert.AreEqual("<color=red>E</color> 打出卡牌：<b>普通牌-4</b>\n", updated.actionEventList[0].args[0]);
    }

    // 复活 复活牌
    [Test]
    public void ChooseCard_Medic_Medic_Self()
    {
        SetHostFirst(true);
        AppendAllCardInfoList(new List<CardInfo> {
            new CardInfo { chineseName = "复活牌", infoId = 6, originPower = 6, ability = CardAbility.Medic, badgeType = CardBadgeType.Wood },
            new CardInfo { chineseName = "伞击牌", infoId = 7, originPower = 5, ability = CardAbility.ScorchWood, badgeType = CardBadgeType.Wood },
        });
        var model = new WholeAreaModel(true, "S", "E", CardGroup.KumikoFirstYear);
        var updated = model.SelfInit(new List<int> { 6, 6, 6 })
            .EnemyInit(new List<int> { 7, 7 }, new List<int> { 73, 73 }, CardGroup.KumikoThirdYear, false)
            .SelfDrawInitHandCard()
            .SelfReDrawInitHandCard()
            .EnemyDrawInitHandCard(new List<int> { 73, 73 });
        updated = updated.ChooseCard(updated.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0], true);
        updated = updated.ChooseCard(updated.enemySinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0], false);
        updated = updated.ChooseCard(updated.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0], true);
        updated = updated.ChooseCard(updated.enemySinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0], false); // 此时self弃牌区有两张复活牌
        updated = updated.ChooseCard(updated.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0], true);
        updated = updated with {
            actionEventList = ImmutableList<ActionEvent>.Empty
        };
        var card = updated.selfSinglePlayerAreaModel.discardAreaModel.cardListModel.cardList[0];
        updated = updated.ChooseCard(card, true);
        Assert.AreEqual(GameState.State.WAIT_SELF_ACTION, updated.gameState.curState);
        Assert.AreEqual(GameState.ActionState.MEDICING, updated.gameState.actionState);
        Assert.AreEqual(12, updated.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(10, updated.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(3, updated.actionEventList.Count);
        Assert.AreEqual(ActionEvent.Type.BattleMsg, updated.actionEventList[0].type);
        Assert.AreEqual(BattleModel.ActionType.ChooseCard, updated.actionEventList[0].args[0]);
        Assert.AreEqual(card.cardInfo.id, updated.actionEventList[0].args[1]);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[1].type);
        Assert.AreEqual("<color=green>S</color> 打出卡牌：<b>复活牌</b>\n", updated.actionEventList[1].args[0]);
        Assert.AreEqual(ActionEvent.Type.Toast, updated.actionEventList[2].type);
        Assert.AreEqual("请选择复活目标", updated.actionEventList[2].args[0]);
        updated = updated with {
            actionEventList = ImmutableList<ActionEvent>.Empty
        };
        card = updated.selfSinglePlayerAreaModel.discardAreaModel.cardListModel.cardList[0];
        updated = updated.ChooseCard(card, true);
        Assert.AreEqual(GameState.State.WAIT_ENEMY_ACTION, updated.gameState.curState);
        Assert.AreEqual(GameState.ActionState.None, updated.gameState.actionState);
        Assert.AreEqual(18, updated.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(10, updated.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(3, updated.actionEventList.Count);
        Assert.AreEqual(ActionEvent.Type.BattleMsg, updated.actionEventList[0].type);
        Assert.AreEqual(BattleModel.ActionType.ChooseCard, updated.actionEventList[0].args[0]);
        Assert.AreEqual(card.cardInfo.id, updated.actionEventList[0].args[1]);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[1].type);
        Assert.AreEqual("<color=green>S</color> 打出卡牌：<b>复活牌</b>\n", updated.actionEventList[1].args[0]);
        Assert.AreEqual(ActionEvent.Type.Toast, updated.actionEventList[2].type);
        Assert.AreEqual("无可复活目标", updated.actionEventList[2].args[0]);
    }

    [Test]
    public void ChooseCard_Medic_Medic_Enemy()
    {
        SetHostFirst(false);
        AppendAllCardInfoList(new List<CardInfo> {
            new CardInfo { chineseName = "复活牌", infoId = 6, originPower = 6, ability = CardAbility.Medic, badgeType = CardBadgeType.Wood },
            new CardInfo { chineseName = "伞击牌", infoId = 7, originPower = 5, ability = CardAbility.ScorchWood, badgeType = CardBadgeType.Wood },
        });
        var model = new WholeAreaModel(true, "S", "E", CardGroup.KumikoFirstYear);
        var updated = model.SelfInit(new List<int> { 7, 7 })
            .EnemyInit(new List<int> { 6, 6, 6 }, new List<int> { 63, 63, 63 }, CardGroup.KumikoThirdYear, false)
            .SelfDrawInitHandCard()
            .SelfReDrawInitHandCard()
            .EnemyDrawInitHandCard(new List<int> { 63, 63, 63 });
        updated = updated.ChooseCard(updated.enemySinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0], false);
        updated = updated.ChooseCard(updated.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0], true);
        updated = updated.ChooseCard(updated.enemySinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0], false);
        updated = updated.ChooseCard(updated.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0], true); // 此时enemy弃牌区有两张复活牌
        updated = updated.ChooseCard(updated.enemySinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0], false);
        updated = updated with {
            actionEventList = ImmutableList<ActionEvent>.Empty
        };
        var card = updated.enemySinglePlayerAreaModel.discardAreaModel.cardListModel.cardList[0];
        updated = updated.ChooseCard(card, false);
        Assert.AreEqual(GameState.State.WAIT_ENEMY_ACTION, updated.gameState.curState);
        Assert.AreEqual(GameState.ActionState.MEDICING, updated.gameState.actionState);
        Assert.AreEqual(10, updated.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(12, updated.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(1, updated.actionEventList.Count);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[0].type);
        Assert.AreEqual("<color=red>E</color> 打出卡牌：<b>复活牌</b>\n", updated.actionEventList[0].args[0]);
        updated = updated with {
            actionEventList = ImmutableList<ActionEvent>.Empty
        };
        card = updated.enemySinglePlayerAreaModel.discardAreaModel.cardListModel.cardList[0];
        updated = updated.ChooseCard(card, false);
        Assert.AreEqual(GameState.State.WAIT_SELF_ACTION, updated.gameState.curState);
        Assert.AreEqual(GameState.ActionState.None, updated.gameState.actionState);
        Assert.AreEqual(10, updated.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(18, updated.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(1, updated.actionEventList.Count);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[0].type);
        Assert.AreEqual("<color=red>E</color> 打出卡牌：<b>复活牌</b>\n", updated.actionEventList[0].args[0]);
    }

    // 复活 中断
    [Test]
    public void ChooseCard_Medic_Interrupt_Self()
    {
        SetHostFirst(true);
        AppendAllCardInfoList(new List<CardInfo> {
            new CardInfo { chineseName = "复活牌", infoId = 6, originPower = 5, ability = CardAbility.Medic, badgeType = CardBadgeType.Wood },
            new CardInfo { chineseName = "普通牌-4", infoId = 7, originPower = 4, badgeType = CardBadgeType.Wood },
            new CardInfo { chineseName = "攻击牌", infoId = 8, originPower = 5, ability = CardAbility.Attack, badgeType = CardBadgeType.Wood, attackNum = 4 },
        });
        var model = new WholeAreaModel(true, "S", "E", CardGroup.KumikoFirstYear);
        var updated = model.SelfInit(new List<int> { 6, 7 })
            .EnemyInit(new List<int> { 8 }, new List<int> { 83 }, CardGroup.KumikoThirdYear, false)
            .SelfDrawInitHandCard()
            .SelfReDrawInitHandCard()
            .EnemyDrawInitHandCard(new List<int> { 83 });
        int normalIndex = updated.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0].cardInfo.infoId == 7 ? 0 : 1;
        updated = updated.ChooseCard(updated.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[normalIndex], true);
        updated = updated.ChooseCard(updated.enemySinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0], false);
        updated = updated.ChooseCard(updated.selfSinglePlayerAreaModel.battleRowAreaList[(int)CardBadgeType.Wood].cardListModel.cardList[0], false);
        updated = updated.ChooseCard(updated.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0], true);
        updated = updated with {
            actionEventList = ImmutableList<ActionEvent>.Empty
        };
        updated = updated.InterruptAction(true);
        Assert.AreEqual(GameState.State.WAIT_ENEMY_ACTION, updated.gameState.curState);
        Assert.AreEqual(GameState.ActionState.None, updated.gameState.actionState);
        Assert.AreEqual(5, updated.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(5, updated.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(2, updated.actionEventList.Count);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[0].type);
        Assert.AreEqual("<color=green>S</color> 未选择目标\n", updated.actionEventList[0].args[0]);
        Assert.AreEqual(ActionEvent.Type.BattleMsg, updated.actionEventList[1].type);
        Assert.AreEqual(BattleModel.ActionType.InterruptAction, updated.actionEventList[1].args[0]);
    }

    [Test]
    public void ChooseCard_Medic_Interrupt_Enemy()
    {
        SetHostFirst(false);
        AppendAllCardInfoList(new List<CardInfo> {
            new CardInfo { chineseName = "复活牌", infoId = 6, originPower = 5, ability = CardAbility.Medic, badgeType = CardBadgeType.Wood },
            new CardInfo { chineseName = "普通牌-4", infoId = 7, originPower = 4, badgeType = CardBadgeType.Wood },
            new CardInfo { chineseName = "攻击牌", infoId = 8, originPower = 5, ability = CardAbility.Attack, badgeType = CardBadgeType.Wood, attackNum = 4 },
        });
        var model = new WholeAreaModel(true, "S", "E", CardGroup.KumikoFirstYear);
        var updated = model.SelfInit(new List<int> { 8 })
            .EnemyInit(new List<int> { 6, 7 }, new List<int> { 63, 73 }, CardGroup.KumikoThirdYear, false)
            .SelfDrawInitHandCard()
            .SelfReDrawInitHandCard()
            .EnemyDrawInitHandCard(new List<int> { 63, 73 });
        int normalIndex = updated.enemySinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0].cardInfo.infoId == 7 ? 0 : 1;
        updated = updated.ChooseCard(updated.enemySinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[normalIndex], false);
        updated = updated.ChooseCard(updated.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0], true);
        updated = updated.ChooseCard(updated.enemySinglePlayerAreaModel.battleRowAreaList[(int)CardBadgeType.Wood].cardListModel.cardList[0], true);
        updated = updated.ChooseCard(updated.enemySinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0], false);
        updated = updated with {
            actionEventList = ImmutableList<ActionEvent>.Empty
        };
        updated = updated.InterruptAction(false);
        Assert.AreEqual(GameState.State.WAIT_SELF_ACTION, updated.gameState.curState);
        Assert.AreEqual(GameState.ActionState.None, updated.gameState.actionState);
        Assert.AreEqual(5, updated.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(5, updated.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(1, updated.actionEventList.Count);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[0].type);
        Assert.AreEqual("<color=red>E</color> 未选择目标\n", updated.actionEventList[0].args[0]);
    }

    // 复活 指挥牌
    [Test]
    public void ChooseCard_Medic_Leader_Self()
    {
        SetHostFirst(true);
        AppendAllCardInfoList(new List<CardInfo> {
            new CardInfo { chineseName = "复活指挥牌", infoId = 6, originPower = 5, ability = CardAbility.Medic, cardType = CardType.Leader },
            new CardInfo { chineseName = "普通牌-4", infoId = 7, originPower = 4, badgeType = CardBadgeType.Wood },
            new CardInfo { chineseName = "攻击牌", infoId = 8, originPower = 5, ability = CardAbility.Attack, badgeType = CardBadgeType.Wood, attackNum = 4 },
        });
        var model = new WholeAreaModel(true, "S", "E", CardGroup.KumikoFirstYear);
        var updated = model.SelfInit(new List<int> { 6, 7 })
            .EnemyInit(new List<int> { 8 }, new List<int> { 83 }, CardGroup.KumikoThirdYear, false)
            .SelfDrawInitHandCard()
            .SelfReDrawInitHandCard()
            .EnemyDrawInitHandCard(new List<int> { 83 });
        updated = updated.ChooseCard(updated.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0], true);
        updated = updated.ChooseCard(updated.enemySinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0], false);
        updated = updated.ChooseCard(updated.selfSinglePlayerAreaModel.battleRowAreaList[(int)CardBadgeType.Wood].cardListModel.cardList[0], false);
        updated = updated with {
            actionEventList = ImmutableList<ActionEvent>.Empty
        };
        var card = updated.selfSinglePlayerAreaModel.handCardAreaModel.leaderCardListModel.cardList[0];
        updated = updated.ChooseCard(card, true);
        Assert.AreEqual(GameState.State.WAIT_SELF_ACTION, updated.gameState.curState);
        Assert.AreEqual(GameState.ActionState.MEDICING, updated.gameState.actionState);
        Assert.AreEqual(0, updated.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(5, updated.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(0, updated.selfSinglePlayerAreaModel.battleRowAreaList[(int)CardBadgeType.Wood].cardListModel.cardList.Count);
        Assert.AreEqual(3, updated.actionEventList.Count);
        Assert.AreEqual(ActionEvent.Type.BattleMsg, updated.actionEventList[0].type);
        Assert.AreEqual(BattleModel.ActionType.ChooseCard, updated.actionEventList[0].args[0]);
        Assert.AreEqual(card.cardInfo.id, updated.actionEventList[0].args[1]);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[1].type);
        Assert.AreEqual("<color=green>S</color> 打出卡牌：<b>复活指挥牌</b>\n", updated.actionEventList[1].args[0]);
        Assert.AreEqual(ActionEvent.Type.Toast, updated.actionEventList[2].type);
        Assert.AreEqual("请选择复活目标", updated.actionEventList[2].args[0]);
        updated = updated with {
            actionEventList = ImmutableList<ActionEvent>.Empty
        };
        card = updated.selfSinglePlayerAreaModel.discardAreaModel.cardListModel.cardList[0];
        updated = updated.ChooseCard(card, true);
        Assert.AreEqual(GameState.State.WAIT_ENEMY_ACTION, updated.gameState.curState);
        Assert.AreEqual(GameState.ActionState.None, updated.gameState.actionState);
        Assert.AreEqual(4, updated.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(5, updated.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(2, updated.actionEventList.Count);
        Assert.AreEqual(ActionEvent.Type.BattleMsg, updated.actionEventList[0].type);
        Assert.AreEqual(BattleModel.ActionType.ChooseCard, updated.actionEventList[0].args[0]);
        Assert.AreEqual(card.cardInfo.id, updated.actionEventList[0].args[1]);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[1].type);
        Assert.AreEqual("<color=green>S</color> 打出卡牌：<b>普通牌-4</b>\n", updated.actionEventList[1].args[0]);
    }

    [Test]
    public void ChooseCard_Medic_Leader_Enemy()
    {
        SetHostFirst(false);
        AppendAllCardInfoList(new List<CardInfo> {
            new CardInfo { chineseName = "复活指挥牌", infoId = 6, originPower = 5, ability = CardAbility.Medic, cardType = CardType.Leader },
            new CardInfo { chineseName = "普通牌-4", infoId = 7, originPower = 4, badgeType = CardBadgeType.Wood },
            new CardInfo { chineseName = "攻击牌", infoId = 8, originPower = 5, ability = CardAbility.Attack, badgeType = CardBadgeType.Wood, attackNum = 4 },
        });
        var model = new WholeAreaModel(true, "S", "E", CardGroup.KumikoFirstYear);
        var updated = model.SelfInit(new List<int> { 8 })
            .EnemyInit(new List<int> { 6, 7 }, new List<int> { 63, 73 }, CardGroup.KumikoThirdYear, false)
            .SelfDrawInitHandCard()
            .SelfReDrawInitHandCard()
            .EnemyDrawInitHandCard(new List<int> { 73 });
        updated = updated.ChooseCard(updated.enemySinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0], false);
        updated = updated.ChooseCard(updated.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0], true);
        updated = updated.ChooseCard(updated.enemySinglePlayerAreaModel.battleRowAreaList[(int)CardBadgeType.Wood].cardListModel.cardList[0], true);
        updated = updated with {
            actionEventList = ImmutableList<ActionEvent>.Empty
        };
        var card = updated.enemySinglePlayerAreaModel.handCardAreaModel.leaderCardListModel.cardList[0];
        updated = updated.ChooseCard(card, false);
        Assert.AreEqual(GameState.State.WAIT_ENEMY_ACTION, updated.gameState.curState);
        Assert.AreEqual(GameState.ActionState.MEDICING, updated.gameState.actionState);
        Assert.AreEqual(5, updated.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(0, updated.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(0, updated.enemySinglePlayerAreaModel.battleRowAreaList[(int)CardBadgeType.Wood].cardListModel.cardList.Count);
        Assert.AreEqual(1, updated.actionEventList.Count);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[0].type);
        Assert.AreEqual("<color=red>E</color> 打出卡牌：<b>复活指挥牌</b>\n", updated.actionEventList[0].args[0]);
        updated = updated with {
            actionEventList = ImmutableList<ActionEvent>.Empty
        };
        card = updated.enemySinglePlayerAreaModel.discardAreaModel.cardListModel.cardList[0];
        updated = updated.ChooseCard(card, false);
        Assert.AreEqual(GameState.State.WAIT_SELF_ACTION, updated.gameState.curState);
        Assert.AreEqual(GameState.ActionState.None, updated.gameState.actionState);
        Assert.AreEqual(5, updated.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(4, updated.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(1, updated.actionEventList.Count);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[0].type);
        Assert.AreEqual("<color=red>E</color> 打出卡牌：<b>普通牌-4</b>\n", updated.actionEventList[0].args[0]);
    }

    // 大号君 无目标
    [Test]
    public void ChooseCard_Decoy_NoTarget_Self()
    {
        SetHostFirst(true);
        AppendAllCardInfoList(new List<CardInfo> {
            new CardInfo { chineseName = "大号君", infoId = 6, originPower = 0, ability = CardAbility.Decoy },
        });
        var model = new WholeAreaModel(true, "S", "E", CardGroup.KumikoFirstYear);
        var updated = model.SelfInit(new List<int> { 6 })
            .EnemyInit(new List<int> { 1 }, new List<int> { 13 }, CardGroup.KumikoThirdYear, false)
            .SelfDrawInitHandCard()
            .SelfReDrawInitHandCard()
            .EnemyDrawInitHandCard(new List<int> { 13 });
        updated = updated with {
            actionEventList = ImmutableList<ActionEvent>.Empty
        };
        CardModel card = updated.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0];
        updated = updated.ChooseCard(card, true);
        Assert.AreEqual(GameState.State.WAIT_ENEMY_ACTION, updated.gameState.curState);
        Assert.AreEqual(GameState.ActionState.None, updated.gameState.actionState);
        Assert.AreEqual(0, updated.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(0, updated.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(3, updated.actionEventList.Count);
        Assert.AreEqual(ActionEvent.Type.BattleMsg, updated.actionEventList[0].type);
        Assert.AreEqual(BattleModel.ActionType.ChooseCard, updated.actionEventList[0].args[0]);
        Assert.AreEqual(card.cardInfo.id, updated.actionEventList[0].args[1]);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[1].type);
        Assert.AreEqual("<color=green>S</color> 打出卡牌：<b>大号君</b>\n", updated.actionEventList[1].args[0]);
        Assert.AreEqual(ActionEvent.Type.Toast, updated.actionEventList[2].type);
        Assert.AreEqual("无可使用大号君的目标", updated.actionEventList[2].args[0]);
    }

    [Test]
    public void ChooseCard_Decoy_NoTarget_Enemy()
    {
        SetHostFirst(false);
        AppendAllCardInfoList(new List<CardInfo> {
            new CardInfo { chineseName = "大号君", infoId = 6, originPower = 0, ability = CardAbility.Decoy },
        });
        var model = new WholeAreaModel(true, "S", "E", CardGroup.KumikoFirstYear);
        var updated = model.SelfInit(new List<int> { 1 })
            .EnemyInit(new List<int> { 6 }, new List<int> { 63 }, CardGroup.KumikoThirdYear, false)
            .SelfDrawInitHandCard()
            .SelfReDrawInitHandCard()
            .EnemyDrawInitHandCard(new List<int> { 63 });
        updated = updated with {
            actionEventList = ImmutableList<ActionEvent>.Empty
        };
        updated = updated.ChooseCard(updated.enemySinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0], false);
        Assert.AreEqual(GameState.State.WAIT_SELF_ACTION, updated.gameState.curState);
        Assert.AreEqual(0, updated.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(0, updated.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(1, updated.actionEventList.Count);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[0].type);
        Assert.AreEqual("<color=red>E</color> 打出卡牌：<b>大号君</b>\n", updated.actionEventList[0].args[0]);
    }

    // 大号君
    [Test]
    public void ChooseCard_Decoy_Normal_Self()
    {
        SetHostFirst(true);
        AppendAllCardInfoList(new List<CardInfo> {
            new CardInfo { chineseName = "大号君", infoId = 6, originPower = 0, ability = CardAbility.Decoy },
        });
        var model = new WholeAreaModel(true, "S", "E", CardGroup.KumikoFirstYear);
        var updated = model.SelfInit(new List<int> { 2, 6 }) // 使用非默认的brass牌
            .EnemyInit(new List<int> { 1 }, new List<int> { 13 }, CardGroup.KumikoThirdYear, false)
            .SelfDrawInitHandCard()
            .SelfReDrawInitHandCard()
            .EnemyDrawInitHandCard(new List<int> { 13 });
        var normalCard = updated.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList.Find(o => o.cardInfo.ability == CardAbility.None);
        updated = updated.ChooseCard(normalCard, true);
        updated = updated.ChooseCard(updated.enemySinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0], false);
        updated = updated with {
            actionEventList = ImmutableList<ActionEvent>.Empty
        };
        var card = updated.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0];
        updated = updated.ChooseCard(card, true);
        Assert.AreEqual(GameState.State.WAIT_SELF_ACTION, updated.gameState.curState);
        Assert.AreEqual(GameState.ActionState.DECOYING, updated.gameState.actionState);
        Assert.AreEqual(3, updated.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(5, updated.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(3, updated.actionEventList.Count);
        Assert.AreEqual(ActionEvent.Type.BattleMsg, updated.actionEventList[0].type);
        Assert.AreEqual(BattleModel.ActionType.ChooseCard, updated.actionEventList[0].args[0]);
        Assert.AreEqual(card.cardInfo.id, updated.actionEventList[0].args[1]);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[1].type);
        Assert.AreEqual("<color=green>S</color> 打出卡牌：<b>大号君</b>\n", updated.actionEventList[1].args[0]);
        Assert.AreEqual(ActionEvent.Type.Toast, updated.actionEventList[2].type);
        Assert.AreEqual("请选择使用大号君的目标", updated.actionEventList[2].args[0]);
        updated = updated with {
            actionEventList = ImmutableList<ActionEvent>.Empty
        };
        card = updated.selfSinglePlayerAreaModel.battleRowAreaList[(int)CardBadgeType.Brass].cardListModel.cardList[0];
        updated = updated.ChooseCard(card, true);
        Assert.AreEqual(GameState.State.WAIT_ENEMY_ACTION, updated.gameState.curState);
        Assert.AreEqual(GameState.ActionState.None, updated.gameState.actionState);
        Assert.AreEqual(0, updated.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(5, updated.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(1, updated.selfSinglePlayerAreaModel.battleRowAreaList[(int)CardBadgeType.Brass].cardListModel.cardList.Count);
        Assert.AreEqual(6, updated.selfSinglePlayerAreaModel.battleRowAreaList[(int)CardBadgeType.Brass].cardListModel.cardList[0].cardInfo.infoId);
        Assert.AreEqual(CardLocation.BattleArea, updated.selfSinglePlayerAreaModel.battleRowAreaList[(int)CardBadgeType.Brass].cardListModel.cardList[0].cardLocation);
        Assert.AreEqual(1, updated.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList.Count);
        Assert.AreEqual(2, updated.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0].cardInfo.infoId);
        Assert.AreEqual(2, updated.actionEventList.Count);
        Assert.AreEqual(ActionEvent.Type.BattleMsg, updated.actionEventList[0].type);
        Assert.AreEqual(BattleModel.ActionType.ChooseCard, updated.actionEventList[0].args[0]);
        Assert.AreEqual(card.cardInfo.id, updated.actionEventList[0].args[1]);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[1].type);
        Assert.AreEqual("施放<b>大号君</b>技能，换下卡牌：<b>普通牌-3</b>\n", updated.actionEventList[1].args[0]);
    }

    [Test]
    public void ChooseCard_Decoy_Normal_Enemy()
    {
        SetHostFirst(false);
        AppendAllCardInfoList(new List<CardInfo> {
            new CardInfo { chineseName = "大号君", infoId = 6, originPower = 0, ability = CardAbility.Decoy },
        });
        var model = new WholeAreaModel(true, "S", "E", CardGroup.KumikoFirstYear);
        var updated = model.SelfInit(new List<int> { 1 })
            .EnemyInit(new List<int> { 2, 6 }, new List<int> { 23, 63 }, CardGroup.KumikoThirdYear, false) // 使用非默认的brass牌
            .SelfDrawInitHandCard()
            .SelfReDrawInitHandCard()
            .EnemyDrawInitHandCard(new List<int> { 23, 63 });
        var normalCard = updated.enemySinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList.Find(o => o.cardInfo.ability == CardAbility.None);
        updated = updated.ChooseCard(normalCard, false);
        updated = updated.ChooseCard(updated.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0], true);
        updated = updated with {
            actionEventList = ImmutableList<ActionEvent>.Empty
        };
        var card = updated.enemySinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0];
        updated = updated.ChooseCard(updated.enemySinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0], false);
        Assert.AreEqual(GameState.State.WAIT_ENEMY_ACTION, updated.gameState.curState);
        Assert.AreEqual(GameState.ActionState.DECOYING, updated.gameState.actionState);
        Assert.AreEqual(5, updated.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(3, updated.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(1, updated.actionEventList.Count);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[0].type);
        Assert.AreEqual("<color=red>E</color> 打出卡牌：<b>大号君</b>\n", updated.actionEventList[0].args[0]);
        updated = updated with {
            actionEventList = ImmutableList<ActionEvent>.Empty
        };
        card = updated.enemySinglePlayerAreaModel.battleRowAreaList[(int)CardBadgeType.Brass].cardListModel.cardList[0];
        updated = updated.ChooseCard(card, false);
        Assert.AreEqual(GameState.State.WAIT_SELF_ACTION, updated.gameState.curState);
        Assert.AreEqual(GameState.ActionState.None, updated.gameState.actionState);
        Assert.AreEqual(5, updated.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(0, updated.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(1, updated.enemySinglePlayerAreaModel.battleRowAreaList[(int)CardBadgeType.Brass].cardListModel.cardList.Count);
        Assert.AreEqual(6, updated.enemySinglePlayerAreaModel.battleRowAreaList[(int)CardBadgeType.Brass].cardListModel.cardList[0].cardInfo.infoId);
        Assert.AreEqual(CardLocation.BattleArea, updated.enemySinglePlayerAreaModel.battleRowAreaList[(int)CardBadgeType.Brass].cardListModel.cardList[0].cardLocation);
        Assert.AreEqual(1, updated.enemySinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList.Count);
        Assert.AreEqual(2, updated.enemySinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0].cardInfo.infoId);
        Assert.AreEqual(1, updated.actionEventList.Count);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[0].type);
        Assert.AreEqual("施放<b>大号君</b>技能，换下卡牌：<b>普通牌-3</b>\n", updated.actionEventList[0].args[0]);
    }

    // 大号君 中断
    [Test]
    public void ChooseCard_Decoy_Interrupt_Self()
    {
        SetHostFirst(true);
        AppendAllCardInfoList(new List<CardInfo> {
            new CardInfo { chineseName = "大号君", infoId = 6, originPower = 0, ability = CardAbility.Decoy },
        });
        var model = new WholeAreaModel(true, "S", "E", CardGroup.KumikoFirstYear);
        var updated = model.SelfInit(new List<int> { 2, 6 })
            .EnemyInit(new List<int> { 1 }, new List<int> { 13 }, CardGroup.KumikoThirdYear, false)
            .SelfDrawInitHandCard()
            .SelfReDrawInitHandCard()
            .EnemyDrawInitHandCard(new List<int> { 13 });
        var normalCard = updated.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList.Find(o => o.cardInfo.ability == CardAbility.None);
        updated = updated.ChooseCard(normalCard, true);
        updated = updated.ChooseCard(updated.enemySinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0], false);
        updated = updated.ChooseCard(updated.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0], true);
        updated = updated with {
            actionEventList = ImmutableList<ActionEvent>.Empty
        };
        updated = updated.InterruptAction(true);
        Assert.AreEqual(GameState.State.WAIT_ENEMY_ACTION, updated.gameState.curState);
        Assert.AreEqual(GameState.ActionState.None, updated.gameState.actionState);
        Assert.AreEqual(3, updated.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(5, updated.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(2, updated.actionEventList.Count);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[0].type);
        Assert.AreEqual("<color=green>S</color> 未选择目标\n", updated.actionEventList[0].args[0]);
        Assert.AreEqual(ActionEvent.Type.BattleMsg, updated.actionEventList[1].type);
        Assert.AreEqual(BattleModel.ActionType.InterruptAction, updated.actionEventList[1].args[0]);
    }

    [Test]
    public void ChooseCard_Decoy_Interrupt_Enemy()
    {
        SetHostFirst(false);
        AppendAllCardInfoList(new List<CardInfo> {
            new CardInfo { chineseName = "大号君", infoId = 6, originPower = 0, ability = CardAbility.Decoy },
        });
        var model = new WholeAreaModel(true, "S", "E", CardGroup.KumikoFirstYear);
        var updated = model.SelfInit(new List<int> { 1 })
            .EnemyInit(new List<int> { 2, 6 }, new List<int> { 23, 63 }, CardGroup.KumikoThirdYear, false)
            .SelfDrawInitHandCard()
            .SelfReDrawInitHandCard()
            .EnemyDrawInitHandCard(new List<int> { 23, 63 });
        var normalCard = updated.enemySinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList.Find(o => o.cardInfo.ability == CardAbility.None);
        updated = updated.ChooseCard(normalCard, false);
        updated = updated.ChooseCard(updated.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0], true);
        updated = updated.ChooseCard(updated.enemySinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0], false);
        updated = updated with {
            actionEventList = ImmutableList<ActionEvent>.Empty
        };
        updated = updated.InterruptAction(false);
        Assert.AreEqual(GameState.State.WAIT_SELF_ACTION, updated.gameState.curState);
        Assert.AreEqual(GameState.ActionState.None, updated.gameState.actionState);
        Assert.AreEqual(5, updated.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(3, updated.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(1, updated.actionEventList.Count);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[0].type);
        Assert.AreEqual("<color=red>E</color> 未选择目标\n", updated.actionEventList[0].args[0]);
    }

    // 退部 无目标
    [Test]
    public void ChooseCard_Scorch_NoTarget_Self()
    {
        SetHostFirst(true);
        AppendAllCardInfoList(new List<CardInfo> {
            new CardInfo { chineseName = "退部牌", infoId = 6, originPower = 5, ability = CardAbility.Scorch, cardType = CardType.Util },
        });
        var model = new WholeAreaModel(true, "S", "E", CardGroup.KumikoFirstYear);
        var updated = model.SelfInit(new List<int> { 6 })
            .EnemyInit(new List<int> { 1 }, new List<int> { 13 }, CardGroup.KumikoThirdYear, false)
            .SelfDrawInitHandCard()
            .SelfReDrawInitHandCard()
            .EnemyDrawInitHandCard(new List<int> { 13 });
        updated = updated with {
            actionEventList = ImmutableList<ActionEvent>.Empty
        };
        CardModel card = updated.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0];
        updated = updated.ChooseCard(card, true);
        Assert.AreEqual(GameState.State.WAIT_ENEMY_ACTION, updated.gameState.curState);
        Assert.AreEqual(0, updated.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(0, updated.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(0, updated.selfSinglePlayerAreaModel.battleRowAreaList[(int)CardBadgeType.Wood].cardListModel.cardList.Count);
        Assert.AreEqual(3, updated.actionEventList.Count);
        Assert.AreEqual(ActionEvent.Type.BattleMsg, updated.actionEventList[0].type);
        Assert.AreEqual(BattleModel.ActionType.ChooseCard, updated.actionEventList[0].args[0]);
        Assert.AreEqual(card.cardInfo.id, updated.actionEventList[0].args[1]);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[1].type);
        Assert.AreEqual("<color=green>S</color> 打出卡牌：<b>退部牌</b>\n", updated.actionEventList[1].args[0]);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[2].type);
        Assert.AreEqual("施放<b>退部申请书</b>技能，无退部目标\n", updated.actionEventList[2].args[0]);
    }

    [Test]
    public void ChooseCard_Scorch_NoTarget_Enemy()
    {
        SetHostFirst(false);
        AppendAllCardInfoList(new List<CardInfo> {
            new CardInfo { chineseName = "退部牌", infoId = 6, originPower = 5, ability = CardAbility.Scorch, cardType = CardType.Util },
        });
        var model = new WholeAreaModel(true, "S", "E", CardGroup.KumikoFirstYear);
        var updated = model.SelfInit(new List<int> { 1 })
            .EnemyInit(new List<int> { 6 }, new List<int> { 63 }, CardGroup.KumikoThirdYear, false)
            .SelfDrawInitHandCard()
            .SelfReDrawInitHandCard()
            .EnemyDrawInitHandCard(new List<int> { 63 });
        updated = updated with {
            actionEventList = ImmutableList<ActionEvent>.Empty
        };
        updated = updated.ChooseCard(updated.enemySinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0], false);
        Assert.AreEqual(GameState.State.WAIT_SELF_ACTION, updated.gameState.curState);
        Assert.AreEqual(0, updated.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(0, updated.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(0, updated.enemySinglePlayerAreaModel.battleRowAreaList[(int)CardBadgeType.Wood].cardListModel.cardList.Count);
        Assert.AreEqual(2, updated.actionEventList.Count);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[0].type);
        Assert.AreEqual("<color=red>E</color> 打出卡牌：<b>退部牌</b>\n", updated.actionEventList[0].args[0]);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[1].type);
        Assert.AreEqual("施放<b>退部申请书</b>技能，无退部目标\n", updated.actionEventList[1].args[0]);
    }

    // 退部
    [Test]
    public void ChooseCard_Scorch_Normal_Self()
    {
        SetHostFirst(false);
        AppendAllCardInfoList(new List<CardInfo> {
            new CardInfo { chineseName = "退部牌", infoId = 6, originPower = 5, ability = CardAbility.Scorch, cardType = CardType.Util },
            new CardInfo { chineseName = "普通牌-11", infoId = 7, originPower = 11, ability = CardAbility.None, badgeType = CardBadgeType.Wood },
        });
        var model = new WholeAreaModel(true, "S", "E", CardGroup.KumikoFirstYear);
        var updated = model.SelfInit(new List<int> { 6 })
            .EnemyInit(new List<int> { 7 }, new List<int> { 73 }, CardGroup.KumikoThirdYear, false)
            .SelfDrawInitHandCard()
            .SelfReDrawInitHandCard()
            .EnemyDrawInitHandCard(new List<int> { 73 });
        updated = updated.ChooseCard(updated.enemySinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0], false);
        updated = updated with {
            actionEventList = ImmutableList<ActionEvent>.Empty
        };
        CardModel card = updated.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0];
        updated = updated.ChooseCard(card, true);
        Assert.AreEqual(GameState.State.WAIT_ENEMY_ACTION, updated.gameState.curState);
        Assert.AreEqual(0, updated.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(0, updated.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(1, updated.enemySinglePlayerAreaModel.discardAreaModel.cardListModel.cardList.Count);
        Assert.AreEqual(0, updated.selfSinglePlayerAreaModel.battleRowAreaList[(int)CardBadgeType.Wood].cardListModel.cardList.Count);
        Assert.AreEqual(4, updated.actionEventList.Count);
        Assert.AreEqual(ActionEvent.Type.BattleMsg, updated.actionEventList[0].type);
        Assert.AreEqual(BattleModel.ActionType.ChooseCard, updated.actionEventList[0].args[0]);
        Assert.AreEqual(card.cardInfo.id, updated.actionEventList[0].args[1]);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[1].type);
        Assert.AreEqual("<color=green>S</color> 打出卡牌：<b>退部牌</b>\n", updated.actionEventList[1].args[0]);
        Assert.AreEqual(ActionEvent.Type.Sfx, updated.actionEventList[2].type);
        Assert.AreEqual(AudioManager.SFXType.Scorch, updated.actionEventList[2].args[0]);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[3].type);
        Assert.AreEqual("施放<b>退部申请书</b>技能，移除卡牌：<b>普通牌-11</b>\n", updated.actionEventList[3].args[0]);
    }

    [Test]
    public void ChooseCard_Scorch_Normal_Enemy()
    {
        SetHostFirst(true);
        AppendAllCardInfoList(new List<CardInfo> {
            new CardInfo { chineseName = "退部牌", infoId = 6, originPower = 5, ability = CardAbility.Scorch, cardType = CardType.Util },
            new CardInfo { chineseName = "普通牌-11", infoId = 7, originPower = 11, ability = CardAbility.None, badgeType = CardBadgeType.Wood },
        });
        var model = new WholeAreaModel(true, "S", "E", CardGroup.KumikoFirstYear);
        var updated = model.SelfInit(new List<int> { 7 })
            .EnemyInit(new List<int> { 6 }, new List<int> { 63 }, CardGroup.KumikoThirdYear, false)
            .SelfDrawInitHandCard()
            .SelfReDrawInitHandCard()
            .EnemyDrawInitHandCard(new List<int> { 63 });
        updated = updated.ChooseCard(updated.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0], true);
        updated = updated with {
            actionEventList = ImmutableList<ActionEvent>.Empty
        };
        updated = updated.ChooseCard(updated.enemySinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0], false);
        Assert.AreEqual(GameState.State.WAIT_SELF_ACTION, updated.gameState.curState);
        Assert.AreEqual(0, updated.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(0, updated.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(1, updated.selfSinglePlayerAreaModel.discardAreaModel.cardListModel.cardList.Count);
        Assert.AreEqual(0, updated.enemySinglePlayerAreaModel.battleRowAreaList[(int)CardBadgeType.Wood].cardListModel.cardList.Count);
        Assert.AreEqual(3, updated.actionEventList.Count);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[0].type);
        Assert.AreEqual("<color=red>E</color> 打出卡牌：<b>退部牌</b>\n", updated.actionEventList[0].args[0]);
        Assert.AreEqual(ActionEvent.Type.Sfx, updated.actionEventList[1].type);
        Assert.AreEqual(AudioManager.SFXType.Scorch, updated.actionEventList[1].args[0]);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[2].type);
        Assert.AreEqual("施放<b>退部申请书</b>技能，移除卡牌：<b>普通牌-11</b>\n", updated.actionEventList[2].args[0]);
    }

    // 退部 角色牌
    [Test]
    public void ChooseCard_Scorch_Role_Self()
    {
        SetHostFirst(false);
        AppendAllCardInfoList(new List<CardInfo> {
            new CardInfo { chineseName = "退部牌", infoId = 6, originPower = 5, ability = CardAbility.Scorch, badgeType = CardBadgeType.Wood },
            new CardInfo { chineseName = "普通牌-11", infoId = 7, originPower = 11, ability = CardAbility.None, badgeType = CardBadgeType.Wood },
        });
        var model = new WholeAreaModel(true, "S", "E", CardGroup.KumikoFirstYear);
        var updated = model.SelfInit(new List<int> { 6 })
            .EnemyInit(new List<int> { 7 }, new List<int> { 73 }, CardGroup.KumikoThirdYear, false)
            .SelfDrawInitHandCard()
            .SelfReDrawInitHandCard()
            .EnemyDrawInitHandCard(new List<int> { 73 });
        updated = updated.ChooseCard(updated.enemySinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0], false);
        updated = updated with {
            actionEventList = ImmutableList<ActionEvent>.Empty
        };
        CardModel card = updated.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0];
        updated = updated.ChooseCard(card, true);
        Assert.AreEqual(GameState.State.WAIT_ENEMY_ACTION, updated.gameState.curState);
        Assert.AreEqual(5, updated.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(0, updated.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(1, updated.enemySinglePlayerAreaModel.discardAreaModel.cardListModel.cardList.Count);
        Assert.AreEqual(4, updated.actionEventList.Count);
        Assert.AreEqual(ActionEvent.Type.BattleMsg, updated.actionEventList[0].type);
        Assert.AreEqual(BattleModel.ActionType.ChooseCard, updated.actionEventList[0].args[0]);
        Assert.AreEqual(card.cardInfo.id, updated.actionEventList[0].args[1]);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[1].type);
        Assert.AreEqual("<color=green>S</color> 打出卡牌：<b>退部牌</b>\n", updated.actionEventList[1].args[0]);
        Assert.AreEqual(ActionEvent.Type.Sfx, updated.actionEventList[2].type);
        Assert.AreEqual(AudioManager.SFXType.Scorch, updated.actionEventList[2].args[0]);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[3].type);
        Assert.AreEqual("施放<b>退部申请书</b>技能，移除卡牌：<b>普通牌-11</b>\n", updated.actionEventList[3].args[0]);
    }

    [Test]
    public void ChooseCard_Scorch_Role_Enemy()
    {
        SetHostFirst(true);
        AppendAllCardInfoList(new List<CardInfo> {
            new CardInfo { chineseName = "退部牌", infoId = 6, originPower = 5, ability = CardAbility.Scorch, badgeType = CardBadgeType.Wood },
            new CardInfo { chineseName = "普通牌-11", infoId = 7, originPower = 11, ability = CardAbility.None, badgeType = CardBadgeType.Wood },
        });
        var model = new WholeAreaModel(true, "S", "E", CardGroup.KumikoFirstYear);
        var updated = model.SelfInit(new List<int> { 7 })
            .EnemyInit(new List<int> { 6 }, new List<int> { 63 }, CardGroup.KumikoThirdYear, false)
            .SelfDrawInitHandCard()
            .SelfReDrawInitHandCard()
            .EnemyDrawInitHandCard(new List<int> { 63 });
        updated = updated.ChooseCard(updated.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0], true);
        updated = updated with {
            actionEventList = ImmutableList<ActionEvent>.Empty
        };
        updated = updated.ChooseCard(updated.enemySinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0], false);
        Assert.AreEqual(GameState.State.WAIT_SELF_ACTION, updated.gameState.curState);
        Assert.AreEqual(0, updated.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(5, updated.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(1, updated.selfSinglePlayerAreaModel.discardAreaModel.cardListModel.cardList.Count);
        Assert.AreEqual(3, updated.actionEventList.Count);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[0].type);
        Assert.AreEqual("<color=red>E</color> 打出卡牌：<b>退部牌</b>\n", updated.actionEventList[0].args[0]);
        Assert.AreEqual(ActionEvent.Type.Sfx, updated.actionEventList[1].type);
        Assert.AreEqual(AudioManager.SFXType.Scorch, updated.actionEventList[1].args[0]);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[2].type);
        Assert.AreEqual("施放<b>退部申请书</b>技能，移除卡牌：<b>普通牌-11</b>\n", updated.actionEventList[2].args[0]);
    }

    // 天气牌
    [Test]
    public void ChooseCard_Weather_Self()
    {
        SetHostFirst(false);
        AppendAllCardInfoList(new List<CardInfo> {
            new CardInfo { chineseName = "天气牌-木管", infoId = 6, ability = CardAbility.SunFes, cardType = CardType.Util },
            new CardInfo { chineseName = "天气牌-铜管", infoId = 7, ability = CardAbility.Daisangakushou, cardType = CardType.Util },
            new CardInfo { chineseName = "天气牌-打击", infoId = 8, ability = CardAbility.Drumstick, cardType = CardType.Util },
            new CardInfo { chineseName = "普通牌-木管", infoId = 9, originPower = 5, ability = CardAbility.None, badgeType = CardBadgeType.Wood },
            new CardInfo { chineseName = "普通牌-铜管", infoId = 10, originPower = 5, ability = CardAbility.None, badgeType = CardBadgeType.Brass },
            new CardInfo { chineseName = "普通牌-打击", infoId = 11, originPower = 5, ability = CardAbility.None, badgeType = CardBadgeType.Percussion },
        });
        var model = new WholeAreaModel(true, "S", "E", CardGroup.KumikoFirstYear);
        var updated = model.SelfInit(new List<int> { 6, 7, 8 })
            .EnemyInit(new List<int> { 9, 10, 11 }, new List<int> { 93, 103, 113 }, CardGroup.KumikoThirdYear, false)
            .SelfDrawInitHandCard()
            .SelfReDrawInitHandCard()
            .EnemyDrawInitHandCard(new List<int> { 93, 103, 113 });
        var card = updated.enemySinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList.Find(o => o.cardInfo.infoId == 9);
        updated = updated.ChooseCard(card, false);
        updated = updated with {
            actionEventList = ImmutableList<ActionEvent>.Empty
        };
        card = updated.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList.Find(o => o.cardInfo.infoId == 6);
        updated = updated.ChooseCard(card, true);
        Assert.AreEqual(GameState.State.WAIT_ENEMY_ACTION, updated.gameState.curState);
        Assert.AreEqual(0, updated.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(1, updated.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(1, updated.weatherAreaModel.wood.cardList.Count);
        Assert.AreEqual(2, updated.actionEventList.Count);
        Assert.AreEqual(ActionEvent.Type.BattleMsg, updated.actionEventList[0].type);
        Assert.AreEqual(BattleModel.ActionType.ChooseCard, updated.actionEventList[0].args[0]);
        Assert.AreEqual(card.cardInfo.id, updated.actionEventList[0].args[1]);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[1].type);
        Assert.AreEqual("<color=green>S</color> 打出卡牌：<b>天气牌-木管</b>\n", updated.actionEventList[1].args[0]);
        card = updated.enemySinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList.Find(o => o.cardInfo.infoId == 10);
        updated = updated.ChooseCard(card, false);
        Assert.AreEqual(0, updated.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(6, updated.enemySinglePlayerAreaModel.GetCurrentPower());
        updated = updated with {
            actionEventList = ImmutableList<ActionEvent>.Empty
        };
        card = updated.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList.Find(o => o.cardInfo.infoId == 7);
        updated = updated.ChooseCard(card, true);
        Assert.AreEqual(GameState.State.WAIT_ENEMY_ACTION, updated.gameState.curState);
        Assert.AreEqual(0, updated.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(2, updated.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(1, updated.weatherAreaModel.wood.cardList.Count);
        Assert.AreEqual(1, updated.weatherAreaModel.brass.cardList.Count);
        Assert.AreEqual(2, updated.actionEventList.Count);
        Assert.AreEqual(ActionEvent.Type.BattleMsg, updated.actionEventList[0].type);
        Assert.AreEqual(BattleModel.ActionType.ChooseCard, updated.actionEventList[0].args[0]);
        Assert.AreEqual(card.cardInfo.id, updated.actionEventList[0].args[1]);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[1].type);
        Assert.AreEqual("<color=green>S</color> 打出卡牌：<b>天气牌-铜管</b>\n", updated.actionEventList[1].args[0]);
        card = updated.enemySinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList.Find(o => o.cardInfo.infoId == 11);
        updated = updated.ChooseCard(card, false);
        Assert.AreEqual(0, updated.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(7, updated.enemySinglePlayerAreaModel.GetCurrentPower());
        updated = updated with {
            actionEventList = ImmutableList<ActionEvent>.Empty
        };
        card = updated.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList.Find(o => o.cardInfo.infoId == 8);
        updated = updated.ChooseCard(card, true);
        Assert.AreEqual(GameState.State.WAIT_ENEMY_ACTION, updated.gameState.curState);
        Assert.AreEqual(0, updated.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(3, updated.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(1, updated.weatherAreaModel.wood.cardList.Count);
        Assert.AreEqual(1, updated.weatherAreaModel.brass.cardList.Count);
        Assert.AreEqual(1, updated.weatherAreaModel.percussion.cardList.Count);
        Assert.AreEqual(2, updated.actionEventList.Count);
        Assert.AreEqual(ActionEvent.Type.BattleMsg, updated.actionEventList[0].type);
        Assert.AreEqual(BattleModel.ActionType.ChooseCard, updated.actionEventList[0].args[0]);
        Assert.AreEqual(card.cardInfo.id, updated.actionEventList[0].args[1]);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[1].type);
        Assert.AreEqual("<color=green>S</color> 打出卡牌：<b>天气牌-打击</b>\n", updated.actionEventList[1].args[0]);
    }

    [Test]
    public void ChooseCard_Weather_Enemy()
    {
        SetHostFirst(true);
        AppendAllCardInfoList(new List<CardInfo> {
            new CardInfo { chineseName = "天气牌-木管", infoId = 6, ability = CardAbility.SunFes, cardType = CardType.Util },
            new CardInfo { chineseName = "天气牌-铜管", infoId = 7, ability = CardAbility.Daisangakushou, cardType = CardType.Util },
            new CardInfo { chineseName = "天气牌-打击", infoId = 8, ability = CardAbility.Drumstick, cardType = CardType.Util },
            new CardInfo { chineseName = "普通牌-木管", infoId = 9, originPower = 5, ability = CardAbility.None, badgeType = CardBadgeType.Wood },
            new CardInfo { chineseName = "普通牌-铜管", infoId = 10, originPower = 5, ability = CardAbility.None, badgeType = CardBadgeType.Brass },
            new CardInfo { chineseName = "普通牌-打击", infoId = 11, originPower = 5, ability = CardAbility.None, badgeType = CardBadgeType.Percussion },
        });
        var model = new WholeAreaModel(true, "S", "E", CardGroup.KumikoFirstYear);
        var updated = model.SelfInit(new List<int> { 9, 10, 11 })
            .EnemyInit(new List<int> { 6, 7, 8 }, new List<int> { 63, 73, 83 }, CardGroup.KumikoThirdYear, false)
            .SelfDrawInitHandCard()
            .SelfReDrawInitHandCard()
            .EnemyDrawInitHandCard(new List<int> { 63, 73, 83 });
        var card = updated.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList.Find(o => o.cardInfo.infoId == 9);
        updated = updated.ChooseCard(card, true);
        updated = updated with {
            actionEventList = ImmutableList<ActionEvent>.Empty
        };
        card = updated.enemySinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList.Find(o => o.cardInfo.infoId == 6);
        updated = updated.ChooseCard(card, false);
        Assert.AreEqual(GameState.State.WAIT_SELF_ACTION, updated.gameState.curState);
        Assert.AreEqual(1, updated.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(0, updated.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(1, updated.weatherAreaModel.wood.cardList.Count);
        Assert.AreEqual(1, updated.actionEventList.Count);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[0].type);
        Assert.AreEqual("<color=red>E</color> 打出卡牌：<b>天气牌-木管</b>\n", updated.actionEventList[0].args[0]);
        card = updated.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList.Find(o => o.cardInfo.infoId == 10);
        updated = updated.ChooseCard(card, true);
        Assert.AreEqual(6, updated.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(0, updated.enemySinglePlayerAreaModel.GetCurrentPower());
        updated = updated with {
            actionEventList = ImmutableList<ActionEvent>.Empty
        };
        card = updated.enemySinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList.Find(o => o.cardInfo.infoId == 7);
        updated = updated.ChooseCard(card, false);
        Assert.AreEqual(GameState.State.WAIT_SELF_ACTION, updated.gameState.curState);
        Assert.AreEqual(2, updated.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(0, updated.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(1, updated.weatherAreaModel.wood.cardList.Count);
        Assert.AreEqual(1, updated.weatherAreaModel.brass.cardList.Count);
        Assert.AreEqual(1, updated.actionEventList.Count);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[0].type);
        Assert.AreEqual("<color=red>E</color> 打出卡牌：<b>天气牌-铜管</b>\n", updated.actionEventList[0].args[0]);
        card = updated.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList.Find(o => o.cardInfo.infoId == 11);
        updated = updated.ChooseCard(card, true);
        Assert.AreEqual(7, updated.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(0, updated.enemySinglePlayerAreaModel.GetCurrentPower());
        updated = updated with {
            actionEventList = ImmutableList<ActionEvent>.Empty
        };
        card = updated.enemySinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList.Find(o => o.cardInfo.infoId == 8);
        updated = updated.ChooseCard(card, false);
        Assert.AreEqual(GameState.State.WAIT_SELF_ACTION, updated.gameState.curState);
        Assert.AreEqual(3, updated.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(0, updated.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(1, updated.weatherAreaModel.wood.cardList.Count);
        Assert.AreEqual(1, updated.weatherAreaModel.brass.cardList.Count);
        Assert.AreEqual(1, updated.weatherAreaModel.percussion.cardList.Count);
        Assert.AreEqual(1, updated.actionEventList.Count);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[0].type);
        Assert.AreEqual("<color=red>E</color> 打出卡牌：<b>天气牌-打击</b>\n", updated.actionEventList[0].args[0]);
    }

    // 天气牌 清除
    [Test]
    public void ChooseCard_Weather_Clear_Self()
    {
        SetHostFirst(true);
        AppendAllCardInfoList(new List<CardInfo> {
            new CardInfo { chineseName = "天气牌-铜管", infoId = 6, ability = CardAbility.Daisangakushou, cardType = CardType.Util },
            new CardInfo { chineseName = "普通牌-铜管", infoId = 7, originPower = 5, ability = CardAbility.None, badgeType = CardBadgeType.Brass },
            new CardInfo { chineseName = "晴天牌", infoId = 8, ability = CardAbility.ClearWeather },
        });
        var model = new WholeAreaModel(true, "S", "E", CardGroup.KumikoFirstYear);
        var updated = model.SelfInit(new List<int> { 7, 8 })
            .EnemyInit(new List<int> { 6 }, new List<int> { 63 }, CardGroup.KumikoThirdYear, false)
            .SelfDrawInitHandCard()
            .SelfReDrawInitHandCard()
            .EnemyDrawInitHandCard(new List<int> { 63 });
        var card = updated.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList.Find(o => o.cardInfo.infoId == 7);
        updated = updated.ChooseCard(card, true);
        updated = updated.ChooseCard(updated.enemySinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0], false);
        updated = updated with {
            actionEventList = ImmutableList<ActionEvent>.Empty
        };
        card = updated.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0];
        updated = updated.ChooseCard(card, true);
        Assert.AreEqual(GameState.State.WAIT_ENEMY_ACTION, updated.gameState.curState);
        Assert.AreEqual(5, updated.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(0, updated.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(0, updated.weatherAreaModel.brass.cardList.Count);
        Assert.AreEqual(2, updated.actionEventList.Count);
        Assert.AreEqual(ActionEvent.Type.BattleMsg, updated.actionEventList[0].type);
        Assert.AreEqual(BattleModel.ActionType.ChooseCard, updated.actionEventList[0].args[0]);
        Assert.AreEqual(card.cardInfo.id, updated.actionEventList[0].args[1]);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[1].type);
        Assert.AreEqual("<color=green>S</color> 打出卡牌：<b>晴天牌</b>\n", updated.actionEventList[1].args[0]);
    }

    [Test]
    public void ChooseCard_Weather_Clear_Enemy()
    {
        SetHostFirst(false);
        AppendAllCardInfoList(new List<CardInfo> {
            new CardInfo { chineseName = "天气牌-铜管", infoId = 6, ability = CardAbility.Daisangakushou, cardType = CardType.Util },
            new CardInfo { chineseName = "普通牌-铜管", infoId = 7, originPower = 5, ability = CardAbility.None, badgeType = CardBadgeType.Brass },
            new CardInfo { chineseName = "晴天牌", infoId = 8, ability = CardAbility.ClearWeather },
        });
        var model = new WholeAreaModel(true, "S", "E", CardGroup.KumikoFirstYear);
        var updated = model.SelfInit(new List<int> { 6 })
            .EnemyInit(new List<int> { 7, 8 }, new List<int> { 73, 83 }, CardGroup.KumikoThirdYear, false)
            .SelfDrawInitHandCard()
            .SelfReDrawInitHandCard()
            .EnemyDrawInitHandCard(new List<int> { 73, 83 });
        var card = updated.enemySinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList.Find(o => o.cardInfo.infoId == 7);
        updated = updated.ChooseCard(card, false);
        updated = updated.ChooseCard(updated.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0], true);
        updated = updated with {
            actionEventList = ImmutableList<ActionEvent>.Empty
        };
        card = updated.enemySinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0];
        updated = updated.ChooseCard(card, false);
        Assert.AreEqual(GameState.State.WAIT_SELF_ACTION, updated.gameState.curState);
        Assert.AreEqual(0, updated.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(5, updated.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(0, updated.weatherAreaModel.brass.cardList.Count);
        Assert.AreEqual(1, updated.actionEventList.Count);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[0].type);
        Assert.AreEqual("<color=red>E</color> 打出卡牌：<b>晴天牌</b>\n", updated.actionEventList[0].args[0]);
    }

    // 天气牌 替换
    [Test]
    public void ChooseCard_Weather_Replace_Self()
    {
        SetHostFirst(false);
        AppendAllCardInfoList(new List<CardInfo> {
            new CardInfo { chineseName = "天气牌-铜管", infoId = 6, ability = CardAbility.Daisangakushou, cardType = CardType.Util },
        });
        var model = new WholeAreaModel(true, "S", "E", CardGroup.KumikoFirstYear);
        var updated = model.SelfInit(new List<int> { 6 })
            .EnemyInit(new List<int> { 6 }, new List<int> { 63 }, CardGroup.KumikoThirdYear, false)
            .SelfDrawInitHandCard()
            .SelfReDrawInitHandCard()
            .EnemyDrawInitHandCard(new List<int> { 63 });
        updated = updated.ChooseCard(updated.enemySinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0], false);
        updated = updated with {
            actionEventList = ImmutableList<ActionEvent>.Empty
        };
        var card = updated.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0];
        updated = updated.ChooseCard(card, true);
        Assert.AreEqual(GameState.State.WAIT_ENEMY_ACTION, updated.gameState.curState);
        Assert.AreEqual(0, updated.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(0, updated.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(1, updated.weatherAreaModel.brass.cardList.Count);
        Assert.AreEqual(card.cardInfo.id, updated.weatherAreaModel.brass.cardList[0].cardInfo.id);
        Assert.AreEqual(2, updated.actionEventList.Count);
        Assert.AreEqual(ActionEvent.Type.BattleMsg, updated.actionEventList[0].type);
        Assert.AreEqual(BattleModel.ActionType.ChooseCard, updated.actionEventList[0].args[0]);
        Assert.AreEqual(card.cardInfo.id, updated.actionEventList[0].args[1]);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[1].type);
        Assert.AreEqual("<color=green>S</color> 打出卡牌：<b>天气牌-铜管</b>\n", updated.actionEventList[1].args[0]);
    }

    [Test]
    public void ChooseCard_Weather_Replace_Enemy()
    {
        SetHostFirst(true);
        AppendAllCardInfoList(new List<CardInfo> {
            new CardInfo { chineseName = "天气牌-铜管", infoId = 6, ability = CardAbility.Daisangakushou, cardType = CardType.Util },
        });
        var model = new WholeAreaModel(true, "S", "E", CardGroup.KumikoFirstYear);
        var updated = model.SelfInit(new List<int> { 6 })
            .EnemyInit(new List<int> { 6 }, new List<int> { 63 }, CardGroup.KumikoThirdYear, false)
            .SelfDrawInitHandCard()
            .SelfReDrawInitHandCard()
            .EnemyDrawInitHandCard(new List<int> { 63 });
        updated = updated.ChooseCard(updated.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0], true);
        updated = updated with {
            actionEventList = ImmutableList<ActionEvent>.Empty
        };
        var card = updated.enemySinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0];
        updated = updated.ChooseCard(card, false);
        Assert.AreEqual(GameState.State.WAIT_SELF_ACTION, updated.gameState.curState);
        Assert.AreEqual(0, updated.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(0, updated.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(1, updated.weatherAreaModel.brass.cardList.Count);
        Assert.AreEqual(card.cardInfo.id, updated.weatherAreaModel.brass.cardList[0].cardInfo.id);
        Assert.AreEqual(1, updated.actionEventList.Count);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[0].type);
        Assert.AreEqual("<color=red>E</color> 打出卡牌：<b>天气牌-铜管</b>\n", updated.actionEventList[0].args[0]);
    }

    // 指导老师
    [Test]
    public void ChooseCard_HornUtil_Self()
    {
        SetHostFirst(true);
        AppendAllCardInfoList(new List<CardInfo> {
            new CardInfo { chineseName = "指导老师牌", infoId = 6, ability = CardAbility.HornUtil },
        });
        var model = new WholeAreaModel(true, "S", "E", CardGroup.KumikoFirstYear);
        var updated = model.SelfInit(new List<int> { 1, 6 })
            .EnemyInit(new List<int> { 1 }, new List<int> { 13 }, CardGroup.KumikoThirdYear, false)
            .SelfDrawInitHandCard()
            .SelfReDrawInitHandCard()
            .EnemyDrawInitHandCard(new List<int> { 13 });
        var card = updated.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList.Find(o => o.cardInfo.infoId == 1);
        updated = updated.ChooseCard(card, true);
        updated = updated.ChooseCard(updated.enemySinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0], false);
        updated = updated with {
            actionEventList = ImmutableList<ActionEvent>.Empty
        };
        card = updated.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0];
        updated = updated.ChooseCard(card, true);
        Assert.AreEqual(GameState.State.WAIT_SELF_ACTION, updated.gameState.curState);
        Assert.AreEqual(GameState.ActionState.HORN_UTILING, updated.gameState.actionState);
        Assert.AreEqual(5, updated.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(5, updated.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(3, updated.actionEventList.Count);
        Assert.AreEqual(ActionEvent.Type.BattleMsg, updated.actionEventList[0].type);
        Assert.AreEqual(BattleModel.ActionType.ChooseCard, updated.actionEventList[0].args[0]);
        Assert.AreEqual(card.cardInfo.id, updated.actionEventList[0].args[1]);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[1].type);
        Assert.AreEqual("<color=green>S</color> 打出卡牌：<b>指导老师牌</b>\n", updated.actionEventList[1].args[0]);
        Assert.AreEqual(ActionEvent.Type.Toast, updated.actionEventList[2].type);
        Assert.AreEqual("请选择指导老师的目标行", updated.actionEventList[2].args[0]);
        updated = updated with {
            actionEventList = ImmutableList<ActionEvent>.Empty
        };
        updated = updated.ChooseHornUtilArea(CardBadgeType.Wood, true);
        Assert.AreEqual(GameState.State.WAIT_ENEMY_ACTION, updated.gameState.curState);
        Assert.AreEqual(GameState.ActionState.None, updated.gameState.actionState);
        Assert.AreEqual(10, updated.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(5, updated.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(1, updated.actionEventList.Count);
        Assert.AreEqual(ActionEvent.Type.BattleMsg, updated.actionEventList[0].type);
        Assert.AreEqual(BattleModel.ActionType.ChooseHornUtilArea, updated.actionEventList[0].args[0]);
        Assert.AreEqual(BattleModel.HornUtilAreaType.Wood, updated.actionEventList[0].args[1]);
    }

    [Test]
    public void ChooseCard_HornUtil_Enemy()
    {
        SetHostFirst(false);
        AppendAllCardInfoList(new List<CardInfo> {
            new CardInfo { chineseName = "指导老师牌", infoId = 6, ability = CardAbility.HornUtil },
        });
        var model = new WholeAreaModel(true, "S", "E", CardGroup.KumikoFirstYear);
        var updated = model.SelfInit(new List<int> { 1 })
            .EnemyInit(new List<int> { 1, 6 }, new List<int> { 13, 63 }, CardGroup.KumikoThirdYear, false)
            .SelfDrawInitHandCard()
            .SelfReDrawInitHandCard()
            .EnemyDrawInitHandCard(new List<int> { 13, 63 });
        var card = updated.enemySinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList.Find(o => o.cardInfo.infoId == 1);
        updated = updated.ChooseCard(card, false);
        updated = updated.ChooseCard(updated.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0], true);
        updated = updated with {
            actionEventList = ImmutableList<ActionEvent>.Empty
        };
        card = updated.enemySinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0];
        updated = updated.ChooseCard(card, false);
        Assert.AreEqual(GameState.State.WAIT_ENEMY_ACTION, updated.gameState.curState);
        Assert.AreEqual(GameState.ActionState.HORN_UTILING, updated.gameState.actionState);
        Assert.AreEqual(5, updated.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(5, updated.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(1, updated.actionEventList.Count);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[0].type);
        Assert.AreEqual("<color=red>E</color> 打出卡牌：<b>指导老师牌</b>\n", updated.actionEventList[0].args[0]);
        updated = updated with {
            actionEventList = ImmutableList<ActionEvent>.Empty
        };
        updated = updated.ChooseHornUtilArea(CardBadgeType.Wood, false);
        Assert.AreEqual(GameState.State.WAIT_SELF_ACTION, updated.gameState.curState);
        Assert.AreEqual(GameState.ActionState.None, updated.gameState.actionState);
        Assert.AreEqual(5, updated.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(10, updated.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(0, updated.actionEventList.Count);
    }

    // 指导老师，替换
    [Test]
    public void ChooseCard_HornUtil_Replace_Self()
    {
        SetHostFirst(true);
        AppendAllCardInfoList(new List<CardInfo> {
            new CardInfo { chineseName = "指导老师牌", infoId = 6, ability = CardAbility.HornUtil },
        });
        var model = new WholeAreaModel(true, "S", "E", CardGroup.KumikoFirstYear);
        var updated = model.SelfInit(new List<int> { 1, 6, 6 })
            .EnemyInit(new List<int> { 1, 1 }, new List<int> { 13, 23 }, CardGroup.KumikoThirdYear, false)
            .SelfDrawInitHandCard()
            .SelfReDrawInitHandCard()
            .EnemyDrawInitHandCard(new List<int> { 13, 23 });
        var card = updated.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList.Find(o => o.cardInfo.infoId == 1);
        updated = updated.ChooseCard(card, true);
        updated = updated.ChooseCard(updated.enemySinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0], false);
        updated = updated.ChooseCard(updated.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0], true);
        updated = updated.ChooseHornUtilArea(CardBadgeType.Wood, true);
        updated = updated.ChooseCard(updated.enemySinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0], false);
        card = updated.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0];
        updated = updated.ChooseCard(card, true);
        updated = updated with {
            actionEventList = ImmutableList<ActionEvent>.Empty
        };
        updated = updated.ChooseHornUtilArea(CardBadgeType.Wood, true);
        Assert.AreEqual(GameState.State.WAIT_ENEMY_ACTION, updated.gameState.curState);
        Assert.AreEqual(GameState.ActionState.None, updated.gameState.actionState);
        Assert.AreEqual(10, updated.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(10, updated.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(1, updated.selfSinglePlayerAreaModel.battleRowAreaList[(int)CardBadgeType.Wood].hornCardListModel.cardList.Count);
        Assert.AreEqual(card.cardInfo.id, updated.selfSinglePlayerAreaModel.battleRowAreaList[(int)CardBadgeType.Wood].hornCardListModel.cardList[0].cardInfo.id);
        Assert.AreEqual(1, updated.actionEventList.Count);
        Assert.AreEqual(ActionEvent.Type.BattleMsg, updated.actionEventList[0].type);
        Assert.AreEqual(BattleModel.ActionType.ChooseHornUtilArea, updated.actionEventList[0].args[0]);
        Assert.AreEqual(BattleModel.HornUtilAreaType.Wood, updated.actionEventList[0].args[1]);
    }

    [Test]
    public void ChooseCard_HornUtil_Replace_Enemy()
    {
        SetHostFirst(false);
        AppendAllCardInfoList(new List<CardInfo> {
            new CardInfo { chineseName = "指导老师牌", infoId = 6, ability = CardAbility.HornUtil },
        });
        var model = new WholeAreaModel(true, "S", "E", CardGroup.KumikoFirstYear);
        var updated = model.SelfInit(new List<int> { 1, 1 })
            .EnemyInit(new List<int> { 1, 6, 6 }, new List<int> { 13, 63, 73 }, CardGroup.KumikoThirdYear, false)
            .SelfDrawInitHandCard()
            .SelfReDrawInitHandCard()
            .EnemyDrawInitHandCard(new List<int> { 13, 63, 73 });
        var card = updated.enemySinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList.Find(o => o.cardInfo.infoId == 1);
        updated = updated.ChooseCard(card, false);
        updated = updated.ChooseCard(updated.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0], true);
        updated = updated.ChooseCard(updated.enemySinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0], false);
        updated = updated.ChooseHornUtilArea(CardBadgeType.Wood, false);
        updated = updated.ChooseCard(updated.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0], true);
        card = updated.enemySinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0];
        updated = updated.ChooseCard(card, false);
        updated = updated with {
            actionEventList = ImmutableList<ActionEvent>.Empty
        };
        updated = updated.ChooseHornUtilArea(CardBadgeType.Wood, false);
        Assert.AreEqual(GameState.State.WAIT_SELF_ACTION, updated.gameState.curState);
        Assert.AreEqual(GameState.ActionState.None, updated.gameState.actionState);
        Assert.AreEqual(10, updated.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(10, updated.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(1, updated.enemySinglePlayerAreaModel.battleRowAreaList[(int)CardBadgeType.Wood].hornCardListModel.cardList.Count);
        Assert.AreEqual(card.cardInfo.id, updated.enemySinglePlayerAreaModel.battleRowAreaList[(int)CardBadgeType.Wood].hornCardListModel.cardList[0].cardInfo.id);
        Assert.AreEqual(0, updated.actionEventList.Count);
    }

    // 指导老师 中断
    [Test]
    public void ChooseCard_HornUtil_Interrupt_Self()
    {
        SetHostFirst(true);
        AppendAllCardInfoList(new List<CardInfo> {
            new CardInfo { chineseName = "指导老师牌", infoId = 6, ability = CardAbility.HornUtil },
        });
        var model = new WholeAreaModel(true, "S", "E", CardGroup.KumikoFirstYear);
        var updated = model.SelfInit(new List<int> { 6 })
            .EnemyInit(new List<int> { 1 }, new List<int> { 13 }, CardGroup.KumikoThirdYear, false)
            .SelfDrawInitHandCard()
            .SelfReDrawInitHandCard()
            .EnemyDrawInitHandCard(new List<int> { 13 });
        updated = updated.ChooseCard(updated.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0], true);
        updated = updated with {
            actionEventList = ImmutableList<ActionEvent>.Empty
        };
        updated = updated.InterruptAction(true);
        Assert.AreEqual(GameState.State.WAIT_ENEMY_ACTION, updated.gameState.curState);
        Assert.AreEqual(GameState.ActionState.None, updated.gameState.actionState);
        Assert.AreEqual(0, updated.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(0, updated.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(2, updated.actionEventList.Count);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[0].type);
        Assert.AreEqual("<color=green>S</color> 未选择目标\n", updated.actionEventList[0].args[0]);
        Assert.AreEqual(ActionEvent.Type.BattleMsg, updated.actionEventList[1].type);
        Assert.AreEqual(BattleModel.ActionType.InterruptAction, updated.actionEventList[1].args[0]);
    }

    [Test]
    public void ChooseCard_HornUtil_Interrupt_Enemy()
    {
        SetHostFirst(false);
        AppendAllCardInfoList(new List<CardInfo> {
            new CardInfo { chineseName = "指导老师牌", infoId = 6, ability = CardAbility.HornUtil },
        });
        var model = new WholeAreaModel(true, "S", "E", CardGroup.KumikoFirstYear);
        var updated = model.SelfInit(new List<int> { 1 })
            .EnemyInit(new List<int> { 6 }, new List<int> { 63 }, CardGroup.KumikoThirdYear, false)
            .SelfDrawInitHandCard()
            .SelfReDrawInitHandCard()
            .EnemyDrawInitHandCard(new List<int> { 63 });
        updated = updated.ChooseCard(updated.enemySinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0], false);
        updated = updated with {
            actionEventList = ImmutableList<ActionEvent>.Empty
        };
        updated = updated.InterruptAction(false);
        Assert.AreEqual(GameState.State.WAIT_SELF_ACTION, updated.gameState.curState);
        Assert.AreEqual(GameState.ActionState.None, updated.gameState.actionState);
        Assert.AreEqual(0, updated.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(0, updated.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(1, updated.actionEventList.Count);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[0].type);
        Assert.AreEqual("<color=red>E</color> 未选择目标\n", updated.actionEventList[0].args[0]);
    }

    // lip 无目标
    [Test]
    public void ChooseCard_Lip_NoTarget_Self()
    {
        SetHostFirst(true);
        AppendAllCardInfoList(new List<CardInfo> {
            new CardInfo { chineseName = "迷唇牌", infoId = 6, originPower = 5, ability = CardAbility.Lip, badgeType = CardBadgeType.Wood },
        });
        var model = new WholeAreaModel(true, "S", "E", CardGroup.KumikoFirstYear);
        var updated = model.SelfInit(new List<int> { 6 })
            .EnemyInit(new List<int> { 1 }, new List<int> { 13 }, CardGroup.KumikoThirdYear, false)
            .SelfDrawInitHandCard()
            .SelfReDrawInitHandCard()
            .EnemyDrawInitHandCard(new List<int> { 13 });
        updated = updated with {
            actionEventList = ImmutableList<ActionEvent>.Empty
        };
        var card = updated.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0];
        updated = updated.ChooseCard(card, true);
        Assert.AreEqual(GameState.State.WAIT_ENEMY_ACTION, updated.gameState.curState);
        Assert.AreEqual(GameState.ActionState.None, updated.gameState.actionState);
        Assert.AreEqual(5, updated.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(0, updated.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(2, updated.actionEventList.Count);
        Assert.AreEqual(ActionEvent.Type.BattleMsg, updated.actionEventList[0].type);
        Assert.AreEqual(BattleModel.ActionType.ChooseCard, updated.actionEventList[0].args[0]);
        Assert.AreEqual(card.cardInfo.id, updated.actionEventList[0].args[1]);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[1].type);
        Assert.AreEqual("<color=green>S</color> 打出卡牌：<b>迷唇牌</b>\n", updated.actionEventList[1].args[0]);
    }

    [Test]
    public void ChooseCard_Lip_NoTarget_Enemy()
    {
        SetHostFirst(false);
        AppendAllCardInfoList(new List<CardInfo> {
            new CardInfo { chineseName = "迷唇牌", infoId = 6, originPower = 5, ability = CardAbility.Lip, badgeType = CardBadgeType.Wood },
        });
        var model = new WholeAreaModel(true, "S", "E", CardGroup.KumikoFirstYear);
        var updated = model.SelfInit(new List<int> { 1 })
            .EnemyInit(new List<int> { 6 }, new List<int> { 63 }, CardGroup.KumikoThirdYear, false)
            .SelfDrawInitHandCard()
            .SelfReDrawInitHandCard()
            .EnemyDrawInitHandCard(new List<int> { 63 });
        updated = updated with {
            actionEventList = ImmutableList<ActionEvent>.Empty
        };
        var card = updated.enemySinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0];
        updated = updated.ChooseCard(card, false);
        Assert.AreEqual(GameState.State.WAIT_SELF_ACTION, updated.gameState.curState);
        Assert.AreEqual(GameState.ActionState.None, updated.gameState.actionState);
        Assert.AreEqual(0, updated.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(5, updated.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(1, updated.actionEventList.Count);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[0].type);
        Assert.AreEqual("<color=red>E</color> 打出卡牌：<b>迷唇牌</b>\n", updated.actionEventList[0].args[0]);
    }

    // lip 攻击
    [Test]
    public void ChooseCard_Lip_Attack_Self()
    {
        SetHostFirst(false);
        AppendAllCardInfoList(new List<CardInfo> {
            new CardInfo { chineseName = "迷唇牌", infoId = 6, originPower = 5, ability = CardAbility.Lip, badgeType = CardBadgeType.Wood },
            new CardInfo { chineseName = "普通牌-5-男", infoId = 7, originPower = 5, ability = CardAbility.None, badgeType = CardBadgeType.Wood, isMale = true },
        });
        var model = new WholeAreaModel(true, "S", "E", CardGroup.KumikoFirstYear);
        var updated = model.SelfInit(new List<int> { 6 })
            .EnemyInit(new List<int> { 7 }, new List<int> { 73 }, CardGroup.KumikoThirdYear, false)
            .SelfDrawInitHandCard()
            .SelfReDrawInitHandCard()
            .EnemyDrawInitHandCard(new List<int> { 73 });
        updated = updated.ChooseCard(updated.enemySinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0], false);
        updated = updated with {
            actionEventList = ImmutableList<ActionEvent>.Empty
        };
        var card = updated.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0];
        updated = updated.ChooseCard(card, true);
        Assert.AreEqual(GameState.State.WAIT_ENEMY_ACTION, updated.gameState.curState);
        Assert.AreEqual(GameState.ActionState.None, updated.gameState.actionState);
        Assert.AreEqual(5, updated.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(3, updated.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(4, updated.actionEventList.Count);
        Assert.AreEqual(ActionEvent.Type.BattleMsg, updated.actionEventList[0].type);
        Assert.AreEqual(BattleModel.ActionType.ChooseCard, updated.actionEventList[0].args[0]);
        Assert.AreEqual(card.cardInfo.id, updated.actionEventList[0].args[1]);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[1].type);
        Assert.AreEqual("<color=green>S</color> 打出卡牌：<b>迷唇牌</b>\n", updated.actionEventList[1].args[0]);
        Assert.AreEqual(ActionEvent.Type.Sfx, updated.actionEventList[2].type);
        Assert.AreEqual(AudioManager.SFXType.Attack, updated.actionEventList[2].args[0]);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[3].type);
        Assert.AreEqual("施放<b>迷唇</b>技能，攻击卡牌：<b>普通牌-5-男</b>\n", updated.actionEventList[3].args[0]);
    }

    [Test]
    public void ChooseCard_Lip_Attack_Enemy()
    {
        SetHostFirst(true);
        AppendAllCardInfoList(new List<CardInfo> {
            new CardInfo { chineseName = "迷唇牌", infoId = 6, originPower = 5, ability = CardAbility.Lip, badgeType = CardBadgeType.Wood },
            new CardInfo { chineseName = "普通牌-5-男", infoId = 7, originPower = 5, ability = CardAbility.None, badgeType = CardBadgeType.Wood, isMale = true },
        });
        var model = new WholeAreaModel(true, "S", "E", CardGroup.KumikoFirstYear);
        var updated = model.SelfInit(new List<int> { 7 })
            .EnemyInit(new List<int> { 6 }, new List<int> { 63 }, CardGroup.KumikoThirdYear, false)
            .SelfDrawInitHandCard()
            .SelfReDrawInitHandCard()
            .EnemyDrawInitHandCard(new List<int> { 63 });
        updated = updated.ChooseCard(updated.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0], true);
        updated = updated with {
            actionEventList = ImmutableList<ActionEvent>.Empty
        };
        var card = updated.enemySinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0];
        updated = updated.ChooseCard(card, false);
        Assert.AreEqual(GameState.State.WAIT_SELF_ACTION, updated.gameState.curState);
        Assert.AreEqual(GameState.ActionState.None, updated.gameState.actionState);
        Assert.AreEqual(3, updated.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(5, updated.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(3, updated.actionEventList.Count);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[0].type);
        Assert.AreEqual("<color=red>E</color> 打出卡牌：<b>迷唇牌</b>\n", updated.actionEventList[0].args[0]);
        Assert.AreEqual(ActionEvent.Type.Sfx, updated.actionEventList[1].type);
        Assert.AreEqual(AudioManager.SFXType.Attack, updated.actionEventList[1].args[0]);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[2].type);
        Assert.AreEqual("施放<b>迷唇</b>技能，攻击卡牌：<b>普通牌-5-男</b>\n", updated.actionEventList[2].args[0]);
    }

    // lip 使卡牌退部
    [Test]
    public void ChooseCard_Lip_Dead_Self()
    {
        SetHostFirst(false);
        AppendAllCardInfoList(new List<CardInfo> {
            new CardInfo { chineseName = "迷唇牌", infoId = 6, originPower = 5, ability = CardAbility.Lip, badgeType = CardBadgeType.Wood },
            new CardInfo { chineseName = "普通牌-2-男", infoId = 7, originPower = 2, ability = CardAbility.None, badgeType = CardBadgeType.Wood, isMale = true },
        });
        var model = new WholeAreaModel(true, "S", "E", CardGroup.KumikoFirstYear);
        var updated = model.SelfInit(new List<int> { 6 })
            .EnemyInit(new List<int> { 7 }, new List<int> { 73 }, CardGroup.KumikoThirdYear, false)
            .SelfDrawInitHandCard()
            .SelfReDrawInitHandCard()
            .EnemyDrawInitHandCard(new List<int> { 73 });
        updated = updated.ChooseCard(updated.enemySinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0], false);
        updated = updated with {
            actionEventList = ImmutableList<ActionEvent>.Empty
        };
        var card = updated.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0];
        updated = updated.ChooseCard(card, true);
        Assert.AreEqual(GameState.State.WAIT_ENEMY_ACTION, updated.gameState.curState);
        Assert.AreEqual(GameState.ActionState.None, updated.gameState.actionState);
        Assert.AreEqual(5, updated.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(0, updated.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(1, updated.enemySinglePlayerAreaModel.discardAreaModel.cardListModel.cardList.Count);
        Assert.AreEqual(4, updated.actionEventList.Count);
        Assert.AreEqual(ActionEvent.Type.BattleMsg, updated.actionEventList[0].type);
        Assert.AreEqual(BattleModel.ActionType.ChooseCard, updated.actionEventList[0].args[0]);
        Assert.AreEqual(card.cardInfo.id, updated.actionEventList[0].args[1]);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[1].type);
        Assert.AreEqual("<color=green>S</color> 打出卡牌：<b>迷唇牌</b>\n", updated.actionEventList[1].args[0]);
        Assert.AreEqual(ActionEvent.Type.Sfx, updated.actionEventList[2].type);
        Assert.AreEqual(AudioManager.SFXType.Scorch, updated.actionEventList[2].args[0]);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[3].type);
        Assert.AreEqual("施放<b>迷唇</b>技能，移除卡牌：<b>普通牌-2-男</b>\n", updated.actionEventList[3].args[0]);
    }

    [Test]
    public void ChooseCard_Lip_Dead_Enemy()
    {
        SetHostFirst(true);
        AppendAllCardInfoList(new List<CardInfo> {
            new CardInfo { chineseName = "迷唇牌", infoId = 6, originPower = 5, ability = CardAbility.Lip, badgeType = CardBadgeType.Wood },
            new CardInfo { chineseName = "普通牌-2-男", infoId = 7, originPower = 2, ability = CardAbility.None, badgeType = CardBadgeType.Wood, isMale = true },
        });
        var model = new WholeAreaModel(true, "S", "E", CardGroup.KumikoFirstYear);
        var updated = model.SelfInit(new List<int> { 7 })
            .EnemyInit(new List<int> { 6 }, new List<int> { 63 }, CardGroup.KumikoThirdYear, false)
            .SelfDrawInitHandCard()
            .SelfReDrawInitHandCard()
            .EnemyDrawInitHandCard(new List<int> { 63 });
        updated = updated.ChooseCard(updated.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0], true);
        updated = updated with {
            actionEventList = ImmutableList<ActionEvent>.Empty
        };
        var card = updated.enemySinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0];
        updated = updated.ChooseCard(card, false);
        Assert.AreEqual(GameState.State.WAIT_SELF_ACTION, updated.gameState.curState);
        Assert.AreEqual(GameState.ActionState.None, updated.gameState.actionState);
        Assert.AreEqual(0, updated.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(5, updated.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(1, updated.selfSinglePlayerAreaModel.discardAreaModel.cardListModel.cardList.Count);
        Assert.AreEqual(3, updated.actionEventList.Count);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[0].type);
        Assert.AreEqual("<color=red>E</color> 打出卡牌：<b>迷唇牌</b>\n", updated.actionEventList[0].args[0]);
        Assert.AreEqual(ActionEvent.Type.Sfx, updated.actionEventList[1].type);
        Assert.AreEqual(AudioManager.SFXType.Scorch, updated.actionEventList[1].args[0]);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[2].type);
        Assert.AreEqual("施放<b>迷唇</b>技能，移除卡牌：<b>普通牌-2-男</b>\n", updated.actionEventList[2].args[0]);
    }

    // power first 无目标
    [Test]
    public void ChooseCard_PowerFirst_NoTarget_Self()
    {
        SetHostFirst(false);
        AppendAllCardInfoList(new List<CardInfo> {
            new CardInfo { chineseName = "实力至上牌", infoId = 6, ability = CardAbility.PowerFirst, cardType = CardType.Leader },
        });
        var model = new WholeAreaModel(true, "S", "E", CardGroup.KumikoFirstYear);
        var updated = model.SelfInit(new List<int> { 1, 6 })
            .EnemyInit(new List<int> { 1 }, new List<int> { 13 }, CardGroup.KumikoThirdYear, false)
            .SelfDrawInitHandCard()
            .SelfReDrawInitHandCard()
            .EnemyDrawInitHandCard(new List<int> { 13 });
        updated = updated.ChooseCard(updated.enemySinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0], false);
        updated = updated with {
            actionEventList = ImmutableList<ActionEvent>.Empty
        };
        var card = updated.selfSinglePlayerAreaModel.handCardAreaModel.leaderCardListModel.cardList[0];
        updated = updated.ChooseCard(card, true);
        Assert.AreEqual(GameState.State.WAIT_ENEMY_ACTION, updated.gameState.curState);
        Assert.AreEqual(GameState.ActionState.None, updated.gameState.actionState);
        Assert.AreEqual(0, updated.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(5, updated.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(2, updated.actionEventList.Count);
        Assert.AreEqual(ActionEvent.Type.BattleMsg, updated.actionEventList[0].type);
        Assert.AreEqual(BattleModel.ActionType.ChooseCard, updated.actionEventList[0].args[0]);
        Assert.AreEqual(card.cardInfo.id, updated.actionEventList[0].args[1]);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[1].type);
        Assert.AreEqual("<color=green>S</color> 打出卡牌：<b>实力至上牌</b>\n", updated.actionEventList[1].args[0]);
    }

    [Test]
    public void ChooseCard_PowerFirst_NoTarget_Enemy()
    {
        SetHostFirst(true);
        AppendAllCardInfoList(new List<CardInfo> {
            new CardInfo { chineseName = "实力至上牌", infoId = 6, ability = CardAbility.PowerFirst, cardType = CardType.Leader },
        });
        var model = new WholeAreaModel(true, "S", "E", CardGroup.KumikoFirstYear);
        var updated = model.SelfInit(new List<int> { 1 })
            .EnemyInit(new List<int> { 1, 6 }, new List<int> { 13, 63 }, CardGroup.KumikoThirdYear, false)
            .SelfDrawInitHandCard()
            .SelfReDrawInitHandCard()
            .EnemyDrawInitHandCard(new List<int> { 13 });
        updated = updated.ChooseCard(updated.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0], true);
        updated = updated with {
            actionEventList = ImmutableList<ActionEvent>.Empty
        };
        var card = updated.enemySinglePlayerAreaModel.handCardAreaModel.leaderCardListModel.cardList[0];
        updated = updated.ChooseCard(card, false);
        Assert.AreEqual(GameState.State.WAIT_SELF_ACTION, updated.gameState.curState);
        Assert.AreEqual(GameState.ActionState.None, updated.gameState.actionState);
        Assert.AreEqual(5, updated.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(0, updated.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(1, updated.actionEventList.Count);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[0].type);
        Assert.AreEqual("<color=red>E</color> 打出卡牌：<b>实力至上牌</b>\n", updated.actionEventList[0].args[0]);
    }

    // power first 攻击
    [Test]
    public void ChooseCard_PowerFirst_Attack_Self()
    {
        SetHostFirst(false);
        AppendAllCardInfoList(new List<CardInfo> {
            new CardInfo { chineseName = "实力至上牌", infoId = 6, ability = CardAbility.PowerFirst, cardType = CardType.Leader },
        });
        var model = new WholeAreaModel(true, "S", "E", CardGroup.KumikoFirstYear);
        var updated = model.SelfInit(new List<int> { 1, 6 })
            .EnemyInit(new List<int> { 2 }, new List<int> { 23 }, CardGroup.KumikoThirdYear, false)
            .SelfDrawInitHandCard()
            .SelfReDrawInitHandCard()
            .EnemyDrawInitHandCard(new List<int> { 23 });
        updated = updated.ChooseCard(updated.enemySinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0], false);
        updated = updated with {
            actionEventList = ImmutableList<ActionEvent>.Empty
        };
        var card = updated.selfSinglePlayerAreaModel.handCardAreaModel.leaderCardListModel.cardList[0];
        updated = updated.ChooseCard(card, true);
        Assert.AreEqual(GameState.State.WAIT_ENEMY_ACTION, updated.gameState.curState);
        Assert.AreEqual(GameState.ActionState.None, updated.gameState.actionState);
        Assert.AreEqual(0, updated.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(2, updated.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(4, updated.actionEventList.Count);
        Assert.AreEqual(ActionEvent.Type.BattleMsg, updated.actionEventList[0].type);
        Assert.AreEqual(BattleModel.ActionType.ChooseCard, updated.actionEventList[0].args[0]);
        Assert.AreEqual(card.cardInfo.id, updated.actionEventList[0].args[1]);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[1].type);
        Assert.AreEqual("<color=green>S</color> 打出卡牌：<b>实力至上牌</b>\n", updated.actionEventList[1].args[0]);
        Assert.AreEqual(ActionEvent.Type.Sfx, updated.actionEventList[2].type);
        Assert.AreEqual(AudioManager.SFXType.Attack, updated.actionEventList[2].args[0]);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[3].type);
        Assert.AreEqual("施放<b>实力至上</b>技能，攻击卡牌：<b>普通牌-3</b>\n", updated.actionEventList[3].args[0]);
    }

    [Test]
    public void ChooseCard_PowerFirst_Attack_Enemy()
    {
        SetHostFirst(true);
        AppendAllCardInfoList(new List<CardInfo> {
            new CardInfo { chineseName = "实力至上牌", infoId = 6, ability = CardAbility.PowerFirst, cardType = CardType.Leader },
        });
        var model = new WholeAreaModel(true, "S", "E", CardGroup.KumikoFirstYear);
        var updated = model.SelfInit(new List<int> { 2 })
            .EnemyInit(new List<int> { 1, 6 }, new List<int> { 13, 63 }, CardGroup.KumikoThirdYear, false)
            .SelfDrawInitHandCard()
            .SelfReDrawInitHandCard()
            .EnemyDrawInitHandCard(new List<int> { 13 });
        updated = updated.ChooseCard(updated.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0], true);
        updated = updated with {
            actionEventList = ImmutableList<ActionEvent>.Empty
        };
        var card = updated.enemySinglePlayerAreaModel.handCardAreaModel.leaderCardListModel.cardList[0];
        updated = updated.ChooseCard(card, false);
        Assert.AreEqual(GameState.State.WAIT_SELF_ACTION, updated.gameState.curState);
        Assert.AreEqual(GameState.ActionState.None, updated.gameState.actionState);
        Assert.AreEqual(2, updated.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(0, updated.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(3, updated.actionEventList.Count);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[0].type);
        Assert.AreEqual("<color=red>E</color> 打出卡牌：<b>实力至上牌</b>\n", updated.actionEventList[0].args[0]);
        Assert.AreEqual(ActionEvent.Type.Sfx, updated.actionEventList[1].type);
        Assert.AreEqual(AudioManager.SFXType.Attack, updated.actionEventList[1].args[0]);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[2].type);
        Assert.AreEqual("施放<b>实力至上</b>技能，攻击卡牌：<b>普通牌-3</b>\n", updated.actionEventList[2].args[0]);
    }

    // power first 退部
    [Test]
    public void ChooseCard_PowerFirst_Dead_Self()
    {
        SetHostFirst(false);
        AppendAllCardInfoList(new List<CardInfo> {
            new CardInfo { chineseName = "实力至上牌", infoId = 6, ability = CardAbility.PowerFirst, cardType = CardType.Leader },
            new CardInfo { chineseName = "普通牌-1", infoId = 7, originPower = 1, cardType = CardType.Normal, badgeType = CardBadgeType.Brass },
        });
        var model = new WholeAreaModel(true, "S", "E", CardGroup.KumikoFirstYear);
        var updated = model.SelfInit(new List<int> { 1, 6 })
            .EnemyInit(new List<int> { 7 }, new List<int> { 73 }, CardGroup.KumikoThirdYear, false)
            .SelfDrawInitHandCard()
            .SelfReDrawInitHandCard()
            .EnemyDrawInitHandCard(new List<int> { 73 });
        updated = updated.ChooseCard(updated.enemySinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0], false);
        updated = updated with {
            actionEventList = ImmutableList<ActionEvent>.Empty
        };
        var card = updated.selfSinglePlayerAreaModel.handCardAreaModel.leaderCardListModel.cardList[0];
        updated = updated.ChooseCard(card, true);
        Assert.AreEqual(GameState.State.WAIT_ENEMY_ACTION, updated.gameState.curState);
        Assert.AreEqual(GameState.ActionState.None, updated.gameState.actionState);
        Assert.AreEqual(0, updated.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(0, updated.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(4, updated.actionEventList.Count);
        Assert.AreEqual(ActionEvent.Type.BattleMsg, updated.actionEventList[0].type);
        Assert.AreEqual(BattleModel.ActionType.ChooseCard, updated.actionEventList[0].args[0]);
        Assert.AreEqual(card.cardInfo.id, updated.actionEventList[0].args[1]);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[1].type);
        Assert.AreEqual("<color=green>S</color> 打出卡牌：<b>实力至上牌</b>\n", updated.actionEventList[1].args[0]);
        Assert.AreEqual(ActionEvent.Type.Sfx, updated.actionEventList[2].type);
        Assert.AreEqual(AudioManager.SFXType.Scorch, updated.actionEventList[2].args[0]);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[3].type);
        Assert.AreEqual("施放<b>实力至上</b>技能，移除卡牌：<b>普通牌-1</b>\n", updated.actionEventList[3].args[0]);
    }

    [Test]
    public void ChooseCard_PowerFirst_Dead_Enemy()
    {
        SetHostFirst(true);
        AppendAllCardInfoList(new List<CardInfo> {
            new CardInfo { chineseName = "实力至上牌", infoId = 6, ability = CardAbility.PowerFirst, cardType = CardType.Leader },
            new CardInfo { chineseName = "普通牌-1", infoId = 7, originPower = 1, cardType = CardType.Normal, badgeType = CardBadgeType.Brass },
        });
        var model = new WholeAreaModel(true, "S", "E", CardGroup.KumikoFirstYear);
        var updated = model.SelfInit(new List<int> { 7 })
            .EnemyInit(new List<int> { 1, 6 }, new List<int> { 13, 63 }, CardGroup.KumikoThirdYear, false)
            .SelfDrawInitHandCard()
            .SelfReDrawInitHandCard()
            .EnemyDrawInitHandCard(new List<int> { 13 });
        updated = updated.ChooseCard(updated.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0], true);
        updated = updated with {
            actionEventList = ImmutableList<ActionEvent>.Empty
        };
        var card = updated.enemySinglePlayerAreaModel.handCardAreaModel.leaderCardListModel.cardList[0];
        updated = updated.ChooseCard(card, false);
        Assert.AreEqual(GameState.State.WAIT_SELF_ACTION, updated.gameState.curState);
        Assert.AreEqual(GameState.ActionState.None, updated.gameState.actionState);
        Assert.AreEqual(0, updated.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(0, updated.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(3, updated.actionEventList.Count);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[0].type);
        Assert.AreEqual("<color=red>E</color> 打出卡牌：<b>实力至上牌</b>\n", updated.actionEventList[0].args[0]);
        Assert.AreEqual(ActionEvent.Type.Sfx, updated.actionEventList[1].type);
        Assert.AreEqual(AudioManager.SFXType.Scorch, updated.actionEventList[1].args[0]);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[2].type);
        Assert.AreEqual("施放<b>实力至上</b>技能，移除卡牌：<b>普通牌-1</b>\n", updated.actionEventList[2].args[0]);
    }

    // 守卫，无守卫目标牌
    [Test]
    public void ChooseCard_Guard_NoRelatedCard_Self()
    {
        SetHostFirst(true);
        AppendAllCardInfoList(new List<CardInfo> {
            new CardInfo { chineseName = "守卫牌", infoId = 6, originPower = 5, ability = CardAbility.Guard, badgeType = CardBadgeType.Wood, relatedCard = "守卫目标" },
        });
        var model = new WholeAreaModel(true, "S", "E", CardGroup.KumikoFirstYear);
        var updated = model.SelfInit(new List<int> { 6 })
            .EnemyInit(new List<int> { 1 }, new List<int> { 13 }, CardGroup.KumikoThirdYear, false)
            .SelfDrawInitHandCard()
            .SelfReDrawInitHandCard()
            .EnemyDrawInitHandCard(new List<int> { 13 });
        updated = updated with {
            actionEventList = ImmutableList<ActionEvent>.Empty
        };
        updated = updated.ChooseCard(updated.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0], true);
        Assert.AreEqual(GameState.State.WAIT_ENEMY_ACTION, updated.gameState.curState);
        Assert.AreEqual(GameState.ActionState.None, updated.gameState.actionState);
        Assert.AreEqual(5, updated.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(0, updated.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(3, updated.actionEventList.Count);
        Assert.AreEqual(ActionEvent.Type.BattleMsg, updated.actionEventList[0].type);
        Assert.AreEqual(BattleModel.ActionType.ChooseCard, updated.actionEventList[0].args[0]);
        Assert.AreEqual(11, updated.actionEventList[0].args[1]);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[1].type);
        Assert.AreEqual("<color=green>S</color> 打出卡牌：<b>守卫牌</b>\n", updated.actionEventList[1].args[0]);
        Assert.AreEqual(ActionEvent.Type.Toast, updated.actionEventList[2].type);
        Assert.AreEqual("守卫目标不在场上，无法攻击", updated.actionEventList[2].args[0]);
    }

    [Test]
    public void ChooseCard_Guard_NoRelatedCard_Enemy()
    {
        SetHostFirst(false);
        AppendAllCardInfoList(new List<CardInfo> {
            new CardInfo { chineseName = "守卫牌", infoId = 6, originPower = 5, ability = CardAbility.Guard, badgeType = CardBadgeType.Wood, relatedCard = "守卫目标" },
        });
        var model = new WholeAreaModel(true, "S", "E", CardGroup.KumikoFirstYear);
        var updated = model.SelfInit(new List<int> { 1 })
            .EnemyInit(new List<int> { 6 }, new List<int> { 63 }, CardGroup.KumikoThirdYear, false)
            .SelfDrawInitHandCard()
            .SelfReDrawInitHandCard()
            .EnemyDrawInitHandCard(new List<int> { 63 });
        updated = updated with {
            actionEventList = ImmutableList<ActionEvent>.Empty
        };
        updated = updated.ChooseCard(updated.enemySinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0], false);
        Assert.AreEqual(GameState.State.WAIT_SELF_ACTION, updated.gameState.curState);
        Assert.AreEqual(GameState.ActionState.None, updated.gameState.actionState);
        Assert.AreEqual(0, updated.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(5, updated.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(1, updated.actionEventList.Count);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[0].type);
        Assert.AreEqual("<color=red>E</color> 打出卡牌：<b>守卫牌</b>\n", updated.actionEventList[0].args[0]);
    }

    // 守卫，细节同attack，因此不重复写测试了
    [Test]
    public void ChooseCard_Guard_Normal_Self()
    {
        SetHostFirst(false);
        AppendAllCardInfoList(new List<CardInfo> {
            new CardInfo { chineseName = "守卫牌", infoId = 6, originPower = 5, ability = CardAbility.Guard, badgeType = CardBadgeType.Wood, relatedCard = "守卫目标" },
            new CardInfo { chineseName = "守卫目标", infoId = 7, originPower = 5, cardType = CardType.Normal, badgeType = CardBadgeType.Wood },
        });
        var model = new WholeAreaModel(true, "S", "E", CardGroup.KumikoFirstYear);
        var updated = model.SelfInit(new List<int> { 6 })
            .EnemyInit(new List<int> { 7 }, new List<int> { 73 }, CardGroup.KumikoThirdYear, false)
            .SelfDrawInitHandCard()
            .SelfReDrawInitHandCard()
            .EnemyDrawInitHandCard(new List<int> { 73 });
        updated = updated.ChooseCard(updated.enemySinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0], false);
        updated = updated with {
            actionEventList = ImmutableList<ActionEvent>.Empty
        };
        var card = updated.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0];
        updated = updated.ChooseCard(card, true);
        Assert.AreEqual(GameState.State.WAIT_SELF_ACTION, updated.gameState.curState);
        Assert.AreEqual(GameState.ActionState.ATTACKING, updated.gameState.actionState);
        Assert.AreEqual(5, updated.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(5, updated.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(3, updated.actionEventList.Count);
        Assert.AreEqual(ActionEvent.Type.BattleMsg, updated.actionEventList[0].type);
        Assert.AreEqual(BattleModel.ActionType.ChooseCard, updated.actionEventList[0].args[0]);
        Assert.AreEqual(card.cardInfo.id, updated.actionEventList[0].args[1]);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[1].type);
        Assert.AreEqual("<color=green>S</color> 打出卡牌：<b>守卫牌</b>\n", updated.actionEventList[1].args[0]);
        Assert.AreEqual(ActionEvent.Type.Toast, updated.actionEventList[2].type);
        Assert.AreEqual("请选择攻击目标", updated.actionEventList[2].args[0]);
        updated = updated with {
            actionEventList = ImmutableList<ActionEvent>.Empty
        };
        card = updated.enemySinglePlayerAreaModel.battleRowAreaList[(int)CardBadgeType.Wood].cardListModel.cardList[0];
        updated = updated.ChooseCard(card, true);
        Assert.AreEqual(GameState.State.WAIT_ENEMY_ACTION, updated.gameState.curState);
        Assert.AreEqual(GameState.ActionState.None, updated.gameState.actionState);
        Assert.AreEqual(5, updated.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(1, updated.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(3, updated.actionEventList.Count);
        Assert.AreEqual(ActionEvent.Type.BattleMsg, updated.actionEventList[0].type);
        Assert.AreEqual(BattleModel.ActionType.ChooseCard, updated.actionEventList[0].args[0]);
        Assert.AreEqual(card.cardInfo.id, updated.actionEventList[0].args[1]);
        Assert.AreEqual(ActionEvent.Type.Sfx, updated.actionEventList[1].type);
        Assert.AreEqual(AudioManager.SFXType.Attack, updated.actionEventList[1].args[0]);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[2].type);
        Assert.AreEqual("施放<b>卫队</b>技能，攻击卡牌：<b>守卫目标</b>\n", updated.actionEventList[2].args[0]);
    }

    [Test]
    public void ChooseCard_Guard_Normal_Enemy()
    {
        SetHostFirst(true);
        AppendAllCardInfoList(new List<CardInfo> {
            new CardInfo { chineseName = "守卫牌", infoId = 6, originPower = 5, ability = CardAbility.Guard, badgeType = CardBadgeType.Wood, relatedCard = "守卫目标" },
            new CardInfo { chineseName = "守卫目标", infoId = 7, originPower = 5, cardType = CardType.Normal, badgeType = CardBadgeType.Wood },
        });
        var model = new WholeAreaModel(true, "S", "E", CardGroup.KumikoFirstYear);
        var updated = model.SelfInit(new List<int> { 7 })
            .EnemyInit(new List<int> { 6 }, new List<int> { 63 }, CardGroup.KumikoThirdYear, false)
            .SelfDrawInitHandCard()
            .SelfReDrawInitHandCard()
            .EnemyDrawInitHandCard(new List<int> { 63 });
        updated = updated.ChooseCard(updated.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0], true);
        updated = updated with {
            actionEventList = ImmutableList<ActionEvent>.Empty
        };
        var card = updated.enemySinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0];
        updated = updated.ChooseCard(card, false);
        Assert.AreEqual(GameState.State.WAIT_ENEMY_ACTION, updated.gameState.curState);
        Assert.AreEqual(GameState.ActionState.ATTACKING, updated.gameState.actionState);
        Assert.AreEqual(5, updated.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(5, updated.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(1, updated.actionEventList.Count);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[0].type);
        Assert.AreEqual("<color=red>E</color> 打出卡牌：<b>守卫牌</b>\n", updated.actionEventList[0].args[0]);
        updated = updated with {
            actionEventList = ImmutableList<ActionEvent>.Empty
        };
        card = updated.selfSinglePlayerAreaModel.battleRowAreaList[(int)CardBadgeType.Wood].cardListModel.cardList[0];
        updated = updated.ChooseCard(card, false);
        Assert.AreEqual(GameState.State.WAIT_SELF_ACTION, updated.gameState.curState);
        Assert.AreEqual(GameState.ActionState.None, updated.gameState.actionState);
        Assert.AreEqual(1, updated.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(5, updated.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(2, updated.actionEventList.Count);
        Assert.AreEqual(ActionEvent.Type.Sfx, updated.actionEventList[0].type);
        Assert.AreEqual(AudioManager.SFXType.Attack, updated.actionEventList[0].args[0]);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[1].type);
        Assert.AreEqual("施放<b>卫队</b>技能，攻击卡牌：<b>守卫目标</b>\n", updated.actionEventList[1].args[0]);
    }

    // monaka，无目标
    [Test]
    public void ChooseCard_Monaka_NoTarget_Self()
    {
        SetHostFirst(true);
        AppendAllCardInfoList(new List<CardInfo> {
            new CardInfo { chineseName = "Monaka牌", infoId = 6, originPower = 5, ability = CardAbility.Monaka, badgeType = CardBadgeType.Wood },
        });
        var model = new WholeAreaModel(true, "S", "E", CardGroup.KumikoFirstYear);
        var updated = model.SelfInit(new List<int> { 6 })
            .EnemyInit(new List<int> { 1 }, new List<int> { 13 }, CardGroup.KumikoThirdYear, false)
            .SelfDrawInitHandCard()
            .SelfReDrawInitHandCard()
            .EnemyDrawInitHandCard(new List<int> { 13 });
        updated = updated with {
            actionEventList = ImmutableList<ActionEvent>.Empty
        };
        updated = updated.ChooseCard(updated.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0], true);
        Assert.AreEqual(GameState.State.WAIT_ENEMY_ACTION, updated.gameState.curState);
        Assert.AreEqual(GameState.ActionState.None, updated.gameState.actionState);
        Assert.AreEqual(5, updated.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(0, updated.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(3, updated.actionEventList.Count);
        Assert.AreEqual(ActionEvent.Type.BattleMsg, updated.actionEventList[0].type);
        Assert.AreEqual(BattleModel.ActionType.ChooseCard, updated.actionEventList[0].args[0]);
        Assert.AreEqual(11, updated.actionEventList[0].args[1]);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[1].type);
        Assert.AreEqual("<color=green>S</color> 打出卡牌：<b>Monaka牌</b>\n", updated.actionEventList[1].args[0]);
        Assert.AreEqual(ActionEvent.Type.Toast, updated.actionEventList[2].type);
        Assert.AreEqual("无可使用monaka的目标", updated.actionEventList[2].args[0]);
    }

    [Test]
    public void ChooseCard_Monaka_NoTarget_Enemy()
    {
        SetHostFirst(false);
        AppendAllCardInfoList(new List<CardInfo> {
            new CardInfo { chineseName = "Monaka牌", infoId = 6, originPower = 5, ability = CardAbility.Monaka, badgeType = CardBadgeType.Wood },
        });
        var model = new WholeAreaModel(true, "S", "E", CardGroup.KumikoFirstYear);
        var updated = model.SelfInit(new List<int> { 1 })
            .EnemyInit(new List<int> { 6 }, new List<int> { 63 }, CardGroup.KumikoThirdYear, false)
            .SelfDrawInitHandCard()
            .SelfReDrawInitHandCard()
            .EnemyDrawInitHandCard(new List<int> { 63 });
        updated = updated with {
            actionEventList = ImmutableList<ActionEvent>.Empty
        };
        updated = updated.ChooseCard(updated.enemySinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0], false);
        Assert.AreEqual(GameState.State.WAIT_SELF_ACTION, updated.gameState.curState);
        Assert.AreEqual(GameState.ActionState.None, updated.gameState.actionState);
        Assert.AreEqual(0, updated.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(5, updated.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(1, updated.actionEventList.Count);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[0].type);
        Assert.AreEqual("<color=red>E</color> 打出卡牌：<b>Monaka牌</b>\n", updated.actionEventList[0].args[0]);
    }

    // monaka
    [Test]
    public void ChooseCard_Monaka_Normal_Self()
    {
        SetHostFirst(true);
        AppendAllCardInfoList(new List<CardInfo> {
            new CardInfo { chineseName = "Monaka牌", infoId = 6, originPower = 5, ability = CardAbility.Monaka, badgeType = CardBadgeType.Wood },
        });
        var model = new WholeAreaModel(true, "S", "E", CardGroup.KumikoFirstYear);
        var updated = model.SelfInit(new List<int> { 1, 6 })
            .EnemyInit(new List<int> { 1 }, new List<int> { 13 }, CardGroup.KumikoThirdYear, false)
            .SelfDrawInitHandCard()
            .SelfReDrawInitHandCard()
            .EnemyDrawInitHandCard(new List<int> { 13 });
        var normalCard = updated.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList.Find(o => o.cardInfo.ability == CardAbility.None);
        updated = updated.ChooseCard(normalCard, true);
        updated = updated.ChooseCard(updated.enemySinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0], false);
        updated = updated with {
            actionEventList = ImmutableList<ActionEvent>.Empty
        };
        var card = updated.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0];
        updated = updated.ChooseCard(card, true);
        Assert.AreEqual(GameState.State.WAIT_SELF_ACTION, updated.gameState.curState);
        Assert.AreEqual(GameState.ActionState.MONAKAING, updated.gameState.actionState);
        Assert.AreEqual(10, updated.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(5, updated.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(3, updated.actionEventList.Count);
        Assert.AreEqual(ActionEvent.Type.BattleMsg, updated.actionEventList[0].type);
        Assert.AreEqual(BattleModel.ActionType.ChooseCard, updated.actionEventList[0].args[0]);
        Assert.AreEqual(card.cardInfo.id, updated.actionEventList[0].args[1]);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[1].type);
        Assert.AreEqual("<color=green>S</color> 打出卡牌：<b>Monaka牌</b>\n", updated.actionEventList[1].args[0]);
        Assert.AreEqual(ActionEvent.Type.Toast, updated.actionEventList[2].type);
        Assert.AreEqual("请选择使用monaka的目标", updated.actionEventList[2].args[0]);
        updated = updated with {
            actionEventList = ImmutableList<ActionEvent>.Empty
        };
        card = updated.selfSinglePlayerAreaModel.battleRowAreaList[(int)CardBadgeType.Wood].cardListModel.cardList[0];
        updated = updated.ChooseCard(card, true);
        Assert.AreEqual(GameState.State.WAIT_ENEMY_ACTION, updated.gameState.curState);
        Assert.AreEqual(GameState.ActionState.None, updated.gameState.actionState);
        Assert.AreEqual(12, updated.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(5, updated.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(2, updated.actionEventList.Count);
        Assert.AreEqual(ActionEvent.Type.BattleMsg, updated.actionEventList[0].type);
        Assert.AreEqual(BattleModel.ActionType.ChooseCard, updated.actionEventList[0].args[0]);
        Assert.AreEqual(card.cardInfo.id, updated.actionEventList[0].args[1]);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[1].type);
        Assert.AreEqual("施放<b>Monaka</b>技能，增益卡牌：<b>普通牌-5</b>\n", updated.actionEventList[1].args[0]);
    }

    [Test]
    public void ChooseCard_Monaka_Normal_Enemy()
    {
        SetHostFirst(false);
        AppendAllCardInfoList(new List<CardInfo> {
            new CardInfo { chineseName = "Monaka牌", infoId = 6, originPower = 5, ability = CardAbility.Monaka, badgeType = CardBadgeType.Wood },
        });
        var model = new WholeAreaModel(true, "S", "E", CardGroup.KumikoFirstYear);
        var updated = model.SelfInit(new List<int> { 1 })
            .EnemyInit(new List<int> { 1, 6 }, new List<int> { 13, 63 }, CardGroup.KumikoThirdYear, false)
            .SelfDrawInitHandCard()
            .SelfReDrawInitHandCard()
            .EnemyDrawInitHandCard(new List<int> { 13, 63 });
        var normalCard = updated.enemySinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList.Find(o => o.cardInfo.ability == CardAbility.None);
        updated = updated.ChooseCard(normalCard, false);
        updated = updated.ChooseCard(updated.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0], true);
        updated = updated with {
            actionEventList = ImmutableList<ActionEvent>.Empty
        };
        var card = updated.enemySinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0];
        updated = updated.ChooseCard(updated.enemySinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0], false);
        Assert.AreEqual(GameState.State.WAIT_ENEMY_ACTION, updated.gameState.curState);
        Assert.AreEqual(GameState.ActionState.MONAKAING, updated.gameState.actionState);
        Assert.AreEqual(5, updated.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(10, updated.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(1, updated.actionEventList.Count);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[0].type);
        Assert.AreEqual("<color=red>E</color> 打出卡牌：<b>Monaka牌</b>\n", updated.actionEventList[0].args[0]);
        updated = updated with {
            actionEventList = ImmutableList<ActionEvent>.Empty
        };
        card = updated.enemySinglePlayerAreaModel.battleRowAreaList[(int)CardBadgeType.Wood].cardListModel.cardList[0];
        updated = updated.ChooseCard(card, false);
        Assert.AreEqual(GameState.State.WAIT_SELF_ACTION, updated.gameState.curState);
        Assert.AreEqual(GameState.ActionState.None, updated.gameState.actionState);
        Assert.AreEqual(5, updated.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(12, updated.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(1, updated.actionEventList.Count);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[0].type);
        Assert.AreEqual("施放<b>Monaka</b>技能，增益卡牌：<b>普通牌-5</b>\n", updated.actionEventList[0].args[0]);
    }

    // Monaka 中断
    [Test]
    public void ChooseCard_Monaka_Interrupt_Self()
    {
        SetHostFirst(true);
        AppendAllCardInfoList(new List<CardInfo> {
            new CardInfo { chineseName = "Monaka牌", infoId = 6, originPower = 5, ability = CardAbility.Monaka, badgeType = CardBadgeType.Wood },
        });
        var model = new WholeAreaModel(true, "S", "E", CardGroup.KumikoFirstYear);
        var updated = model.SelfInit(new List<int> { 2, 6 })
            .EnemyInit(new List<int> { 1 }, new List<int> { 13 }, CardGroup.KumikoThirdYear, false)
            .SelfDrawInitHandCard()
            .SelfReDrawInitHandCard()
            .EnemyDrawInitHandCard(new List<int> { 13 });
        var normalCard = updated.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList.Find(o => o.cardInfo.ability == CardAbility.None);
        updated = updated.ChooseCard(normalCard, true);
        updated = updated.ChooseCard(updated.enemySinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0], false);
        updated = updated.ChooseCard(updated.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0], true);
        updated = updated with {
            actionEventList = ImmutableList<ActionEvent>.Empty
        };
        updated = updated.InterruptAction(true);
        Assert.AreEqual(GameState.State.WAIT_ENEMY_ACTION, updated.gameState.curState);
        Assert.AreEqual(GameState.ActionState.None, updated.gameState.actionState);
        Assert.AreEqual(8, updated.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(5, updated.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(2, updated.actionEventList.Count);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[0].type);
        Assert.AreEqual("<color=green>S</color> 未选择目标\n", updated.actionEventList[0].args[0]);
        Assert.AreEqual(ActionEvent.Type.BattleMsg, updated.actionEventList[1].type);
        Assert.AreEqual(BattleModel.ActionType.InterruptAction, updated.actionEventList[1].args[0]);
    }

    [Test]
    public void ChooseCard_Monaka_Interrupt_Enemy()
    {
        SetHostFirst(false);
        AppendAllCardInfoList(new List<CardInfo> {
            new CardInfo { chineseName = "Monaka牌", infoId = 6, originPower = 5, ability = CardAbility.Monaka, badgeType = CardBadgeType.Wood },
        });
        var model = new WholeAreaModel(true, "S", "E", CardGroup.KumikoFirstYear);
        var updated = model.SelfInit(new List<int> { 1 })
            .EnemyInit(new List<int> { 2, 6 }, new List<int> { 23, 63 }, CardGroup.KumikoThirdYear, false)
            .SelfDrawInitHandCard()
            .SelfReDrawInitHandCard()
            .EnemyDrawInitHandCard(new List<int> { 23, 63 });
        var normalCard = updated.enemySinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList.Find(o => o.cardInfo.ability == CardAbility.None);
        updated = updated.ChooseCard(normalCard, false);
        updated = updated.ChooseCard(updated.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0], true);
        updated = updated.ChooseCard(updated.enemySinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0], false);
        updated = updated with {
            actionEventList = ImmutableList<ActionEvent>.Empty
        };
        updated = updated.InterruptAction(false);
        Assert.AreEqual(GameState.State.WAIT_SELF_ACTION, updated.gameState.curState);
        Assert.AreEqual(GameState.ActionState.None, updated.gameState.actionState);
        Assert.AreEqual(5, updated.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(8, updated.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(1, updated.actionEventList.Count);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[0].type);
        Assert.AreEqual("<color=red>E</color> 未选择目标\n", updated.actionEventList[0].args[0]);
    }

    [Test]
    public void ChooseCard_FindCard()
    {
        SetHostFirst(true);
        var model = new WholeAreaModel(true, "S", "E", CardGroup.KumikoFirstYear);
        var updated = model.SelfInit(allInfoIdList)
            .EnemyInit(allInfoIdList, allIdList, CardGroup.KumikoThirdYear, false)
            .SelfDrawInitHandCard()
            .SelfReDrawInitHandCard()
            .EnemyDrawInitHandCard(new List<int> { 13, 23 });
        updated = updated.ChooseCard(updated.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0], true);
        Assert.IsNotNull(updated.FindCard(11));
        Assert.IsNotNull(updated.FindCard(21));
        Assert.IsNotNull(updated.FindCard(31));
        Assert.IsNotNull(updated.FindCard(13));
        Assert.IsNotNull(updated.FindCard(23));
        Assert.IsNotNull(updated.FindCard(33));
        updated = updated.InterruptAction(false);
    }

    [Test]
    public void EnemyDrawHandCard()
    {
        SetHostFirst(false);
        AppendAllCardInfoList(new List<CardInfo> {
            new CardInfo { chineseName = "间谍牌", infoId = 6, originPower = 5, ability = CardAbility.Spy, badgeType = CardBadgeType.Wood },
        });
        var model = new WholeAreaModel(true, "S", "E", CardGroup.KumikoFirstYear);
        var infoList = Enumerable.Repeat(6, 13).ToList();
        var idList = Enumerable.Range(63, 13).ToList();
        var updated = model.SelfInit(infoList)
            .EnemyInit(infoList, idList, CardGroup.KumikoThirdYear, false)
            .SelfDrawInitHandCard()
            .SelfReDrawInitHandCard()
            .EnemyDrawInitHandCard(Enumerable.Range(63, 10).ToList());
        var drawIdList = updated.enemySinglePlayerAreaModel.handCardAreaModel.backupCardList.GetRange(0, 2).Select(o => o.cardInfo.id).ToList();
        updated = updated with {
            actionEventList = ImmutableList<ActionEvent>.Empty
        };
        updated = updated.EnemyDrawHandCard(drawIdList);
        Assert.AreEqual(1, updated.actionEventList.Count);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[0].type);
        Assert.AreEqual("<color=red>E</color> 抽取了2张牌\n", updated.actionEventList[0].args[0]);
    }

    [Test]
    public void ChooseCard_Pass_Self()
    {
        SetHostFirst(true);
        var model = new WholeAreaModel(true, "S", "E", CardGroup.KumikoFirstYear);
        var updated = model.SelfInit(allInfoIdList)
            .EnemyInit(allInfoIdList, allIdList, CardGroup.KumikoThirdYear, false)
            .SelfDrawInitHandCard()
            .SelfReDrawInitHandCard()
            .EnemyDrawInitHandCard(new List<int> { 13, 23 });
        updated = updated with {
            actionEventList = ImmutableList<ActionEvent>.Empty
        };
        updated = updated.Pass(true);
        Assert.AreEqual(GameState.State.WAIT_ENEMY_ACTION, updated.gameState.curState);
        Assert.AreEqual(GameState.ActionState.None, updated.gameState.actionState);
        Assert.AreEqual(0, updated.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(0, updated.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(2, updated.actionEventList.Count);
        Assert.AreEqual(ActionEvent.Type.BattleMsg, updated.actionEventList[0].type);
        Assert.AreEqual(BattleModel.ActionType.Pass, updated.actionEventList[0].args[0]);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[1].type);
        Assert.AreEqual("<color=green>S</color> 放弃跟牌\n", updated.actionEventList[1].args[0]);
    }

    [Test]
    public void ChooseCard_Pass_Enemy()
    {
        SetHostFirst(false);
        var model = new WholeAreaModel(true, "S", "E", CardGroup.KumikoFirstYear);
        var updated = model.SelfInit(allInfoIdList)
            .EnemyInit(allInfoIdList, allIdList, CardGroup.KumikoThirdYear, false)
            .SelfDrawInitHandCard()
            .SelfReDrawInitHandCard()
            .EnemyDrawInitHandCard(new List<int> { 13, 23 });
        updated = updated with {
            actionEventList = ImmutableList<ActionEvent>.Empty
        };
        updated = updated.Pass(false);
        Assert.AreEqual(GameState.State.WAIT_SELF_ACTION, updated.gameState.curState);
        Assert.AreEqual(GameState.ActionState.None, updated.gameState.actionState);
        Assert.AreEqual(0, updated.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(0, updated.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(1, updated.actionEventList.Count);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[0].type);
        Assert.AreEqual("<color=red>E</color> 放弃跟牌\n", updated.actionEventList[0].args[0]);
    }

    // pass然后一局结束 平局
    [Test]
    public void ChooseCard_Pass_SetFinish_Draw()
    {
        SetHostFirst(true);
        AppendAllCardInfoList(new List<CardInfo> {
            new CardInfo { chineseName = "天气牌-木管", infoId = 6, ability = CardAbility.SunFes, cardType = CardType.Util },
        });
        var model = new WholeAreaModel(true, "S", "E", CardGroup.KumikoSecondYear);
        var updated = model.SelfInit(new List<int> { 1, 6 })
            .EnemyInit(new List<int> { 1 }, new List<int> { 13 }, CardGroup.KumikoSecondYear, false)
            .SelfDrawInitHandCard()
            .SelfReDrawInitHandCard()
            .EnemyDrawInitHandCard(new List<int> { 13 });
        updated = updated.ChooseCard(updated.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0], true);
        updated = updated.ChooseCard(updated.enemySinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0], false);
        updated = updated.ChooseCard(updated.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0], true);
        updated = updated.Pass(false);
        updated = updated with {
            actionEventList = ImmutableList<ActionEvent>.Empty
        };
        updated = updated.Pass(true);
        Assert.AreEqual(GameState.State.WAIT_ENEMY_ACTION, updated.gameState.curState); // 平分交换先手
        Assert.AreEqual(GameState.ActionState.None, updated.gameState.actionState);
        Assert.AreEqual(0, updated.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(0, updated.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(0, updated.weatherAreaModel.wood.cardList.Count);
        Assert.AreEqual(1, updated.selfSinglePlayerAreaModel.discardAreaModel.cardListModel.cardList.Count);
        Assert.AreEqual(1, updated.enemySinglePlayerAreaModel.discardAreaModel.cardListModel.cardList.Count);
        Assert.AreEqual(false, updated.selfSinglePlayerAreaModel.battleRowAreaList[(int)CardBadgeType.Wood].hasWeatherBuff);
        Assert.AreEqual(false, updated.enemySinglePlayerAreaModel.battleRowAreaList[(int)CardBadgeType.Wood].hasWeatherBuff);
        Assert.AreEqual(5, updated.actionEventList.Count);
        Assert.AreEqual(ActionEvent.Type.BattleMsg, updated.actionEventList[0].type);
        Assert.AreEqual(BattleModel.ActionType.Pass, updated.actionEventList[0].args[0]);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[1].type);
        Assert.AreEqual("<color=green>S</color> 放弃跟牌\n", updated.actionEventList[1].args[0]);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[2].type);
        Assert.AreEqual("本局结果：双方平局\n", updated.actionEventList[2].args[0]);
        Assert.AreEqual(ActionEvent.Type.Sfx, updated.actionEventList[3].type);
        Assert.AreEqual(AudioManager.SFXType.SetFinish, updated.actionEventList[3].args[0]);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[4].type);
        Assert.AreEqual("新一局开始，<color=red>E</color> 先手\n", updated.actionEventList[4].args[0]);
    }

    [Test]
    public void ChooseCard_Pass_SetFinish_Win()
    {
        SetHostFirst(true);
        var model = new WholeAreaModel(true, "S", "E", CardGroup.KumikoSecondYear);
        var updated = model.SelfInit(new List<int> { 1 })
            .EnemyInit(new List<int> { 1 }, new List<int> { 13 }, CardGroup.KumikoSecondYear, false)
            .SelfDrawInitHandCard()
            .SelfReDrawInitHandCard()
            .EnemyDrawInitHandCard(new List<int> { 13 });
        updated = updated.ChooseCard(updated.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0], true);
        updated = updated.Pass(false);
        updated = updated with {
            actionEventList = ImmutableList<ActionEvent>.Empty
        };
        updated = updated.Pass(true);
        Assert.AreEqual(GameState.State.WAIT_SELF_ACTION, updated.gameState.curState); // 1:0
        Assert.AreEqual(GameState.ActionState.None, updated.gameState.actionState);
        Assert.AreEqual(0, updated.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(0, updated.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(5, updated.actionEventList.Count);
        Assert.AreEqual(ActionEvent.Type.BattleMsg, updated.actionEventList[0].type);
        Assert.AreEqual(BattleModel.ActionType.Pass, updated.actionEventList[0].args[0]);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[1].type);
        Assert.AreEqual("<color=green>S</color> 放弃跟牌\n", updated.actionEventList[1].args[0]);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[2].type);
        Assert.AreEqual("本局结果：<color=green>S</color> 胜利\n", updated.actionEventList[2].args[0]);
        Assert.AreEqual(ActionEvent.Type.Sfx, updated.actionEventList[3].type);
        Assert.AreEqual(AudioManager.SFXType.SetFinish, updated.actionEventList[3].args[0]);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[4].type);
        Assert.AreEqual("新一局开始，<color=green>S</color> 先手\n", updated.actionEventList[4].args[0]);
    }

    [Test]
    public void ChooseCard_Pass_SetFinish_Lose()
    {
        SetHostFirst(false);
        var model = new WholeAreaModel(true, "S", "E", CardGroup.KumikoSecondYear);
        var updated = model.SelfInit(new List<int> { 1 })
            .EnemyInit(new List<int> { 1 }, new List<int> { 13 }, CardGroup.KumikoSecondYear, false)
            .SelfDrawInitHandCard()
            .SelfReDrawInitHandCard()
            .EnemyDrawInitHandCard(new List<int> { 13 });
        updated = updated.ChooseCard(updated.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0], false);
        updated = updated.Pass(true);
        updated = updated with {
            actionEventList = ImmutableList<ActionEvent>.Empty
        };
        updated = updated.Pass(false);
        Assert.AreEqual(GameState.State.WAIT_ENEMY_ACTION, updated.gameState.curState); // 0:1
        Assert.AreEqual(GameState.ActionState.None, updated.gameState.actionState);
        Assert.AreEqual(0, updated.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(0, updated.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(4, updated.actionEventList.Count);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[0].type);
        Assert.AreEqual("<color=red>E</color> 放弃跟牌\n", updated.actionEventList[0].args[0]);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[1].type);
        Assert.AreEqual("本局结果：<color=red>E</color> 胜利\n", updated.actionEventList[1].args[0]);
        Assert.AreEqual(ActionEvent.Type.Sfx, updated.actionEventList[2].type);
        Assert.AreEqual(AudioManager.SFXType.SetFinish, updated.actionEventList[2].args[0]);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[3].type);
        Assert.AreEqual("新一局开始，<color=red>E</color> 先手\n", updated.actionEventList[3].args[0]);
    }

    // 久一年获胜，抽牌
    [Test]
    public void ChooseCard_K1_SetFinish_Win_Self()
    {
        SetHostFirst(true);
        var model = new WholeAreaModel(true, "S", "E", CardGroup.KumikoFirstYear);
        var updated = model.SelfInit(Enumerable.Repeat(1, 11).ToList())
            .EnemyInit(new List<int> { 1 }, new List<int> { 13 }, CardGroup.KumikoFirstYear, false)
            .SelfDrawInitHandCard()
            .SelfReDrawInitHandCard()
            .EnemyDrawInitHandCard(new List<int> { 13 });
        updated = updated.ChooseCard(updated.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0], true);
        updated = updated.Pass(false);
        updated = updated with {
            actionEventList = ImmutableList<ActionEvent>.Empty
        };
        var backCard = updated.selfSinglePlayerAreaModel.handCardAreaModel.backupCardList[0];
        updated = updated.Pass(true);
        Assert.AreEqual(GameState.State.WAIT_SELF_ACTION, updated.gameState.curState); // 1:0
        Assert.AreEqual(GameState.ActionState.None, updated.gameState.actionState);
        Assert.AreEqual(0, updated.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(0, updated.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(0, updated.selfSinglePlayerAreaModel.handCardAreaModel.backupCardList.Count);
        Assert.AreEqual(10, updated.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList.Count);
        Assert.AreEqual(7, updated.actionEventList.Count);
        Assert.AreEqual(ActionEvent.Type.BattleMsg, updated.actionEventList[0].type);
        Assert.AreEqual(BattleModel.ActionType.Pass, updated.actionEventList[0].args[0]);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[1].type);
        Assert.AreEqual("<color=green>S</color> 放弃跟牌\n", updated.actionEventList[1].args[0]);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[2].type);
        Assert.AreEqual("本局结果：<color=green>S</color> 胜利\n", updated.actionEventList[2].args[0]);
        Assert.AreEqual(ActionEvent.Type.Sfx, updated.actionEventList[3].type);
        Assert.AreEqual(AudioManager.SFXType.SetFinish, updated.actionEventList[3].args[0]);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[4].type);
        Assert.AreEqual("新一局开始，<color=green>S</color> 先手\n", updated.actionEventList[4].args[0]);
        Assert.AreEqual(ActionEvent.Type.BattleMsg, updated.actionEventList[5].type);
        Assert.AreEqual(BattleModel.ActionType.DrawHandCard, updated.actionEventList[5].args[0]);
        CollectionAssert.AreEquivalent(new List<int> { backCard.cardInfo.id }, (List<int>)(updated.actionEventList[5].args[1]));
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[6].type);
        Assert.AreEqual("<color=green>S</color> 抽取了1张牌\n", updated.actionEventList[6].args[0]);
    }

    [Test]
    public void ChooseCard_K1_SetFinish_Win_Enemy()
    {
        SetHostFirst(false);
        var model = new WholeAreaModel(true, "S", "E", CardGroup.KumikoFirstYear);
        var updated = model.SelfInit(new List<int> { 1 })
            .EnemyInit(Enumerable.Repeat(1, 13).ToList(), Enumerable.Range(13, 13).ToList(), CardGroup.KumikoFirstYear, false)
            .SelfDrawInitHandCard()
            .SelfReDrawInitHandCard()
            .EnemyDrawInitHandCard(Enumerable.Range(13, 10).ToList());
        updated = updated.ChooseCard(updated.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0], false);
        updated = updated.Pass(true);
        updated = updated with {
            actionEventList = ImmutableList<ActionEvent>.Empty
        };
        updated = updated.Pass(false);
        Assert.AreEqual(GameState.State.WAIT_ENEMY_ACTION, updated.gameState.curState); // 0:1
        Assert.AreEqual(GameState.ActionState.None, updated.gameState.actionState);
        Assert.AreEqual(0, updated.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(0, updated.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(4, updated.actionEventList.Count);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[0].type);
        Assert.AreEqual("<color=red>E</color> 放弃跟牌\n", updated.actionEventList[0].args[0]);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[1].type);
        Assert.AreEqual("本局结果：<color=red>E</color> 胜利\n", updated.actionEventList[1].args[0]);
        Assert.AreEqual(ActionEvent.Type.Sfx, updated.actionEventList[2].type);
        Assert.AreEqual(AudioManager.SFXType.SetFinish, updated.actionEventList[2].args[0]);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[3].type);
        Assert.AreEqual("新一局开始，<color=red>E</color> 先手\n", updated.actionEventList[3].args[0]);
        var drawIdList = updated.enemySinglePlayerAreaModel.handCardAreaModel.backupCardList.GetRange(0, 1).Select(o => o.cardInfo.id).ToList();
        updated = updated with {
            actionEventList = ImmutableList<ActionEvent>.Empty
        };
        updated = updated.EnemyDrawHandCard(drawIdList);
        Assert.AreEqual(1, updated.actionEventList.Count);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[0].type);
        Assert.AreEqual("<color=red>E</color> 抽取了1张牌\n", updated.actionEventList[0].args[0]);
    }

    // 游戏结束，本方获胜
    [Test]
    public void ChooseCard_GameFinish_SelfWinner()
    {
        SetHostFirst(true);
        var model = new WholeAreaModel(true, "S", "E", CardGroup.KumikoSecondYear);
        var updated = model.SelfInit(new List<int> { 1 })
            .EnemyInit(new List<int> { 1 }, new List<int> { 13 }, CardGroup.KumikoSecondYear, false)
            .SelfDrawInitHandCard()
            .SelfReDrawInitHandCard()
            .EnemyDrawInitHandCard(new List<int> { 13 });
        updated = updated.ChooseCard(updated.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0], true);
        updated = updated.Pass(false);
        updated = updated.Pass(true);
        updated = updated.Pass(true);
        updated = updated with {
            actionEventList = ImmutableList<ActionEvent>.Empty
        };
        updated = updated.Pass(false);
        Assert.AreEqual(GameState.State.STOP, updated.gameState.curState);
        Assert.AreEqual(GameState.ActionState.None, updated.gameState.actionState);
        Assert.AreEqual(0, updated.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(0, updated.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(3, updated.actionEventList.Count);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[0].type);
        Assert.AreEqual("<color=red>E</color> 放弃跟牌\n", updated.actionEventList[0].args[0]);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[1].type);
        Assert.AreEqual("本局结果：双方平局\n", updated.actionEventList[1].args[0]);
        Assert.AreEqual(ActionEvent.Type.Sfx, updated.actionEventList[2].type);
        Assert.AreEqual(AudioManager.SFXType.Win, updated.actionEventList[2].args[0]);
    }

    // 游戏结束，本方获胜
    [Test]
    public void ChooseCard_GameFinish_EnemyWinner()
    {
        SetHostFirst(false);
        var model = new WholeAreaModel(true, "S", "E", CardGroup.KumikoSecondYear);
        var updated = model.SelfInit(new List<int> { 1 })
            .EnemyInit(new List<int> { 1 }, new List<int> { 13 }, CardGroup.KumikoSecondYear, false)
            .SelfDrawInitHandCard()
            .SelfReDrawInitHandCard()
            .EnemyDrawInitHandCard(new List<int> { 13 });
        updated = updated.ChooseCard(updated.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList[0], false);
        updated = updated.Pass(true);
        updated = updated.Pass(false);
        updated = updated.Pass(false);
        updated = updated with {
            actionEventList = ImmutableList<ActionEvent>.Empty
        };
        updated = updated.Pass(true);
        Assert.AreEqual(GameState.State.STOP, updated.gameState.curState);
        Assert.AreEqual(GameState.ActionState.None, updated.gameState.actionState);
        Assert.AreEqual(0, updated.selfSinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(0, updated.enemySinglePlayerAreaModel.GetCurrentPower());
        Assert.AreEqual(3, updated.actionEventList.Count);
        Assert.AreEqual(ActionEvent.Type.BattleMsg, updated.actionEventList[0].type);
        Assert.AreEqual(BattleModel.ActionType.Pass, updated.actionEventList[0].args[0]);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[1].type);
        Assert.AreEqual("<color=green>S</color> 放弃跟牌\n", updated.actionEventList[1].args[0]);
        Assert.AreEqual(ActionEvent.Type.ActionText, updated.actionEventList[2].type);
        Assert.AreEqual("本局结果：双方平局\n", updated.actionEventList[2].args[0]);
    }
}
