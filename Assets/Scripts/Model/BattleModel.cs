using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

/**
 * 对战消息交互接口层
 * 对战框架：
 *      涉及方：server、client
 *      逻辑流程：
 *          1. server、client初始化PlaySceneModel，生成各自卡牌的id（包括备选卡牌）
 *          2. server、client将各自卡牌情况（infoId + id）发送至对方。接收对方的卡牌infoId+id组合后，生成CardModel
 *          3. server、client各自抽取自己的手牌，并将手牌信息（id）发送至对方
 *          4. 用户可能的操作
 *              1) 选择卡牌。将卡牌id发送至对方
 *              2) pass。将信息发送至对方
 *              3) 抽取手牌。将抽到手牌的id发送至对方
 *      消息发送方式：
 *          1. 调用BattleModel接口，将信息设置到发送队列中
 *          2. BattleModel的发送线程定期轮询，将发送队列的信息发送出去
 *      消息接收方式：
 *          1. （可能是网络模块或本地AI模块）调用BattleModel接口，将信息设置到接收队列中
 *          2. BattleModel的接收线程定期轮询，将接收队列的信息转译为指令回调PlaySceneModel
 *      可能需要注意的点：
 *          1. 双方各自抽取牌，可能涉及id冲突问题。解决方式：各方生成id时进行加盐值处理，暂定server为 *10+1，client为*10+2。保证双方的id不会冲突
 */
public class BattleModel
{
    private static string TAG = "BattleModel";

    public enum ActionType
    {
        Init = 0, // 初始卡牌信息。data: infoIdList, idList
        DrawHandCard, // 抽取手牌。data: idList
        ChooseCard, // 选择卡牌。data: id
        Pass, // 过牌。data: null
    }

    private struct ActionMsg
    {
        public ActionType actionType;
        public List<int> infoIdList;
        public List<int> idList;
    }

    public BattleModel()
    {
        sendQueue = new ConcurrentQueue<ActionMsg>();
        receiveQueue = new ConcurrentQueue<ActionMsg>();
        sendThread = new Thread(() => {
            SendThreadFunc();
        });
        sendThread.Start();
        receiveThread = new Thread(() => {
            ReceiveThreadFunc();
        });
        receiveThread.Start();
    }

    ~BattleModel()
    {
        isAort = true;
        if (sendThread.IsAlive) {
            sendThread.Join();
            sendThread = null;
        }
        if (receiveThread.IsAlive) {
            receiveThread.Join();
            receiveThread = null;
        }
    }

    private bool isAort = false;
    private Thread sendThread = null;
    private Thread receiveThread = null;

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
                actionMsg.infoIdList = (List<int>)list[0];
                actionMsg.idList = (List<int>)list[1];
                break;
            }
            case ActionType.DrawHandCard: {
                actionMsg.idList = (List<int>)list[0];
                break;
            }
            case ActionType.ChooseCard: {
                actionMsg.idList = new List<int> { (int)list[0] };
                break;
            }
            case ActionType.Pass: {
                break;
            }
        }
        sendQueue.Enqueue(actionMsg);
    }

    public void AddEnemyctionMsg(string actionMsgStr)
    {
        ActionMsg actionMsg = JsonUtility.FromJson<ActionMsg>(actionMsgStr);
        receiveQueue.Enqueue(actionMsg);
        KLog.I(TAG, "AddEnemyctionMsg: " + actionMsgStr);
    }

    private void SendThreadFunc()
    {
        while (!isAort) {
            if (sendQueue.Count == 0) {
                Thread.Sleep(10);
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
    }

    private void ReceiveThreadFunc()
    {
        while (!isAort) {
            if (receiveQueue.Count == 0) {
                Thread.Sleep(10);
                continue;
            }
            ActionMsg actionMsg;
            if (!receiveQueue.TryDequeue(out actionMsg)) {
                continue;
            }
            if (EnemyMsgCallback != null) {
                switch (actionMsg.actionType) {
                    case ActionType.Init: {
                        EnemyMsgCallback(actionMsg.actionType, actionMsg.infoIdList, actionMsg.idList);
                        break;
                    }
                    case ActionType.DrawHandCard: {
                        EnemyMsgCallback(actionMsg.actionType, actionMsg.idList);
                        break;
                    }
                    case ActionType.ChooseCard: {
                        EnemyMsgCallback(actionMsg.actionType, actionMsg.idList);
                        break;
                    }
                    case ActionType.Pass: {
                        EnemyMsgCallback(actionMsg.actionType);
                        break;
                    }
                }
            }
        }
    }
}
