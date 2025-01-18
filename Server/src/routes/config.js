import { BuildRes, ApiTypeEnum } from '../util/message_util.js';

export const ConfigRoutes = {
    [ApiTypeEnum.CONFIG_DECK_GET]: (ws, sessionId) => {
        // 请求格式：{}
        // 返回格式：{"status": "success", "deck": [int数组]}
        let deckConfig = [];
        // wood
        deckConfig.push(2005);
        deckConfig.push(2006);
        deckConfig.push(2007);
        deckConfig.push(2008);
        deckConfig.push(2011);
        deckConfig.push(2012);
        deckConfig.push(2013);
        // brass
        deckConfig.push(2028);
        deckConfig.push(2034);
        deckConfig.push(2035);
        // percussion
        deckConfig.push(2042);
        deckConfig.push(2047);
        deckConfig.push(2048);
        // util
        deckConfig.push(5002);
        deckConfig.push(5003);
        deckConfig.push(5004);
        // leader
        deckConfig.push(2080);
        ws.send(BuildRes(sessionId, {status:"success", deck:deckConfig}));
    }
}
