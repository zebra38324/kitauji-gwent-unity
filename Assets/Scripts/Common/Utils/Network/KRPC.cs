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

    // 请求格式：{"apiType":"","apiArgs":""}
    public enum ApiType
    {
        register = 1, // 注册。请求格式：{"username":"","password":""}
        auth_login, // 登录。请求格式：{"isTourist":true,"username":"","password":""}
        config_deck_get, // 获取配置的牌组。请求格式：{}
        config_deck_update, // 更新配置牌组。请求格式：{"deck": { "group": 0, "config": [[int数组], [int数组]]}}
        config_competition_get, // 获取竞赛模式进度。请求格式：{}
        config_competition_update, // 更新竞赛模式进度。请求格式：{"competition_config": "json_str"}
        pvp_match_start, // 请求格式：{}
        pvp_match_cancel, // 请求格式：{}
        pvp_match_action, // 请求格式：{"action":""}
        pvp_match_stop, // 请求格式：{}
        heartbeat, // 心跳，格式：{"user_status":0}
    }

    public enum ApiRetStatus
    {
        success = 1,
        error,
        waiting,
    }

    private class ApiData
    {
        public string apiType;
        public string apiArgs;
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

    public void Send(int sessionId, ApiType apiType, string apiArgs)
    {
        ApiData apiData = new ApiData();
        apiData.apiType = apiType.ToString();
        apiData.apiArgs = apiArgs;
        string jsonStr = JsonUtility.ToJson(apiData);
        byte[] byteArray = Encoding.Default.GetBytes(jsonStr);

        KNetwork.NetMsg netMsg = new KNetwork.NetMsg();
        netMsg.sessionId = sessionId;
        netMsg.sessionData = byteArray;
        KNetwork.Instance.Send(netMsg);
    }

    public string Receive(int sessionId)
    {
        KNetwork.NetMsg netMsg = KNetwork.Instance.Receive(sessionId);
        if (netMsg == null) {
            return null;
        }
        string receiveStr = Encoding.Default.GetString(netMsg.sessionData);
        return receiveStr;
    }

    public void CloseSession(int sessionId)
    {
        KNetwork.Instance.CloseSession(sessionId);
    }
}
