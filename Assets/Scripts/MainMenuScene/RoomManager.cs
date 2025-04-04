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

    public void StartPVE()
    {
        PlayerPrefs.SetString(PlayerPrefsKey.PLAY_SCENE_SELF_NAME.ToString(), KConfig.Instance.playerName);
        PlayerPrefs.SetString(PlayerPrefsKey.PLAY_SCENE_ENEMY_NAME.ToString(), "北宇治B编");
        PlayerPrefs.SetInt(PlayerPrefsKey.PLAY_SCENE_SELF_GROUP.ToString(), (int)KConfig.Instance.deckCardGroup);
        PlayerPrefs.SetInt(PlayerPrefsKey.PLAY_SCENE_IS_HOST.ToString(), 1);
        PlayerPrefs.SetInt(PlayerPrefsKey.PLAY_SCENE_IS_PVP.ToString(), 0);
        PlayerPrefs.SetInt(PlayerPrefsKey.PLAY_SCENE_PVE_AI_TYPE.ToString(), (int)PlaySceneAI.AIType.K2Basic);
        PlayerPrefs.Save();
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
                    PlayerPrefs.SetString(PlayerPrefsKey.PLAY_SCENE_SELF_NAME.ToString(), KConfig.Instance.playerName);
                    PlayerPrefs.SetString(PlayerPrefsKey.PLAY_SCENE_ENEMY_NAME.ToString(), receiveJson["opponent"]?.ToString());
                    PlayerPrefs.SetInt(PlayerPrefsKey.PLAY_SCENE_SELF_GROUP.ToString(), (int)KConfig.Instance.deckCardGroup);
                    PlayerPrefs.SetInt(PlayerPrefsKey.PLAY_SCENE_IS_HOST.ToString(), Convert.ToInt32(receiveJson["isHost"].ToObject<bool>()));
                    PlayerPrefs.SetInt(PlayerPrefsKey.PLAY_SCENE_IS_PVP.ToString(), 1);
                    PlayerPrefs.SetInt(PlayerPrefsKey.PLAY_SCENE_PVP_SESSION_ID.ToString(), sessionId);
                    PlayerPrefs.Save();
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
