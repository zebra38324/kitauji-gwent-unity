using System.Collections.Generic;
using static UnityEngine.Rendering.DebugUI;

/**
 * 记录游戏中每局的先后手顺序、每局结果、是否pass等
 * 以及PlaySceneModel的状态
 */
public class PlayStateTracker
{
    private string TAG = "PlayStateTracker";

    public bool selfPass = false; // self本局已pass

    public bool enemyPass = false; // enemy本局已pass

    public static int SET_NUM = 3; // 每场比赛最多三局

    // 每局比赛的结果，正数代表self赢，负数enemy赢，0平局
    // List长度为已完成的局数
    public List<int> setResult { get; private set; }

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

    public State curState { get; private set; }

    // curState变化的时间点
    public long stateChangeTs { get; private set; }

    public static long TURN_TIME = 30000; // 每方出牌30s时限

    // actionState不为None时，不允许流转State
    // 只能在None与非None之间互相流转
    public enum ActionState
    {
        None = 0, // 无特殊状态
        ATTACKING, // 正在使用攻击技能中
        MEDICING, // 正在使用复活技能中
    }

    public ActionState actionState { get; private set; }

    public PlayStateTracker(bool isHost = true)
    {
        TAG += isHost ? "-Host" : "-Player";
        curState = State.WAIT_BACKUP_INFO;
        actionState = ActionState.None;
        stateChangeTs = 0;
    }

    // 流转curState
    public void TransState(State newState)
    {
        TransStateInternal(newState);
        if (selfPass && enemyPass) {
            KLog.I(TAG, "TransState: set finsih");
            TransStateInternal(State.SET_FINFISH);
            // TODO: finish set
        } else if (selfPass && curState == State.WAIT_SELF_ACTION) {
            KLog.I(TAG, "TransState: self pass, also enemy turn");
            TransStateInternal(State.WAIT_ENEMY_ACTION);
        } else if (enemyPass && curState == State.WAIT_ENEMY_ACTION) {
            KLog.I(TAG, "TransState: enemy pass, also self turn");
            TransStateInternal(State.WAIT_SELF_ACTION);
        }
    }

    // 流转actionState
    public void TransActionState(ActionState newActionState)
    {
        if (actionState == newActionState) {
            return;
        }
        if (!CheckNewActionStateValid(actionState, newActionState)) {
            return;
        }
        KLog.I(TAG, "TransActionState: cur = " + actionState + ", new = " + newActionState);
        actionState = newActionState;
    }

    public void Pass(bool isSelf)
    {
        if (isSelf && curState == State.WAIT_SELF_ACTION) {
            KLog.I(TAG, "Pass: isSelf: " + isSelf);
            selfPass = true;
            TransState(State.WAIT_ENEMY_ACTION);
        } else if (curState == State.WAIT_ENEMY_ACTION) {
            KLog.I(TAG, "Pass: isSelf: " + isSelf);
            enemyPass = true;
            TransState(State.WAIT_SELF_ACTION);
        } else {
            KLog.E(TAG, "Pass: state invalid: " + curState + ", isSelf = " + isSelf);
        }
    }

    private bool CheckNewStateValid(State oldState, State newState)
    {
        if (oldState == newState) {
            return false;
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
        bool valid = true;
        switch (oldActionState) {
            case ActionState.None: {
                break;
            }
            case ActionState.ATTACKING: {
                if (newActionState != ActionState.None) {
                    valid = false;
                }
                break;
            }
            case ActionState.MEDICING: {
                if (newActionState != ActionState.None) {
                    valid = false;
                }
                break;
            }
            default: {
                valid = false;
                break;
            }
        }
        if (!valid) {
            KLog.E(TAG, "CheckNewActionStateValid: invalid, cur = " + oldActionState + ", new = " + newActionState);
        }
        return valid;
    }

    private void TransStateInternal(State newState)
    {
        if (curState == newState) {
            KLog.I(TAG, "TransStateInternal: same: " + curState);
            return;
        }
        if (actionState != ActionState.None) {
            KLog.E(TAG, "TransStateInternal: cur = " + curState + ", actionState invalid = " + actionState);
            return;
        }
        if (!CheckNewStateValid(curState, newState)) {
            return;
        }
        KLog.I(TAG, "TransStateInternal: cur = " + curState + ", new = " + newState);
        curState = newState;
        if (curState != State.WAIT_SELF_ACTION && curState != State.WAIT_ENEMY_ACTION) {
            selfPass = false;
            enemyPass = false;
        }
        stateChangeTs = KTime.CurrentMill();
    }
}
