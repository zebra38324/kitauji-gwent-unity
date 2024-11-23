using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

public class BattleModelTest
{
    private static string TAG = "BattleModelTest";

    private BattleModel battleModel;

    private List<int> testInfoIdList;

    private List<int> testIdList;

    private string testActionMsgStr;

    [SetUp]
    public void SetUp()
    {
        KLog.I(TAG, "SetUp");
        battleModel = new BattleModel();
        testInfoIdList = null;
        testIdList = null;
        testActionMsgStr = null;
        battleModel.SendToEnemyFunc += SendToEnemyFuncTest;
        battleModel.EnemyMsgCallback += EnemyMsgCallbackTest;
    }

    [TearDown]
    public void Teardown()
    {
        KLog.I(TAG, "Teardown");
    }

    private void SendToEnemyFuncTest(string actionMsgStr)
    {
        testActionMsgStr = actionMsgStr;
    }

    public void EnemyMsgCallbackTest(BattleModel.ActionType actionType, params object[] list)
    {
        switch (actionType) {
            case BattleModel.ActionType.Init: {
                testInfoIdList = (List<int>)list[0];
                testIdList = (List<int>)list[1];
                break;
            }
            case BattleModel.ActionType.DrawHandCard: {
                testIdList = (List<int>)list[0];
                break;
            }
            case BattleModel.ActionType.ChooseCard: {
                testIdList = new List<int> { (int)list[0] };
                break;
            }
            case BattleModel.ActionType.Pass: {
                break;
            }
        }
    }

    // 测试初始化
    [Test]
    public void Init()
    {
        List<int> infoIdList = new List<int> { 2001, 2002 };
        List<int> idList = new List<int> { 11, 21 };
        battleModel.AddSelfActionMsg(BattleModel.ActionType.Init, infoIdList, idList);
        Thread.Sleep(30); // 等待异步执行完成

        // 消息原路返回，测试序列化、反序列化是否正常
        Assert.AreNotEqual(null, testActionMsgStr);
        battleModel.AddEnemyctionMsg(testActionMsgStr);
        Thread.Sleep(30); // 等待异步执行完成
        Assert.AreEqual(2, testInfoIdList.Count);
        Assert.AreEqual(infoIdList[0], testInfoIdList[0]);
        Assert.AreEqual(infoIdList[1], testInfoIdList[1]);
        Assert.AreEqual(2, testIdList.Count);
        Assert.AreEqual(idList[0], testIdList[0]);
        Assert.AreEqual(idList[1], testIdList[1]);
    }
}
