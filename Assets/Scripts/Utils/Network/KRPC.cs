using System.Collections.Generic;
using System.Text;
using UnityEngine;
using NativeWebSocket;
using System.Collections.Concurrent;
using Cysharp.Threading.Tasks;

/**
 * kitauji rpc
 * 对于KNetwork的一层封装
 * 全局单例，每个模块调用时维护自己的一份session id
 * 使用方式：
 * 1. CreateSession。创建session，并获取session id
 * 2. Send。发送指定session与指定api类型的json字符串
 * 3. Recv。接收指定session的json字符串
 * 4. CloseSession。关闭session
 */
public class KRPC : MonoBehaviour
{
    private string TAG = "KRPC";

    public static KRPC Instance;

    public enum ApiType
    {
        Login = 1, // 登录
        GetDeckConfig, // 获取配置的牌组
    }

    private class ApiData
    {
        public int apiType;
        public string msg;
    }

    void Start()
    {
        KLog.I(TAG, "will init KRPC");
        Instance = this;
        KLog.I(TAG, "finish init KRPC");
    }

    void Update()
    {

    }

    public int CreateSession()
    {
        return KNetwork.Instance.CreateSession();
    }

    public void Send(int sessionId, ApiType apiType, string msg)
    {
        ApiData apiData = new ApiData();
        apiData.apiType = (int)apiType;
        apiData.msg = msg;
        string jsonStr = JsonUtility.ToJson(apiData);
        byte[] byteArray = Encoding.Default.GetBytes(jsonStr);

        KNetwork.NetMsg netMsg = new KNetwork.NetMsg();
        netMsg.sessionId = sessionId;
        netMsg.data = byteArray;
        KNetwork.Instance.Send(netMsg);
    }

    public string Receive(int sessionId)
    {
        KNetwork.NetMsg netMsg = KNetwork.Instance.Receive(sessionId);
        if (netMsg == null) {
            return null;
        }
        string receiveStr = Encoding.Default.GetString(netMsg.data);
        return receiveStr;
    }

    public void CloseSession(int sessionId)
    {
        KNetwork.Instance.CloseSession(sessionId);
    }
}
