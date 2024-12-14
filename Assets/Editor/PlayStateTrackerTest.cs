using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

public class PlayStateTrackerTest
{
    private static string TAG = "PlayStateTrackerTest";

    private PlayStateTracker tracker;

    [SetUp]
    public void SetUp()
    {
        KLog.I(TAG, "SetUp");
        tracker = new PlayStateTracker();
    }

    [TearDown]
    public void Teardown()
    {
        KLog.I(TAG, "Teardown");
    }

    private void MockGameState(List<int> setResultList)
    {
        int setNum = setResultList.Count;
        Action<bool> MockPass = (bool selfFirst) => {
            if (selfFirst) {
                tracker.Pass(true);
                tracker.Pass(false);
            } else {
                tracker.Pass(false);
                tracker.Pass(true);
            }
        };
        Action<int> MockSet = (int setResult) => {
            if (setResult == 1) {
                tracker.SetFinish(10, 5);
            } else if (setResult == -1) {
                tracker.SetFinish(5, 10);
            } else {
                tracker.SetFinish(5, 5);
            }
        };
        // player视角，方便测试
        tracker.TransState(PlayStateTracker.State.WAIT_INIT_HAND_CARD);
        tracker.TransState(PlayStateTracker.State.DOING_INIT_HAND_CARD);
        tracker.TransState(PlayStateTracker.State.WAIT_START);
        tracker.StartGamePlayer(false); // 设置的是host first
        // 第一局
        MockPass(tracker.setRecordList[0].selfFirst);
        MockSet(setResultList[0]);
        if (setNum == 1) {
            return;
        }
        // 第二局
        MockPass(tracker.setRecordList[1].selfFirst);
        MockSet(setResultList[1]);
        if (tracker.curState == PlayStateTracker.State.STOP || setNum == 2) {
            return;
        }
        // 第三局
        MockPass(tracker.setRecordList[2].selfFirst);
        MockSet(setResultList[2]);
    }

    [Test]
    public void SetFinishSelfWin()
    {
        List<int> setResultList = new List<int> { 1 };
        MockGameState(setResultList);
        Assert.AreEqual(true, tracker.setRecordList[0].selfFirst);
        Assert.AreEqual(1, tracker.setRecordList[0].result);
        Assert.AreEqual(10, tracker.setRecordList[0].selfScore);
        Assert.AreEqual(5, tracker.setRecordList[0].enemyScore);
        Assert.AreEqual(false, tracker.setRecordList[1].selfFirst);
        Assert.AreEqual(1, tracker.curSet);
        Assert.AreEqual(1, tracker.selfSetScore);
        Assert.AreEqual(0, tracker.enemySetScore);
    }

    [Test]
    public void SetFinishSelfLose()
    {
        List<int> setResultList = new List<int> { -1 };
        MockGameState(setResultList);
        Assert.AreEqual(true, tracker.setRecordList[0].selfFirst);
        Assert.AreEqual(-1, tracker.setRecordList[0].result);
        Assert.AreEqual(5, tracker.setRecordList[0].selfScore);
        Assert.AreEqual(10, tracker.setRecordList[0].enemyScore);
        Assert.AreEqual(true, tracker.setRecordList[1].selfFirst);
        Assert.AreEqual(1, tracker.curSet);
        Assert.AreEqual(0, tracker.selfSetScore);
        Assert.AreEqual(1, tracker.enemySetScore);
    }

    [Test]
    public void SetFinishDraw()
    {
        List<int> setResultList = new List<int> { 0 };
        MockGameState(setResultList);
        Assert.AreEqual(true, tracker.setRecordList[0].selfFirst);
        Assert.AreEqual(0, tracker.setRecordList[0].result);
        Assert.AreEqual(5, tracker.setRecordList[0].selfScore);
        Assert.AreEqual(5, tracker.setRecordList[0].enemyScore);
        Assert.AreEqual(false, tracker.setRecordList[1].selfFirst);
        Assert.AreEqual(1, tracker.curSet);
        Assert.AreEqual(1, tracker.selfSetScore);
        Assert.AreEqual(1, tracker.enemySetScore);
    }

    // 以下测试后缀：W胜，L负，D平
    [Test]
    public void GameFinishWW()
    {
        List<int> setResultList = new List<int> { 1, 1 };
        MockGameState(setResultList);
        Assert.AreEqual(PlayStateTracker.State.STOP, tracker.curState);
        Assert.AreEqual(true, tracker.isSelfWinner);
        Assert.AreEqual(2, tracker.selfSetScore);
        Assert.AreEqual(0, tracker.enemySetScore);
    }

    [Test]
    public void GameFinishWD()
    {
        List<int> setResultList = new List<int> { 1, 0 };
        MockGameState(setResultList);
        Assert.AreEqual(PlayStateTracker.State.STOP, tracker.curState);
        Assert.AreEqual(true, tracker.isSelfWinner);
        Assert.AreEqual(2, tracker.selfSetScore);
        Assert.AreEqual(1, tracker.enemySetScore);
    }

    [Test]
    public void GameFinishWLW()
    {
        List<int> setResultList = new List<int> { 1, -1, 1 };
        MockGameState(setResultList);
        Assert.AreEqual(PlayStateTracker.State.STOP, tracker.curState);
        Assert.AreEqual(true, tracker.isSelfWinner);
        Assert.AreEqual(2, tracker.selfSetScore);
        Assert.AreEqual(1, tracker.enemySetScore);
    }

    [Test]
    public void GameFinishWLD()
    {
        // 三局先手分别为self self enemy
        List<int> setResultList = new List<int> { 1, -1, 0 };
        MockGameState(setResultList);
        Assert.AreEqual(PlayStateTracker.State.STOP, tracker.curState);
        Assert.AreEqual(true, tracker.isSelfWinner);
    }

    [Test]
    public void GameFinishWLL()
    {
        List<int> setResultList = new List<int> { 1, -1, -1 };
        MockGameState(setResultList);
        Assert.AreEqual(PlayStateTracker.State.STOP, tracker.curState);
        Assert.AreEqual(false, tracker.isSelfWinner);
        Assert.AreEqual(1, tracker.selfSetScore);
        Assert.AreEqual(2, tracker.enemySetScore);
    }

    [Test]
    public void GameFinishDW()
    {
        List<int> setResultList = new List<int> { 0, 1 };
        MockGameState(setResultList);
        Assert.AreEqual(PlayStateTracker.State.STOP, tracker.curState);
        Assert.AreEqual(true, tracker.isSelfWinner);
        Assert.AreEqual(2, tracker.selfSetScore);
        Assert.AreEqual(1, tracker.enemySetScore);
    }

    [Test]
    public void GameFinishDDW()
    {
        List<int> setResultList = new List<int> { 0, 0, 1 };
        MockGameState(setResultList);
        Assert.AreEqual(PlayStateTracker.State.STOP, tracker.curState);
        Assert.AreEqual(true, tracker.isSelfWinner);
        Assert.AreEqual(3, tracker.selfSetScore);
        Assert.AreEqual(2, tracker.enemySetScore);
    }

    [Test]
    public void GameFinishDDD()
    {
        // 三局先手分别为self enemy self
        List<int> setResultList = new List<int> { 0, 0, 0 };
        MockGameState(setResultList);
        Assert.AreEqual(PlayStateTracker.State.STOP, tracker.curState);
        Assert.AreEqual(true, tracker.isSelfWinner);
        Assert.AreEqual(3, tracker.selfSetScore);
        Assert.AreEqual(3, tracker.enemySetScore);
    }

    [Test]
    public void GameFinishDDL()
    {
        List<int> setResultList = new List<int> { 0, 0, -1 };
        MockGameState(setResultList);
        Assert.AreEqual(PlayStateTracker.State.STOP, tracker.curState);
        Assert.AreEqual(false, tracker.isSelfWinner);
        Assert.AreEqual(2, tracker.selfSetScore);
        Assert.AreEqual(3, tracker.enemySetScore);
    }

    [Test]
    public void GameFinishDL()
    {
        List<int> setResultList = new List<int> { 0, -1 };
        MockGameState(setResultList);
        Assert.AreEqual(PlayStateTracker.State.STOP, tracker.curState);
        Assert.AreEqual(false, tracker.isSelfWinner);
        Assert.AreEqual(1, tracker.selfSetScore);
        Assert.AreEqual(2, tracker.enemySetScore);
    }

    [Test]
    public void GameFinishLWW()
    {
        List<int> setResultList = new List<int> { -1, 1, 1 };
        MockGameState(setResultList);
        Assert.AreEqual(PlayStateTracker.State.STOP, tracker.curState);
        Assert.AreEqual(true, tracker.isSelfWinner);
        Assert.AreEqual(2, tracker.selfSetScore);
        Assert.AreEqual(1, tracker.enemySetScore);
    }

    [Test]
    public void GameFinishLWD()
    {
        // 三局先手分别为self enemy self
        List<int> setResultList = new List<int> { -1, 1, 0 };
        MockGameState(setResultList);
        Assert.AreEqual(PlayStateTracker.State.STOP, tracker.curState);
        Assert.AreEqual(true, tracker.isSelfWinner);
        Assert.AreEqual(2, tracker.selfSetScore);
        Assert.AreEqual(2, tracker.enemySetScore);
    }

    [Test]
    public void GameFinishLWL()
    {
        List<int> setResultList = new List<int> { -1, 1, -1 };
        MockGameState(setResultList);
        Assert.AreEqual(PlayStateTracker.State.STOP, tracker.curState);
        Assert.AreEqual(false, tracker.isSelfWinner);
        Assert.AreEqual(1, tracker.selfSetScore);
        Assert.AreEqual(2, tracker.enemySetScore);
    }

    [Test]
    public void GameFinishLD()
    {
        List<int> setResultList = new List<int> { -1, 0 };
        MockGameState(setResultList);
        Assert.AreEqual(PlayStateTracker.State.STOP, tracker.curState);
        Assert.AreEqual(false, tracker.isSelfWinner);
        Assert.AreEqual(1, tracker.selfSetScore);
        Assert.AreEqual(2, tracker.enemySetScore);
    }

    [Test]
    public void GameFinishLL()
    {
        List<int> setResultList = new List<int> { -1, -1 };
        MockGameState(setResultList);
        Assert.AreEqual(PlayStateTracker.State.STOP, tracker.curState);
        Assert.AreEqual(false, tracker.isSelfWinner);
        Assert.AreEqual(0, tracker.selfSetScore);
        Assert.AreEqual(2, tracker.enemySetScore);
    }
}
