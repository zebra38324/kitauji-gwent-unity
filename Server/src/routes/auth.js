import { BuildRes, ApiTypeEnum } from '../util/message_util.js';

let uniqueTouristId = 1;

export const AuthRoutes = {
    [ApiTypeEnum.AUTH_LOGIN]: (ws, activeConns, sessionId, apiArgs) => {
        // 请求格式：{"isTourist":true,"username":"","password":""}
        // isTourist=true：username为空，password为空
        // isTourist=false：username正常填写，password在客户端进行hash后再传输
        // 返回格式：{"status": "success", "username": "name"}
        // isTourist=true时，username由服务端指定，保证全局唯一
        if (apiArgs.isTourist) {
            ws.user = {username:`TestName-${uniqueTouristId}`};
            uniqueTouristId += 1;
            activeConns.set(ws.user.username, ws);
            ws.send(BuildRes(sessionId, {status:"success", username:ws.user.username}));
        } else {
            ws.send(BuildRes(sessionId, {status:"error", message:"invalid args"}));
        }
    }
}
