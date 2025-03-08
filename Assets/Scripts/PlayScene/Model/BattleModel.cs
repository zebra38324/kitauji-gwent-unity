using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

/**
 * 对战消息交互接口层
 * 对战框架：
 *      涉及方：host、player
 *      逻辑流程：
 *          1. host、player初始化PlaySceneModel，生成各自卡牌的id（包括备选卡牌）
 *          2. host、player将各自卡牌情况（牌组信息 + infoId + id）发送至对方。接收对方的卡牌infoId+id组合后，生成CardModel
 *          3. host、player各自抽取自己的手牌，并将手牌信息（id）发送至对方
 *          4. host向player发送开始整场游戏的信息，并附带先后手信息
 *          5. 用户可能的操作
 *              1) 选择卡牌。将卡牌id发送至对方
 *              2) pass。将信息发送至对方
 *              3) 抽取手牌。将抽到手牌的id发送至对方
 *              4) 由于各种原因，中止了技能流程，不pass但流转出牌方
 *              5) 选择horn util牌的区域
 *      消息发送方式：
 *          1. 调用BattleModel接口，将信息设置到发送队列中
 *          2. BattleModel的发送线程定期轮询，将发送队列的信息发送出去
 *      消息接收方式：
 *          1. （可能是网络模块或本地AI模块）调用BattleModel接口，将信息设置到接收队列中
 *          2. BattleModel的接收线程定期轮询，将接收队列的信息转译为指令回调PlaySceneModel
 *      可能需要注意的点：
 *          1. 双方各自抽取牌，可能涉及id冲突问题。解决方式：各方生成id时进行加盐值处理，暂定host为 *10+1，player为*10+2。保证双方的id不会冲突
 */
public class BattleModel
{
    private string TAG = "BattleModel";

    public enum ActionType
    {
        Init = 0, // 初始卡牌信息。data: cardGroup, infoIdList, idList
        DrawHandCard, // 抽取手牌。data: idList
        StartGame, // 开始整场比赛，仅host向player发送。data: hostFirst
        ChooseCard, // 选择卡牌。data: id
        Pass, // 过牌。data: null
        InterruptAction, // 中断技能流程。data: null
        ChooseHornUtilArea, // 选择horn util区域。data: hornUtilAreaType
    }

    public enum HornUtilAreaType
    {
        Wood = 0,
        Brass,
        Percussion,
    }

    private struct ActionMsg
    {
        public ActionType actionType;
        public CardGroup cardGroup;
        public List<int> infoIdList;
        public List<int> idList;
        public bool hostFirst; // 第一局游戏，是否由host先手
        public HornUtilAreaType hornUtilAreaType;
    }

    public BattleModel(bool isHost = true)
    {
        TAG += isHost ? "-Host" : "-Player";
        sendQueue = new ConcurrentQueue<ActionMsg>();
        receiveQueue = new ConcurrentQueue<ActionMsg>();
        SendCoroutine();
        ReceiveCoroutine();
    }

    public void Release()
    {
        KLog.I(TAG, "Release");
        isAbort = true;
    }

    private bool isAbort = false;

    private ConcurrentQueue<ActionMsg> sendQueue; // 发送队列
    public delegate void SendToEnemyFuncHandler(string actionMsgStr);
    public SendToEnemyFuncHandler SendToEnemyFunc;

    // 收到对端消息后，回调通知PlaySceneModel
    private ConcurrentQueue<ActionMsg> receiveQueue; // 接收队列
    public delegate void EnemyMsgCallbackHandler(ActionType actionType, params object[] list);
    public EnemyMsgCallbackHandler EnemyMsgCallback;

    public void AddSelfActionMsg(ActionType actionType, params object[] list)
    {
        ActionMsg actionMsg = new ActionMsg();
        actionMsg.actionType = actionType;
        switch (actionType) {
            case ActionType.Init: {
                actionMsg.cardGroup = (CardGroup)list[0];
                actionMsg.infoIdList = (List<int>)list[1];
                actionMsg.idList = (List<int>)list[2];
                break;
            }
            case ActionType.DrawHandCard: {
                actionMsg.idList = (List<int>)list[0];
                break;
            }
            case ActionType.StartGame: {
                actionMsg.hostFirst = (bool)list[0];
                break;
            }
            case ActionType.ChooseCard: {
                actionMsg.idList = new List<int> { (int)list[0] };
                break;
            }
            case ActionType.InterruptAction:
            case ActionType.Pass: {
                break;
            }
            case ActionType.ChooseHornUtilArea: {
                actionMsg.hornUtilAreaType = (HornUtilAreaType)list[0];
                break;
            }
        }
        sendQueue.Enqueue(actionMsg);
    }

    public void AddEnemyActionMsg(string actionMsgStr)
    {
        ActionMsg actionMsg = JsonUtility.FromJson<ActionMsg>(actionMsgStr);
        receiveQueue.Enqueue(actionMsg);
        KLog.I(TAG, "AddEnemyActionMsg: " + actionMsgStr);
    }

    private async void SendCoroutine()
    {
        KLog.I(TAG, "SendCoroutine start");
        while (!isAbort) {
            if (sendQueue.Count == 0) {
                await UniTask.Delay(1);
                continue;
            }
            ActionMsg actionMsg;
            if (!sendQueue.TryDequeue(out actionMsg)) {
                continue;
            }
            string actionMsgStr = JsonUtility.ToJson(actionMsg);
            if (SendToEnemyFunc != null) {
                KLog.I(TAG, "SendToEnemyFunc: " + actionMsgStr);
                SendToEnemyFunc(actionMsgStr);
            }
        }
        KLog.I(TAG, "SendCoroutine end");
    }

    private async void ReceiveCoroutine()
    {
        KLog.I(TAG, "ReceiveCoroutine start");
        while (!isAbort) {
            if (receiveQueue.Count == 0) {
                await UniTask.Delay(1);
                continue;
            }
            ActionMsg actionMsg;
            if (!receiveQueue.TryDequeue(out actionMsg)) {
                continue;
            }
            if (EnemyMsgCallback != null) {
                switch (actionMsg.actionType) {
                    case ActionType.Init: {
                        EnemyMsgCallback(actionMsg.actionType, actionMsg.cardGroup, actionMsg.infoIdList, actionMsg.idList);
                        break;
                    }
                    case ActionType.DrawHandCard: {
                        EnemyMsgCallback(actionMsg.actionType, actionMsg.idList);
                        break;
                    }
                    case ActionType.StartGame: {
                        EnemyMsgCallback(actionMsg.actionType, actionMsg.hostFirst);
                        break;
                    }
                    case ActionType.ChooseCard: {
                        EnemyMsgCallback(actionMsg.actionType, actionMsg.idList);
                        break;
                    }
                    case ActionType.InterruptAction:
                    case ActionType.Pass: {
                        EnemyMsgCallback(actionMsg.actionType);
                        break;
                    }
                    case ActionType.ChooseHornUtilArea: {
                        EnemyMsgCallback(actionMsg.actionType, actionMsg.hornUtilAreaType);
                        break;
                    }
                }
            }
        }
        KLog.I(TAG, "ReceiveCoroutine end");
    }
}
