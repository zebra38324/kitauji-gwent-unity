import { KLog } from './util/klog.js';
// 用户上下文
export class UserContext
{
    #TAG = 'UserContext';
    #wsConn;
    #hasLogin = false;

    #ApiMap = {
        1: this.#Login,
        2: this.#GetDeckConfig,
    }

    constructor(wsConn, loginCallback)
    {
        this.#wsConn = wsConn;
        this.#wsConn.on('message', async (message) => {
            const hasLoginOld = this.#hasLogin;
            this.#ReceiveMsg(message);
            if (!hasLoginOld && this.#hasLogin) {
                loginCallback();
            }
        });
    }

    #ReceiveMsg(message)
    {
        var sessionId = -1;
        try {
            // 请求格式：{"sessionId":1,"data":[byte数组]}
            const jsonData = JSON.parse(message);
            sessionId = jsonData.sessionId;
            const sessionDataStr = Buffer.from(jsonData.data).toString('utf-8');

            // 请求格式：{"apiType":1,"msg":""}
            KLog.I(this.#TAG, sessionDataStr);
            const apiData = JSON.parse(sessionDataStr);
            const apiType = apiData.apiType;
            const handler = this.#ApiMap[apiType];
            if (handler && typeof handler === 'function') {
                const resContent = JSON.stringify({
                    sessionId: sessionId,
                    data: Array.from(Buffer.from(JSON.stringify(handler(apiData.msg))))
                });
                this.#wsConn.send(resContent);
            } else {
                throw new Error(`invalid type: ${apiType}`);
            }
        } catch (error) {
            KLog.E(this.#TAG, error);
            const resContent = JSON.stringify({
                sessionId: sessionId,
                data: Array.from(Buffer.from("error"))
            });
            this.#wsConn.send(resContent);
            this.#wsConn.close();
        }
    }

    #Login(loginReq)
    {
        // 请求格式：{"isTourist":true,"name":"","password":""}
        const resJson = {
            result: true,
            userId: 1234
        }
        return resJson;
    }
    
    #GetDeckConfig(deckConfigReq)
    {
        // 请求格式：{"userId":1234}
        const config = {
          "infoIdList": [2001,2002,2003,2004,2005,2006,2007,2036,2037,2080,5009]
        }
        return config;
    }
    
    #Sleep(ms)
    {
        return new Promise(resolve => setTimeout(resolve, ms));
    }
}
