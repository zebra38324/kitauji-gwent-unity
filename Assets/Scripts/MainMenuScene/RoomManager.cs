using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Newtonsoft.Json.Linq;
using Cysharp.Threading.Tasks;

public class RoomManager
{
    private string TAG = "RoomManager";

    public RoomManager()
    {
    }

    public void StartPVE(PlaySceneAI.AIType aiType)
    {
        GameConfig.Instance.Reset();
        var gameConfig = GameConfig.Instance;
        gameConfig.selfName = KConfig.Instance.playerName;
        gameConfig.enemyName = "北宇治B编";
        gameConfig.selfGroup = KConfig.Instance.deckCardGroup;
        gameConfig.isHost = true;
        gameConfig.isPVP = false;
        gameConfig.pveAIType = aiType;
        gameConfig.fromScene = "MainMenuScene";
        KHeartbeat.Instance.SendHeartbeat(KHeartbeat.UserStatus.PVE_GAMING);
        SceneManager.LoadScene("PlayScene");
    }

    public async void StartPVPMatch()
    {
        string reqStr = "{}";
        int sessionId = KRPC.Instance.CreateSession();
        KRPC.Instance.Send(sessionId, KRPC.ApiType.pvp_match_start, reqStr);
        KHeartbeat.Instance.SendHeartbeat(KHeartbeat.UserStatus.PVP_MATCHING);
        while (true) {
            string receiveStr = KRPC.Instance.Receive(sessionId);
            if (receiveStr != null) {
                KLog.I(TAG, "StartPVPMatch: Receive: " + receiveStr);
                JObject receiveJson = JObject.Parse(receiveStr);
                bool apiSuccess = receiveJson["status"]?.ToString() == KRPC.ApiRetStatus.success.ToString();
                bool needWaiting = receiveJson["status"]?.ToString() == KRPC.ApiRetStatus.waiting.ToString();
                if (needWaiting) {
                    continue;
                }
                if (apiSuccess) {
                    GameConfig.Instance.Reset();
                    var gameConfig = GameConfig.Instance;
                    gameConfig.selfName = KConfig.Instance.playerName;
                    gameConfig.enemyName = receiveJson["opponent"]?.ToString();
                    gameConfig.selfGroup = KConfig.Instance.deckCardGroup;
                    gameConfig.isHost = Convert.ToBoolean(receiveJson["isHost"]?.ToString());
                    gameConfig.isPVP = true;
                    gameConfig.pvpSessionId = sessionId;
                    KHeartbeat.Instance.SendHeartbeat(KHeartbeat.UserStatus.PVP_GAMING);
                    SceneManager.LoadScene("PlayScene");
                } else {
                    KLog.E(TAG, "StartPVPMatch: Error");
                    KHeartbeat.Instance.SendHeartbeat(KHeartbeat.UserStatus.IDLE);
                    KNetwork.Instance.CloseSession(sessionId); // 成功时不关闭，用于对局使用
                }
                break;
            }
            await UniTask.Delay(1);
        }
    }

    public async void CancelPVPMatch()
    {
        string reqStr = "{}";
        int sessionId = KRPC.Instance.CreateSession();
        KRPC.Instance.Send(sessionId, KRPC.ApiType.pvp_match_cancel, reqStr);
        KHeartbeat.Instance.SendHeartbeat(KHeartbeat.UserStatus.IDLE);
        while (true) {
            string receiveStr = KRPC.Instance.Receive(sessionId);
            if (receiveStr != null) {
                KLog.I(TAG, "CancelPVPMatch: Receive: " + receiveStr);
                JObject receiveJson = JObject.Parse(receiveStr);
                bool apiSuccess = receiveJson["status"]?.ToString() == KRPC.ApiRetStatus.success.ToString();
                if (!apiSuccess) {
                    KLog.E(TAG, "CancelPVPMatch: Error");
                }
                break;
            }
            await UniTask.Delay(1);
        }
        KNetwork.Instance.CloseSession(sessionId);
    }
}
