using System.Collections.Generic;
using System.Text;
using UnityEngine;
using NativeWebSocket;
using System.Collections.Concurrent;
using Cysharp.Threading.Tasks;

/**
 * kitauji network
 * 全局单例，每个模块调用时维护自己的一份session id
 * 使用方式：
 * 1. CreateSession。创建session，并获取session id
 * 2. Send。发送指定session的NetMsg
 * 3. Recv。接收指定session的NetMsg
 * 4. CloseSession。关闭session
 */
public class KNetwork : MonoBehaviour
{
    private string TAG = "KNetwork";

    public static KNetwork Instance;

    private WebSocket websocket;

    private ConcurrentQueue<NetMsg> sendQueue; // 发送队列
    private Dictionary<int, ConcurrentQueue<NetMsg>> receiveQueueDict; // 接收队列集合

    private int currentMaxSession = 1;

    private bool isAbort = false;

    public class NetMsg
    {
        public int sessionId; // 网络会话id
        public byte[] data;
    }

    void Start()
    {
        KLog.I(TAG, "will init KNetwork");
        DontDestroyOnLoad(gameObject);
        Instance = this;
        Connect();
        KLog.I(TAG, "finish init KNetwork");
    }

    void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        websocket.DispatchMessageQueue();
#endif
    }

    public int CreateSession()
    {
        int sessionId = 0;
        lock (this) {
            sessionId = currentMaxSession;
            currentMaxSession += 1;
            receiveQueueDict[sessionId] = new ConcurrentQueue<NetMsg>();
        }
        return sessionId;
    }

    public void Send(NetMsg netMsg)
    {
        lock (this) {
            sendQueue.Enqueue(netMsg);
        }
    }

    public NetMsg Receive(int sessionId)
    {
        NetMsg netMsg = null;
        lock (this) {
            var receiveQueue = receiveQueueDict[sessionId];
            if (receiveQueue.Count == 0) {
                return netMsg;
            }
            if (!receiveQueue.TryDequeue(out netMsg)) {
                return netMsg;
            }
            return netMsg;
        }
    }

    public void CloseSession(int sessionId)
    {
        lock (this) {
            receiveQueueDict.Remove(sessionId);
        }
    }

    private async void OnApplicationQuit()
    {
        KLog.I(TAG, "OnApplicationQuit");
        isAbort = true;
        await websocket.Close();
        websocket = null;
    }

    private async void Connect()
    {
        sendQueue = new ConcurrentQueue<NetMsg>();
        receiveQueueDict = new Dictionary<int, ConcurrentQueue<NetMsg>>();
        websocket = new WebSocket("ws://localhost:12323/kitauji_api");

        websocket.OnOpen += () => {
            KLog.I(TAG, "Connection open");
            SendCoroutine();
        };

        websocket.OnError += (e) => {
            KLog.E(TAG, "Error: " + e);
        };

        websocket.OnClose += (e) => {
            KLog.I(TAG, "Connection closed");
        };

        websocket.OnMessage += (bytes) => {
            ReceiveInternal(bytes);
        };

        await websocket.Connect();
    }

    private async void SendCoroutine()
    {
        KLog.I(TAG, "SendCoroutine start");
        while (!isAbort) {
            await SendInternal();
            await UniTask.Delay(1);
        }
        KLog.I(TAG, "SendCoroutine end");
    }

    private async UniTask SendInternal()
    {
        while (true) {
            NetMsg netMsg = null;
            lock (this) {
                if (sendQueue.Count == 0) {
                    return;
                }
                if (!sendQueue.TryDequeue(out netMsg)) {
                    return;
                }
            }
            string netMsgStr = JsonUtility.ToJson(netMsg);
            await websocket.SendText(netMsgStr);
        }
    }

    private void ReceiveInternal(byte[] data)
    {
        string netMsgStr = Encoding.Default.GetString(data);
        NetMsg netMsg = JsonUtility.FromJson<NetMsg>(netMsgStr);
        lock (this) {
            receiveQueueDict[netMsg.sessionId].Enqueue(netMsg);
        }
    }
}
