import { BuildRes, ApiTypeEnum } from '../util/message_util.js';
import { RegisterUser } from '../database/database.js';

export const RegisterRoutes = {
    [ApiTypeEnum.REGISTER]: (ws, sessionId, apiArgs) => {
        // 请求格式：{"username":"","password":""}
        // username正常填写，不可以Tourist开头。password在客户端可选择是否hash后再传输
        // 返回格式：
        // {"status": "success"}
        // {"status": "error", "message": ""}
        if (apiArgs.username.startsWith("Tourist")) {
            ws.send(BuildRes(sessionId, {status:"error", message:"username invalid"}));
        } else {
            const result = RegisterUser(apiArgs.username, apiArgs.password);
            if (result.success) {
                ws.send(BuildRes(sessionId, {status:"success"}));
            } else {
                ws.send(BuildRes(sessionId, {status:"error", message:result.message}));
            }
        }
    }
}

