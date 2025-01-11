import { BuildRes, ApiTypeEnum } from '../util/message_util.js';

export const ConfigRoutes = {
    [ApiTypeEnum.CONFIG_DECK_GET]: (ws, sessionId) => {
        // 请求格式：{}
        // 返回格式：{"status": "success", "deck": [int数组]}
        ws.send(BuildRes(sessionId, {status:"success", deck:[2001,2002,2003,2004,2005,2006,2007,2036,2037,2080,5009]}));
    }
}
