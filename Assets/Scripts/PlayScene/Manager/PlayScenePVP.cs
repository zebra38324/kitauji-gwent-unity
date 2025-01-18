using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json.Linq;

// PVP对战接口，由PlaySceneManager调用
public class PlayScenePVP
{
    private string TAG = "PlayScenePVP";

    private BattleModel battleModel;

    private int sessionId;

    private bool isAbort = false;

    public PlayScenePVP(BattleModel battleModelParam, int sessionIdParam)
    {
        battleModel = battleModelParam;
        sessionId = sessionIdParam;
    }

    public void Release()
    {
        isAbort = true;
    }

    public void Start()
    {
        battleModel.SendToEnemyFunc += SendSelfActionMsg;
        ReceiveCoroutine();
    }

    public void SendStopMsg()
    {
        KLog.I(TAG, "SendStopMsg");
        KRPC.Instance.Send(sessionId, KRPC.ApiType.pvp_match_stop, "{}");
    }

    private async void ReceiveCoroutine()
    {
        while (!isAbort) {
            string receiveStr = KRPC.Instance.Receive(sessionId);
            if (receiveStr == null) {
                await UniTask.Delay(1);
                continue;
            }
            JObject receiveJson = JObject.Parse(receiveStr);
            KLog.I(TAG, "ReceiveCoroutine: Receive: " + receiveStr);
            if (receiveJson["status"]?.ToString() == KRPC.ApiRetStatus.success.ToString()) {
                continue;
            } else if (receiveJson["status"]?.ToString() == KRPC.ApiRetStatus.error.ToString()) {
                continue;
            }
            string actionMsgStr = receiveJson["action"]?.ToString();
            if (actionMsgStr == "stop") {
                KLog.I(TAG, "ReceiveCoroutine: stop");
                PlaySceneManager.Instance.HandleMessage(SceneMsg.PVPEnemyExit);
                break;
            }
            battleModel.AddEnemyActionMsg(actionMsgStr);
        }
    }

    private void SendSelfActionMsg(string actionMsgStr)
    {
        JObject obj = new JObject();
        obj.Add("action", actionMsgStr);
        string reqStr = obj.ToString();
        KLog.I(TAG, "SendSelfActionMsg: reqStr = " + reqStr);
        KRPC.Instance.Send(sessionId, KRPC.ApiType.pvp_match_action, reqStr);
    }
}
