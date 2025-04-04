using Cysharp.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 心跳信息的收发
public class KHeartbeat
{
    private string TAG = "KHeartbeat";

    public static readonly KHeartbeat Instance = new KHeartbeat();

    private int sessionId;

    private string lastReceiveStr = null;

    public enum UserStatus
    {
        IDLE = 0,
        PVP_MATCHING,
        PVP_GAMING,
        PVE_GAMING,
        COUNT,
    }

    public void Init()
    {
        sessionId = KRPC.Instance.CreateSession();
        SendHeartbeat(UserStatus.IDLE);
    }

    [Serializable]
    private class HeartbeatReq
    {
        public UserStatus user_status;
    }

    public void SendHeartbeat(UserStatus userStatus)
    {
        KLog.I(TAG, "SendHeartbeat: " + userStatus);
        HeartbeatReq heartbeatReq = new HeartbeatReq();
        heartbeatReq.user_status = userStatus;
        string heartbeatReqStr = JsonUtility.ToJson(heartbeatReq);
        KRPC.Instance.Send(sessionId, KRPC.ApiType.heartbeat, heartbeatReqStr);
    }

    // 接收当前心跳信息，目前为一个数组，长度为UserStatus.COUNT
    public List<int> RecvHeartbeat()
    {
        List<int> result = null;
        // 返回格式：{"all_users":[1,1,0,2]}
        // 只看最新的一个
        while (true) {
            string receiveStr = KRPC.Instance.Receive(sessionId);
            if (receiveStr == null) {
                break;
            }
            KLog.I(TAG, "RecvHeartbeat: Receive: " + receiveStr);
            lastReceiveStr = receiveStr;
        }
        if (lastReceiveStr == null) {
            return result;
        }
        JObject resJson = JObject.Parse(lastReceiveStr);
        result = ((JArray)resJson["all_users"]).ToObject<List<int>>();
        return result;
    }
}
