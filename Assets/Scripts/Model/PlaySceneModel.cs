using System;
using System.Collections;
using System.Collections.Generic;
/**
 * 游戏对局场景的逻辑管理，单例
 * 流程：
 * 1. SetBackupCardInfoIdList，设置双方备选卡牌
 * 2. StartSet，开始一小局比赛
 * 3. ChooseCard，双方出牌
 * 4. Pass，跳过本局出牌。双方都pass后，结束本局。
 * 5. Stop，结束整场比赛。TODO
 */
public class PlaySceneModel
{
    private static string TAG = "PlaySceneModel";

    public PlaySceneModel()
    {
        selfSinglePlayerAreaModel = new SinglePlayerAreaModel();
        enemySinglePlayerAreaModel = new SinglePlayerAreaModel();
        curState = State.WAIT_BACKUP_INFO;
        actionState = ActionState.None;
    }

    public SinglePlayerAreaModel selfSinglePlayerAreaModel { get; private set; }

    public SinglePlayerAreaModel enemySinglePlayerAreaModel { get; private set; }

    /**
     * 状态机
     * WAIT_BACKUP_INFO -> WAIT_START
     * WAIT_START -> WAIT_SELF_ACTION, WAIT_ENEMY_ACTION
     * WAIT_SELF_ACTION -> WAIT_ENEMY_ACTION, SET_FINFISH
     * WAIT_ENEMY_ACTION -> WAIT_SELF_ACTION, SET_FINFISH
     * SET_FINFISH -> WAIT_SELF_ACTION, WAIT_ENEMY_ACTION, STOP
     * STOP 不会再继续流转
     */
    private enum State
    {
        WAIT_BACKUP_INFO = 0, // 等待设置备选卡牌信息
        WAIT_START, // 本场对局还未开始
        WAIT_SELF_ACTION, // 等待本方操作
        WAIT_ENEMY_ACTION, // 等待对方操作
        SET_FINFISH, // 一小局比赛结束
        STOP, // 全场结束
    }

    // actionState不为None时，不允许流转State
    // 只能在None与非None之间互相流转
    public enum ActionState
    {
        None = 0, // 无特殊状态
        ATTACKING, // 正在使用攻击技能中
        MEDICING, // 正在使用复活技能中
    }

    private State curState_;

    private State curState {
        get {
            return curState_;
        }
        set {
            if (curState_ == value) {
                KLog.I(TAG, "setCurState: same: " + curState_);
                return;
            }
            if (actionState != ActionState.None) {
                KLog.E(TAG, "setCurState: cur = " + curState_ + ", actionState invalid = " + actionState);
                return;
            }
            switch (curState_) {
                case State.WAIT_BACKUP_INFO: {
                    if (value != State.WAIT_START) {
                        KLog.E(TAG, "setCurState: invalid, cur = " + curState_ + ", new = " + value);
                        return;
                    }
                    break;
                }
                case State.WAIT_START: {
                    if (value != State.WAIT_SELF_ACTION &&
                        value != State.WAIT_ENEMY_ACTION) {
                        KLog.E(TAG, "setCurState: invalid, cur = " + curState_ + ", new = " + value);
                        return;
                    }
                    break;
                }
                case State.WAIT_SELF_ACTION: {
                    if (value != State.WAIT_ENEMY_ACTION &&
                        value != State.SET_FINFISH) {
                        KLog.E(TAG, "setCurState: invalid, cur = " + curState_ + ", new = " + value);
                        return;
                    }
                    if (value == State.WAIT_ENEMY_ACTION && enemyPassing) {
                        if (selfPassing) {
                            KLog.I(TAG, "setCurState: set finsih");
                            value = State.SET_FINFISH;
                            // TODO: 结算
                        } else {
                            KLog.I(TAG, "setCurState: enemy pass, also self turn");
                            value = State.WAIT_SELF_ACTION;
                        }
                    }
                    break;
                }
                case State.WAIT_ENEMY_ACTION: {
                    if (value != State.WAIT_SELF_ACTION &&
                        value != State.SET_FINFISH) {
                        KLog.E(TAG, "setCurState: invalid, cur = " + curState_ + ", new = " + value);
                        return;
                    }
                    if (value == State.WAIT_SELF_ACTION && selfPassing) {
                        if (enemyPassing) {
                            KLog.I(TAG, "setCurState: set finsih");
                            value = State.SET_FINFISH;
                            // TODO: 结算
                        } else {
                            KLog.I(TAG, "setCurState: self pass, also enemy turn");
                            value = State.WAIT_ENEMY_ACTION;
                        }
                    }
                    break;
                }
                case State.SET_FINFISH: {
                    if (value != State.WAIT_SELF_ACTION &&
                        value != State.WAIT_ENEMY_ACTION &&
                        value != State.STOP) {
                        KLog.E(TAG, "setCurState: invalid, cur = " + curState_ + ", new = " + value);
                        return;
                    }
                    break;
                }
                case State.STOP: {
                    KLog.E(TAG, "setCurState: invalid, cur = " + curState_ + ", new = " + value);
                    return;
                }
                default: {
                    KLog.E(TAG, "setCurState: invalid, cur = " + curState_ + ", new = " + value);
                    return;
                }
            }
            KLog.I(TAG, "setCurState: cur = " + curState_ + ", new = " + value);
            curState_ = value;
            if (curState_ == State.WAIT_START ||
                curState_ == State.SET_FINFISH ||
                curState_ == State.STOP) {
                selfPassing = false;
                enemyPassing = false;
            }
        }
    }

    private ActionState actionState_;

    public ActionState actionState {
        get {
            return actionState_;
        }
        private set {
            switch (actionState_) {
                case ActionState.None: {
                    break;
                }
                case ActionState.ATTACKING: {
                    if (value != ActionState.None) {
                        KLog.E(TAG, "setActionState: invalid, cur = " + actionState_ + ", new = " + value);
                        return;
                    }
                    break;
                }
                case ActionState.MEDICING: {
                    if (value != ActionState.None) {
                        KLog.E(TAG, "setActionState: invalid, cur = " + actionState_ + ", new = " + value);
                        return;
                    }
                    break;
                }
                default: {
                    KLog.E(TAG, "setActionState: invalid, cur = " + actionState_ + ", new = " + value);
                    break;
                }
            }
            if (actionState_ != value) {
                KLog.I(TAG, "setActionState: cur = " + actionState_ + ", new = " + value);
            }
            actionState_ = value;
        }
    }

    private CardModel attackCard = null; // ActionState.ATTACKING时，记录发动攻击的牌

    private bool selfPassing; // 本方本局已pass

    private bool enemyPassing; // 对方本局已pass

    // 设置双方所有可用卡牌
    public void SetBackupCardInfoIdList(List<int> selfInfoIdList, List<int> enemyInfoIdList)
    {
        selfSinglePlayerAreaModel.SetBackupCardInfoIdList(selfInfoIdList);
        enemySinglePlayerAreaModel.SetBackupCardInfoIdList(enemyInfoIdList);
        curState = State.WAIT_START;
    }

    // 开始一小局比赛。isSelf：哪方先出牌
    public void StartSet(bool isSelf)
    {
        curState = isSelf ? State.WAIT_SELF_ACTION : State.WAIT_ENEMY_ACTION;
    }

    // 跳过本局出牌
    public void Pass(bool isSelf)
    {
        if (isSelf && curState == State.WAIT_SELF_ACTION) {
            KLog.I(TAG, "Pass: isSelf: " + isSelf);
            selfPassing = true;
            curState = State.WAIT_ENEMY_ACTION;
        } else if (curState == State.WAIT_ENEMY_ACTION) {
            KLog.I(TAG, "Pass: isSelf: " + isSelf);
            enemyPassing = true;
            curState = State.WAIT_SELF_ACTION;
        } else {
            KLog.E(TAG, "Pass: state invalid: " + curState + ", isSelf = " + isSelf);
        }
    }

    // 是否是本方/对方出牌回合
    public bool IsTurn(bool isSelf)
    {
        if (isSelf) {
            return curState == State.WAIT_SELF_ACTION;
        } else {
            return curState == State.WAIT_ENEMY_ACTION;
        }
    }

    // 选择卡牌，用户ui操作的处理接口
    // 双方都会调用这个接口
    public void ChooseCard(CardModel card, bool isSelf)
    {
        if (!(isSelf && curState == State.WAIT_SELF_ACTION) &&
            !(!isSelf && curState == State.WAIT_ENEMY_ACTION)) {
            KLog.E(TAG, "ChooseCard: state invalid: " + curState + ", isSelf = " + isSelf);
        }
        // 从选择卡牌方的视角来看
        SinglePlayerAreaModel selfArea = isSelf ? selfSinglePlayerAreaModel : enemySinglePlayerAreaModel;
        SinglePlayerAreaModel enemyArea = isSelf ? enemySinglePlayerAreaModel : selfSinglePlayerAreaModel;
        switch (card.selectType) {
            case CardSelectType.None: {
                KLog.I(TAG, "ChooseCard: selectType = None, can not choose");
                return;
            }
            case CardSelectType.PlayCard: {
                if (actionState == ActionState.MEDICING) {
                    FinishMedic(selfArea);
                }
                actionState = ActionState.None; // medic技能可能递归打牌，先置空再进行后续
                PlayCard(card, selfArea, enemyArea);
                break;
            }
            case CardSelectType.WithstandAttack: {
                ApplyBeAttacked(card, enemyArea);
                FinishAttack(enemyArea);
                actionState = ActionState.None; // 攻击技能，先完成再置空
                break;
            }
        }
        // TODO: 刷新点数
        // 流转双方出牌回合
        if (actionState == ActionState.None) {
            if (isSelf) {
                curState = State.WAIT_ENEMY_ACTION;
            } else {
                curState = State.WAIT_SELF_ACTION;
            }
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
        actionState = ActionState.ATTACKING;
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
        actionState = ActionState.MEDICING;
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
