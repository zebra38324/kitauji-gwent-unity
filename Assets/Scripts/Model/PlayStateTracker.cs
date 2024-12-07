using System.Collections.Generic;
using System.Linq;

/**
 * 记录游戏中每局的先后手顺序、每局结果、是否pass等
 * 以及PlaySceneModel的状态
 */
public class PlayStateTracker
{
    private string TAG = "PlayStateTracker";

    public static int HOST_FIRST_RANDOM_MAX = 100;

    public static int HOST_FIRST_RANDOM_MIN = 0;

    public static int HOST_FIRST_RANDOM_THRESHOLD = 50;

    public bool selfPass = false; // self本局已pass

    public bool enemyPass = false; // enemy本局已pass

    public static int SET_NUM = 3; // 每场比赛最多三局

    // 记录一局的先后手、结果等
    private class SetRecord
    {
        public bool selfFirst; // self先手
        public int selfScore;
        public int enemyScore;
        public int result; // 每局结果，-1：self负，0：平局，1：self胜
    }

    // 每局比赛的结果记录
    private List<SetRecord> setRecordList;

    private int curSet = 0; // 当前是第几局

    /**
     * 状态机
     * WAIT_BACKUP_INFO -> WAIT_INIT_HAND_CARD
     * WAIT_INIT_HAND_CARD -> DOING_INIT_HAND_CARD
     * DOING_INIT_HAND_CARD -> WAIT_START
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
        DOING_INIT_HAND_CARD, // 正在抽取初始手牌
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
        setRecordList = Enumerable.Repeat(new SetRecord(), SET_NUM).ToList();
    }

    // host调用
    public void StartGameHost()
    {
        if (curSet > 0) {
            KLog.E(TAG, "StartGameHost: curSet invalid: " + curSet);
            return;
        }
        // host根据random值决策开局先后手
        System.Random ran = new System.Random();
        int randomNum = ran.Next(HOST_FIRST_RANDOM_MIN, HOST_FIRST_RANDOM_MAX); // 范围0-99，大于等于50为host先手
        setRecordList[curSet].selfFirst = randomNum >= HOST_FIRST_RANDOM_THRESHOLD;
        KLog.I(TAG, "StartGameHost: randomNum = " + randomNum + ", selfFirst = " + setRecordList[curSet].selfFirst);
        // 转换状态
        if (setRecordList[curSet].selfFirst) {
            TransState(State.WAIT_SELF_ACTION);
        } else {
            TransState(State.WAIT_ENEMY_ACTION);
        }
    }

    // player调用
    public void StartGamePlayer(bool hostFirst)
    {
        if (curSet > 0) {
            KLog.E(TAG, "StartGamePlayer: curSet invalid: " + curSet);
            return;
        }
        setRecordList[curSet].selfFirst = !hostFirst;
        KLog.I(TAG, "StartGamePlayer: selfFirst = " + setRecordList[curSet].selfFirst);
        // 转换状态
        if (setRecordList[curSet].selfFirst) {
            TransState(State.WAIT_SELF_ACTION);
        } else {
            TransState(State.WAIT_ENEMY_ACTION);
        }
    }

    // 流转curState
    public void TransState(State newState)
    {
        TransStateInternal(newState);
        if (selfPass && enemyPass) {
            KLog.I(TAG, "TransState: set finsih");
            TransStateInternal(State.SET_FINFISH);
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

    // 结算单局游戏
    public void SetFinish(int selfScore, int enemyScore)
    {
        KLog.I(TAG, "SetFinish: curSet = " + curSet + ", selfScore = " + selfScore + ", enemyScore = " + enemyScore);
        setRecordList[curSet].selfScore = selfScore;
        setRecordList[curSet].enemyScore = enemyScore;
        if (selfScore > enemyScore) {
            setRecordList[curSet].result = 1;
        } else if (selfScore < enemyScore) {
            setRecordList[curSet].result = -1;
        } else {
            setRecordList[curSet].result = 0;
        }
        if (curSet >= SET_NUM - 1) {
            // TODO: finish game
            return;
        }
        curSet += 1;
        setRecordList[curSet] = new SetRecord();
        // 上一局胜者，下一局先手。若平局则交换先后手
        if (setRecordList[curSet].result == 1) {
            setRecordList[curSet].selfFirst = true;
        } else if (setRecordList[curSet].result == -1) {
            setRecordList[curSet].selfFirst = false;
        } else {
            setRecordList[curSet].selfFirst = !setRecordList[curSet - 1].selfFirst;
        }
        KLog.I(TAG, "SetFinish: next set self first = " + setRecordList[curSet].selfFirst);
        // 转换状态
        if (setRecordList[curSet].selfFirst) {
            TransState(State.WAIT_SELF_ACTION);
        } else {
            TransState(State.WAIT_ENEMY_ACTION);
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
                if (newState != State.DOING_INIT_HAND_CARD) {
                    valid = false;
                }
                break;
            }
            case State.DOING_INIT_HAND_CARD: {
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
