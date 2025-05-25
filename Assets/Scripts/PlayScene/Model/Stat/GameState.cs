using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;

/**
 * 状态机流转
 */
public record GameState
{
    private string TAG = "GameState";

    public static long TURN_TIME = 30000; // 每方出牌30s时限

    /**
     * 状态机
     * WAIT_BACKUP_INFO -> WAIT_INIT_HAND_CARD
     * WAIT_INIT_HAND_CARD -> WAIT_START
     * WAIT_START -> WAIT_SELF_ACTION, WAIT_ENEMY_ACTION
     * WAIT_SELF_ACTION -> WAIT_ENEMY_ACTION, SET_FINFISH
     * WAIT_ENEMY_ACTION -> WAIT_SELF_ACTION, SET_FINFISH
     * SET_FINFISH -> WAIT_SELF_ACTION, WAIT_ENEMY_ACTION, STOP
     * STOP 不会再继续流转
     */
    public enum State
    {
        WAIT_BACKUP_INFO = 0, // 等待设置备选卡牌信息
        WAIT_INIT_HAND_CARD, // 等待抽取初始手牌
        WAIT_START, // 本场对局还未开始
        WAIT_SELF_ACTION, // 等待本方操作
        WAIT_ENEMY_ACTION, // 等待对方操作
        SET_FINFISH, // 一小局比赛结束
        STOP, // 全场结束
    }

    public State curState { get; init; } = State.WAIT_BACKUP_INFO;

    // actionState不为None时，不允许流转State
    // 只能在None与非None之间互相流转
    public enum ActionState
    {
        None = 0, // 无特殊状态
        ATTACKING, // 正在使用攻击技能中
        MEDICING, // 正在使用复活技能中
        DECOYING, // 正在使用大号君技能中
        HORN_UTILING, // 正在使用HornUtil技能中
        MONAKAING, // 正在使用monaka技能中
    }

    public ActionState actionState { get; init; } = ActionState.None;

    public long stateChangeTs { get; init; } = 0;

    public CardModel actionCard { get; init; } = null; // 用于记录actionState的卡牌

    public bool selfPass { get; init; } = false;

    public bool enemyPass { get; init; } = false;

    public GameState(bool isHost)
    {
        if (TAG == "GameState") {
            TAG += isHost ? "-Host" : "-Player";
        }
    }

    // Pass接口自带状态流转
    public GameState Pass(bool isSelf)
    {
        KLog.I(TAG, "Pass: isSelf: " + isSelf);
        var newRecord = this;
        State newState;
        if (isSelf) {
            newRecord = newRecord with {
                selfPass = true
            };
            newState = State.WAIT_ENEMY_ACTION;
        } else {
            newRecord = newRecord with {
                enemyPass = true
            };
            newState = State.WAIT_SELF_ACTION;
        }
        if (newRecord.selfPass && newRecord.enemyPass) {
            // 双方都Pass了
            newState = State.SET_FINFISH;
            newRecord = newRecord with {
                selfPass = false,
                enemyPass = false
            };
        }
        return newRecord.TransState(newState);
    }

    public GameState TransState(State newState)
    {
        var newRecord = this;
        if (newRecord.curState == newState) {
            KLog.I(TAG, "TransState: same: " + newState);
            return newRecord;
        }
        if (actionState != ActionState.None) {
            KLog.E(TAG, "TransState: cur = " + curState + ", actionState invalid = " + actionState);
            return newRecord;
        }
        if (newRecord.selfPass && newState == State.WAIT_SELF_ACTION) {
            KLog.I(TAG, "TransState: self pass, also enemy turn");
            newState = State.WAIT_ENEMY_ACTION;
        } else if (newRecord.enemyPass && newState == State.WAIT_ENEMY_ACTION) {
            KLog.I(TAG, "TransState: enemy pass, also self turn");
            newState = State.WAIT_SELF_ACTION;
        }

        if (!CheckNewStateValid(newRecord.curState, newState)) {
            return newRecord;
        }
        KLog.I(TAG, "TransState: cur = " + newRecord.curState + ", new = " + newState);
        return newRecord with {
            curState = newState,
            stateChangeTs = KTime.CurrentMill()
        };
    }

    public GameState TransActionState(ActionState newActionState, CardModel actionCard_ = null)
    {
        var newRecord = this;
        if (!CheckNewActionStateValid(newRecord.actionState, newActionState)) {
            return newRecord;
        }
        if (newRecord.actionState != newActionState) {
            KLog.I(TAG, "TransActionState: cur = " + actionState + ", new = " + newActionState);
        }
        return newRecord with {
            actionState = newActionState,
            actionCard = actionCard_
        };
    }

    private bool CheckNewStateValid(State oldState, State newState)
    {
        if (oldState == newState) {
            return true;
        }
        bool valid = true;
        switch (oldState) {
            case State.WAIT_BACKUP_INFO: {
                if (newState != State.WAIT_INIT_HAND_CARD) {
                    valid = false;
                }
                break;
            }
            case State.WAIT_INIT_HAND_CARD: {
                if (newState != State.WAIT_START) {
                    valid = false;
                }
                break;
            }
            case State.WAIT_START: {
                if (newState != State.WAIT_SELF_ACTION && newState != State.WAIT_ENEMY_ACTION) {
                    valid = false;
                }
                break;
            }
            case State.WAIT_SELF_ACTION: {
                if (newState != State.WAIT_ENEMY_ACTION && newState != State.SET_FINFISH) {
                    valid = false;
                }
                break;
            }
            case State.WAIT_ENEMY_ACTION: {
                if (newState != State.WAIT_SELF_ACTION && newState != State.SET_FINFISH) {
                    valid = false;
                }
                break;
            }
            case State.SET_FINFISH: {
                if (newState != State.WAIT_SELF_ACTION && newState != State.WAIT_ENEMY_ACTION && newState != State.STOP) {
                    valid = false;
                }
                break;
            }
            case State.STOP: {
                valid = false;
                break;
            }
            default: {
                valid = false;
                break;
            }
        }
        if (!valid) {
            KLog.E(TAG, "CheckStateValid: invalid, cur = " + oldState + ", new = " + newState);
        }
        return valid;
    }

    private bool CheckNewActionStateValid(ActionState oldActionState, ActionState newActionState)
    {
        bool valid = oldActionState == ActionState.None || newActionState == ActionState.None;
        if (!valid) {
            KLog.E(TAG, "CheckNewActionStateValid: invalid, cur = " + oldActionState + ", new = " + newActionState);
        }
        return valid;
    }
}
