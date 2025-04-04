import { BuildRes, ApiTypeEnum } from '../util/message_util.js';
import { AuthUser, StatLogin } from '../database/database.js';

const touristNameList = ["黄前久美子", "铠冢霙", "伞木希美", "吉川优子", "高坂丽奈", "久石奏", "川岛绿辉"];
let uniqueTouristId = 1;

export const AuthRoutes = {
    [ApiTypeEnum.AUTH_LOGIN]: (ws, activeConns, sessionId, apiArgs) => {
        // 请求格式：{"isTourist":true,"username":"","password":""}
        // isTourist=true：username为空，password为空
        // isTourist=false：username正常填写，password在客户端进行hash后再传输
        // 游客名前加Tourist前缀，与注册用户严格区分
        // 返回格式：{"status": "success", "username": "name"}
        // isTourist=true时，username由服务端指定，保证全局唯一
        if (apiArgs.isTourist) {
            ws.user = { username: `Tourist-${touristNameList[Math.floor(Math.random() * touristNameList.length)]}-${uniqueTouristId}`, isTourist: true };
            uniqueTouristId += 1;
            activeConns.set(ws.user.username, ws);
            ws.send(BuildRes(sessionId, {status:"success", username:ws.user.username}));
        } else {
            const result = AuthUser(apiArgs.username, apiArgs.password);
            if (!result.success) {
                ws.send(BuildRes(sessionId, {status:"error", message:result.message}));
                return;
            }
            // 禁止顶号
            if (activeConns.has(apiArgs.username)) {
                ws.send(BuildRes(sessionId, {status:"error", message:"此用户已登录，请先退出再重试"}));
                return;
            }
            ws.user = { username: apiArgs.username, isTourist: false };
            activeConns.set(ws.user.username, ws);
            ws.send(BuildRes(sessionId, {status:"success", username:ws.user.username}));
        }
        StatLogin(ws.connId, ws.user.username, activeConns.size);
    }
}
