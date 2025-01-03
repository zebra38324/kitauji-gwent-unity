﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Cysharp.Threading.Tasks;


// kitauji config
// 全局配置读取
public class KConfig
{
    private string TAG = "KConfig";
    private static readonly KConfig instance = new KConfig();

    private int netSessionId;

    public List<int> deckInfoIdList { get; private set; }

    static KConfig() { }

    private KConfig() { }

    public static KConfig Instance {
        get {
            return instance;
        }
    }

    [Serializable]
    private class LoginReq
    {
        public bool isTourist;
        public string name;
        public string password;
    }

    [Serializable]
    private class LoginRes
    {
        public bool result;
        public int userId;
    }

    /**
     * 登录
     * isTourist: 以游客身份登录
     * callback: 返回登录结果
     * name: isTourist为false时设置
     * password: isTourist为false时设置
     */
    public async void Login(bool isTourist, Action<bool> callback, string name = null, string password = null)
    {
        LoginReq loginReq = new LoginReq();
        loginReq.isTourist = isTourist;
        loginReq.name = name;
        loginReq.password = password;
        string loginReqStr = JsonUtility.ToJson(loginReq);
        KLog.I(TAG, "Login: loginReqStr = " + loginReqStr);
        int sessionId = KRPC.Instance.CreateSession();
        KRPC.Instance.Send(sessionId, KRPC.ApiType.Login, loginReqStr);
        while (true) {
            string receiveStr = KRPC.Instance.Receive(sessionId);
            if (receiveStr != null) {
                LoginRes loginRes = JsonUtility.FromJson<LoginRes>(receiveStr);
                KLog.I(TAG, "Receive: " + JsonUtility.ToJson(loginRes));
                if (callback != null) {
                    callback(loginRes.result);
                }
                if (loginRes.result) {
                    GetDeckInfoIdList(loginRes.userId);
                }
                break;
            }
            await UniTask.Delay(1);
        }
    }

    [Serializable]
    private class DeckConfigReq
    {
        public int userId;
    }

    [Serializable]
    private class DeckConfig
    {
        public List<int> infoIdList;
    }

    public async void GetDeckInfoIdList(int userId)
    {
        DeckConfigReq deckConfigReq = new DeckConfigReq();
        deckConfigReq.userId = 1234;
        string deckConfigReqStr = JsonUtility.ToJson(deckConfigReq);
        netSessionId = KRPC.Instance.CreateSession();
        KRPC.Instance.Send(netSessionId, KRPC.ApiType.GetDeckConfig, deckConfigReqStr);
        while (true) {
            string receiveStr = KRPC.Instance.Receive(netSessionId);
            if (receiveStr != null) {
                DeckConfig deckConfig = new DeckConfig();
                deckConfig = JsonUtility.FromJson<DeckConfig>(receiveStr);
                deckInfoIdList = deckConfig.infoIdList;
                KLog.I(TAG, "Receive: " + JsonUtility.ToJson(deckConfig));
                break;
            }
            await UniTask.Delay(1);
        }
        KNetwork.Instance.CloseSession(netSessionId);
    }
}