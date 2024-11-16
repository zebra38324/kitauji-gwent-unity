using System.Threading;
using UnityEngine;

public enum BattleStatus
{
    WaitingStart = 0, // 还未开始，或单局结束
    SelfTurn,
    EnemyTurn,
    Stop,
}

// 用于记录每回合的玩家操作
/*
用于记录每回合的玩家操作
玩家可能的操作：
1. pass
2. 打出一张牌
    a. 普通牌，无特殊
    b. 有技能，选择了另一个牌生效技能
 */
public struct BattleAction
{
    public bool isPass; // 玩家是否点了pass
    public CardInfo selected; // 点击的牌
    public CardInfo extend; // 后续操作的牌
}

// 对战统一管理接口，包括人机对战与网络联机
// 实例由PlaySceneManager持有
public class BattleManager
{
    private static string TAG = "BattleManager";
    private BattleStatus curStatus = BattleStatus.WaitingStart;
    private bool isAbort = false;
    private Thread loopThread = null;

    // 对方做出操作后，回调通知PlaySceneManager
    public delegate void EnemyActionNotifyHandler(BattleAction action);
    public event EnemyActionNotifyHandler EnemyActionNotify;

    public BattleManager() {}

    // 重置并启动
    public void Start()
    {
        if (loopThread != null) {
            loopThread.Abort();
            loopThread = null;
        }
        isAbort = false;
        curStatus = BattleStatus.WaitingStart;
        loopThread = new Thread(() =>
        {
            StatusPolling();
        });
        loopThread.Start();
    }

    public BattleStatus GetCurStatus()
    {
        return curStatus;
    }

    public void FinishSelfTurn(BattleAction action)
    {
        if (curStatus != BattleStatus.SelfTurn) {
            return;
        }
        // curStatus = BattleStatus.EnemyTurn;
        // TODO: network
    }

    // 结束单局比赛
    public void FinishSingleBattle()
    {
        curStatus = BattleStatus.WaitingStart;
        // TODO: network
    }

    // 结束整场比赛
    public void FinishBattle()
    {
        curStatus = BattleStatus.Stop;
        isAbort = true;
        // TODO: network
        if (loopThread != null) {
            loopThread.Join();
            loopThread = null;
        }
    }

    // 状态轮询
    private void StatusPolling()
    {
        while (!isAbort) {
            // TODO: network

            // recieve action
            BattleAction action = new BattleAction();
            EnemyActionNotify(action);
            curStatus = BattleStatus.SelfTurn;
            Thread.Sleep(1000);
            KLog.I(TAG, "StatusPolling");
        }
    }
}
