using System;
using System.Collections;
using System.Collections.Generic;
/**
 * 游戏对局场景的逻辑管理，单例
 * 流程：
 * 1. SetBackupCardInfoIdList，设置本方备选卡牌，生成CardModel并发送信息至BattleModel
 * 2. EnemyMsgCallback，等待收到对方备选卡牌信息，并生成对应CardModel
 * 3. DrawInitHandCard，抽取本方手牌，等待用户选择重抽的牌
 * 4. ReDrawInitHandCard，确定初始手牌，并发送信息至BattleModel
 * 5. EnemyMsgCallback，收到对方手牌信息
 * 6. StartGame，开始整场比赛（不需要外部调用，内部自动流转）
 * 7. ChooseCard，双方出牌
 * 8. Pass，跳过本局出牌。双方都pass后，结束本局。
 * 9. Stop，结束整场比赛。TODO
 */
public class PlaySceneModel
{
    private string TAG = "PlaySceneModel";

    public PlaySceneModel(bool isHost_ = true)
    {
        isHost = isHost_;
        TAG += isHost ? "-Host" : "-Player";
        selfSinglePlayerAreaModel = new SinglePlayerAreaModel(isHost);
        enemySinglePlayerAreaModel = new SinglePlayerAreaModel(isHost);
        battleModel = new BattleModel(isHost);
        battleModel.EnemyMsgCallback += EnemyMsgCallback;
        tracker = new PlayStateTracker(isHost);
    }

    public SinglePlayerAreaModel selfSinglePlayerAreaModel { get; private set; }

    // 待确定的初始手牌区
    public RowAreaModel selfPrepareHandCardAreaModel { get; private set; }

    public SinglePlayerAreaModel enemySinglePlayerAreaModel { get; private set; }

    public BattleModel battleModel { get; private set; }

    // 对方操作是否发生了更新，用于指示是否需要更新ui
    public bool hasEnemyUpdate = false;

    private CardModel attackCard = null; // ActionState.ATTACKING时，记录发动攻击的牌

    public PlayStateTracker tracker { get; private set; }

    private bool isHost;

    public void EnemyMsgCallback(BattleModel.ActionType actionType, params object[] list)
    {
        switch (actionType) {
            case BattleModel.ActionType.Init: {
                List<int> infoIdList = (List<int>)list[0];
                List<int> idList = (List<int>)list[1];
                enemySinglePlayerAreaModel.SetBackupCardInfoIdList(infoIdList, idList);
                lock (this) {
                    if (selfSinglePlayerAreaModel.backupCardList.Count > 0) {
                        tracker.TransState(PlayStateTracker.State.WAIT_INIT_HAND_CARD); // 多线程问题加锁
                    }
                }
                break;
            }
            case BattleModel.ActionType.DrawHandCard: {
                List<int> idList = (List<int>)list[0];
                enemySinglePlayerAreaModel.DrawHandCards(idList);
                lock (this) { // 多线程问题加锁
                    if (selfSinglePlayerAreaModel.handRowAreaModel.cardList.Count > 0) {
                        WillWaitStartGame();
                    }
                }
                break;
            }
            case BattleModel.ActionType.StartGame: {
                bool hostFirst = (bool)list[0];
                tracker.StartGamePlayer(hostFirst);
                break;
            }
            case BattleModel.ActionType.ChooseCard: {
                List<int> idList = (List<int>)list[0];
                int id = idList[0];
                CardModel card = selfSinglePlayerAreaModel.FindCard(id);
                if (card == null) {
                    card = enemySinglePlayerAreaModel.FindCard(id);
                    if (card == null) {
                        KLog.E(TAG, "EnemyMsgCallback: ChooseCard: invalid id: " + id);
                        return;
                    }
                }
                ChooseCard(card, false);
                break;
            }
            case BattleModel.ActionType.Pass: {
                Pass(false);
                break;
            }
            case BattleModel.ActionType.InterruptAction: {
                InterruptAction(false);
                break;
            }
        }
        hasEnemyUpdate = true;
    }

    // 设置本方备选卡牌
    public void SetBackupCardInfoIdList(List<int> selfInfoIdList)
    {
        if (tracker.curState != PlayStateTracker.State.WAIT_BACKUP_INFO) {
            KLog.E(TAG, "SetBackupCardInfoIdList: state invalid: " + tracker.curState);
            return;
        }
        selfSinglePlayerAreaModel.SetBackupCardInfoIdList(selfInfoIdList);
        // 发送self备选卡牌信息
        Action SendSelfBackupCardInfo = () => {
            List<int> infoIdList = new List<int>();
            List<int> idList = new List<int>();
            foreach (CardModel card in selfSinglePlayerAreaModel.backupCardList) {
                infoIdList.Add(card.cardInfo.infoId);
                idList.Add(card.cardInfo.id);
            }
            battleModel.AddSelfActionMsg(BattleModel.ActionType.Init, infoIdList, idList);
            return;
        };
        SendSelfBackupCardInfo();
        lock (this) {
            if (enemySinglePlayerAreaModel.backupCardList.Count > 0) {
                tracker.TransState(PlayStateTracker.State.WAIT_INIT_HAND_CARD); // 多线程问题加锁
            }
        }
    }

    public void DrawInitHandCard()
    {
        if (tracker.curState != PlayStateTracker.State.WAIT_INIT_HAND_CARD) {
            KLog.E(TAG, "DrawInitHandCard: state invalid: " + tracker.curState);
            return;
        }
        // self抽取十张初始手牌
        selfSinglePlayerAreaModel.DrawInitHandCard();
        // 等待玩家确定需要重新抽取的手牌
        tracker.TransState(PlayStateTracker.State.DOING_INIT_HAND_CARD);
    }

    public void ReDrawInitHandCard()
    {
        if (tracker.curState != PlayStateTracker.State.DOING_INIT_HAND_CARD) {
            KLog.E(TAG, "DrawInitHandCard: state invalid: " + tracker.curState);
            return;
        }
        selfSinglePlayerAreaModel.ReDrawInitHandCard();
        // 发送self初始手牌信息
        Action SendSelfInitHandCardInfo = () => {
            List<int> idList = new List<int>();
            foreach (CardModel card in selfSinglePlayerAreaModel.handRowAreaModel.cardList) {
                idList.Add(card.cardInfo.id);
            }
            battleModel.AddSelfActionMsg(BattleModel.ActionType.DrawHandCard, idList);
            return;
        };
        SendSelfInitHandCardInfo();
        lock (this) { // 多线程问题加锁
            if (enemySinglePlayerAreaModel.handRowAreaModel.cardList.Count > 0) {
                WillWaitStartGame();
            }
        }
    }

    // 跳过本局出牌
    public void Pass(bool isSelf = true)
    {
        tracker.Pass(isSelf);
        if (isSelf) {
            // 发送本方pass动作
            battleModel.AddSelfActionMsg(BattleModel.ActionType.Pass);
        }
    }

    // 是否是本方/对方出牌回合
    public bool IsTurn(bool isSelf)
    {
        if (isSelf) {
            return tracker.curState == PlayStateTracker.State.WAIT_SELF_ACTION;
        } else {
            return tracker.curState == PlayStateTracker.State.WAIT_ENEMY_ACTION;
        }
    }

    // 是否允许选择卡牌
    public bool EnableChooseCard(bool isSelf = true)
    {
        if (isSelf) {
            return tracker.curState == PlayStateTracker.State.WAIT_SELF_ACTION || tracker.curState == PlayStateTracker.State.DOING_INIT_HAND_CARD;
        } else {
            return tracker.curState == PlayStateTracker.State.WAIT_ENEMY_ACTION;
        }
    }

    // 选择卡牌，用户ui操作的处理接口
    // 双方都会调用这个接口
    public void ChooseCard(CardModel card, bool isSelf = true)
    {
        if (!EnableChooseCard(isSelf)) {
            KLog.E(TAG, "ChooseCard: state invalid: " + tracker.curState + ", isSelf = " + isSelf);
        }
        // 从选择卡牌方的视角来看
        SinglePlayerAreaModel selfArea = isSelf ? selfSinglePlayerAreaModel : enemySinglePlayerAreaModel;
        SinglePlayerAreaModel enemyArea = isSelf ? enemySinglePlayerAreaModel : selfSinglePlayerAreaModel;
        KLog.I(TAG, "ChooseCard: " + card.cardInfo.chineseName + ", isSelf = " + isSelf);
        switch (card.selectType) {
            case CardSelectType.None: {
                KLog.I(TAG, "ChooseCard: selectType = None, can not choose");
                return;
            }
            case CardSelectType.ReDrawHandCard: {
                // 记录要选择的重抽手牌
                selfSinglePlayerAreaModel.initHandRowAreaModel.SelectCard(card);
                break;
            }
            case CardSelectType.PlayCard: {
                if (tracker.actionState == PlayStateTracker.ActionState.ATTACKING) {
                    KLog.I(TAG, "ChooseCard: actionState = ATTACKING, can not play card");
                    return;
                }
                if (tracker.actionState == PlayStateTracker.ActionState.MEDICING) {
                    if (card.cardLocation != CardLocation.DiscardArea) {
                        // 此时不许打出非弃牌区的牌
                        KLog.I(TAG, "ChooseCard: actionState = MEDICING, card location invalid = " + card.cardLocation);
                        return;
                    }
                    FinishMedic(selfArea);
                }
                tracker.TransActionState(PlayStateTracker.ActionState.None); // medic技能可能递归打牌，先置空再进行后续
                PlayCard(card, selfArea, enemyArea);
                break;
            }
            case CardSelectType.WithstandAttack: {
                ApplyBeAttacked(card, enemyArea);
                FinishAttack(enemyArea);
                tracker.TransActionState(PlayStateTracker.ActionState.None); // 攻击技能，先完成再置空
                break;
            }
        }
        if (isSelf && tracker.curState == PlayStateTracker.State.WAIT_SELF_ACTION) {
            // 发送本方选择牌动作
            battleModel.AddSelfActionMsg(BattleModel.ActionType.ChooseCard, card.cardInfo.id);
        }
        // 流转双方出牌回合
        if (tracker.actionState == PlayStateTracker.ActionState.None) {
            if (isSelf && tracker.curState == PlayStateTracker.State.WAIT_SELF_ACTION) {
                tracker.TransState(PlayStateTracker.State.WAIT_ENEMY_ACTION);
            } else if (tracker.curState == PlayStateTracker.State.WAIT_ENEMY_ACTION) {
                tracker.TransState(PlayStateTracker.State.WAIT_SELF_ACTION);
            }
        }
    }

    // actionState不为None时，一些特殊情况，中止技能流程并流转curState
    public void InterruptAction(bool isSelf = true)
    {
        if (!(isSelf && tracker.curState == PlayStateTracker.State.WAIT_SELF_ACTION) &&
            !(!isSelf && tracker.curState == PlayStateTracker.State.WAIT_ENEMY_ACTION)) {
            KLog.E(TAG, "InterruptAction: state invalid: " + tracker.curState + ", isSelf = " + isSelf);
        }
        if (tracker.actionState == PlayStateTracker.ActionState.None) {
            KLog.E(TAG, "InterruptAction: actionState = None, invalid, isSelf = " + isSelf);
            return;
        }
        SinglePlayerAreaModel selfArea = isSelf ? selfSinglePlayerAreaModel : enemySinglePlayerAreaModel;
        SinglePlayerAreaModel enemyArea = isSelf ? enemySinglePlayerAreaModel : selfSinglePlayerAreaModel;
        KLog.I(TAG, "InterruptAction: actionState = " + tracker.actionState + ", isSelf = " + isSelf);
        if (tracker.actionState == PlayStateTracker.ActionState.ATTACKING) {
            FinishAttack(enemyArea);
        } else if (tracker.actionState == PlayStateTracker.ActionState.MEDICING) {
            FinishMedic(selfArea);
        }
        tracker.TransActionState(PlayStateTracker.ActionState.None);
        if (isSelf) {
            battleModel.AddSelfActionMsg(BattleModel.ActionType.InterruptAction);
            tracker.TransState(PlayStateTracker.State.WAIT_ENEMY_ACTION);
        } else {
            tracker.TransState(PlayStateTracker.State.WAIT_SELF_ACTION);
        }
    }

    private void WillWaitStartGame()
    {
        tracker.TransState(PlayStateTracker.State.WAIT_START);
        if (isHost) {
            tracker.StartGameHost();
            battleModel.AddSelfActionMsg(BattleModel.ActionType.StartGame, tracker.curState == PlayStateTracker.State.WAIT_SELF_ACTION);
        }
    }

    /**
     * 打出卡牌，可能来源于
     * 1. 手牌主动打出
     * 2. 手牌被动打出
     * 3. 备选卡牌区打出
     * 4. 弃牌区复活打出
     * 5. 指挥牌（TODO）
     */
    private void PlayCard(CardModel card, SinglePlayerAreaModel selfArea, SinglePlayerAreaModel enemyArea)
    {
        KLog.I(TAG, "PlayCard: " + card.cardInfo.chineseName);
        // 从原区域移除
        switch (card.cardLocation) {
            case CardLocation.HandArea: {
                selfArea.handRowAreaModel.RemoveCard(card);
                break;
            }
            case CardLocation.DiscardArea: {
                selfArea.discardAreaModel.RemoveCard(card);
                break;
            }
            default: {
                break;
            }
        }

        // 实施卡牌技能
        // 涉及双方的技能放在这一层，其他的放到下层各自实现即可
        // Medic较为特殊，放到下层的话，无法很好地确定结束技能流程的时机，因此也放在这里
        switch (card.cardInfo.ability) {
            case CardAbility.Spy: {
                enemyArea.AddBattleAreaCard(card);
                selfArea.DrawHandCards(2);
                break;
            }
            case CardAbility.Attack: {
                selfArea.AddBattleAreaCard(card);
                ApplyAttack(card, enemyArea);
                break;
            }
            case CardAbility.ScorchWood: {
                selfArea.AddBattleAreaCard(card);
                ApplyScorchWood(enemyArea);
                break;
            }
            case CardAbility.Medic: {
                selfArea.AddBattleAreaCard(card);
                ApplyMedic(selfArea);
                break;
            }
            default: {
                selfArea.AddBattleAreaCard(card);
                break;
            }
        }
    }

    // 实施攻击牌技能
    private void ApplyAttack(CardModel card, SinglePlayerAreaModel enemyArea)
    {
        // 统计可被攻击的牌数量
        int count = enemyArea.CountBattleAreaCard((CardModel targetCard) => {
            return targetCard.cardInfo.cardType == CardType.Normal;
        });
        if (count == 0) {
            KLog.I(TAG, "ApplyAttack: no target card");
            return;
        }
        // 目标卡牌准备被攻击
        enemyArea.ApplyBattleAreaAction((CardModel targetCard) => {
            return targetCard.cardInfo.cardType == CardType.Normal;
        }, (CardModel targetCard) => {
            targetCard.selectType = CardSelectType.WithstandAttack;
        });
        attackCard = card;
        tracker.TransActionState(PlayStateTracker.ActionState.ATTACKING);
    }

    // 为被攻击的牌添加debuff
    private void ApplyBeAttacked(CardModel card, SinglePlayerAreaModel enemyArea)
    {
        if (attackCard == null) {
            KLog.E(TAG, "ApplyBeAttacked: attackCard is null");
            return;
        }
        int attackNum = attackCard.cardInfo.attackNum;
        if (attackNum == 2) {
            card.AddBuff(CardBuffType.Attack2, 1);
        } else {
            card.AddBuff(CardBuffType.Attack4, 1);
        }
        // 攻击可能导致卡牌被移除
        if (card.IsDead()) {
            KLog.I(TAG, "ApplyBeAttacked: remove " + card.cardInfo.chineseName);
            if (card.cardInfo.badgeType == CardBadgeType.Wood) {
                enemyArea.woodRowAreaModel.RemoveCard(card);
            } else if (card.cardInfo.badgeType == CardBadgeType.Wood) {
                enemyArea.brassRowAreaModel.RemoveCard(card);
            } else {
                enemyArea.percussionRowAreaModel.RemoveCard(card);
            }
            enemyArea.discardAreaModel.AddCard(card);
        }
    }

    // 结束攻击技能的流程
    private void FinishAttack(SinglePlayerAreaModel enemyArea)
    {
        enemyArea.ApplyBattleAreaAction((CardModel card) => {
            return card.cardInfo.cardType == CardType.Normal;
        }, (CardModel card) => {
            card.selectType = CardSelectType.None;
        });
        attackCard = null;
    }

    // 应用伞击技能
    private void ApplyScorchWood(SinglePlayerAreaModel enemyArea)
    {
        List<CardModel> targetCardList = enemyArea.woodRowAreaModel.ApplyScorchWood();
        foreach (CardModel card in targetCardList) {
            enemyArea.woodRowAreaModel.RemoveCard(card);
            enemyArea.discardAreaModel.AddCard(card);
            KLog.I(TAG, "ApplyScorchWood: remove " + card.cardInfo.chineseName);
        }
    }

    // 应用复活技能
    private void ApplyMedic(SinglePlayerAreaModel selfArea)
    {
        if (selfArea.discardAreaModel.normalCardList.Count == 0) {
            KLog.I(TAG, "ApplyMedic: no target card");
            return;
        }
        foreach (CardModel card in selfArea.discardAreaModel.normalCardList) {
            card.selectType = CardSelectType.PlayCard;
        }
        selfArea.discardAreaModel.SetRow(true);
        tracker.TransActionState(PlayStateTracker.ActionState.MEDICING);
    }

    // 结束复活技能的流程
    private void FinishMedic(SinglePlayerAreaModel selfArea)
    {
        foreach (CardModel card in selfArea.discardAreaModel.normalCardList) {
            card.selectType = CardSelectType.None;
        }
        selfArea.discardAreaModel.ClearRow();
    }
}
