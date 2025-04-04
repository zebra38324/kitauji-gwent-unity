import { BuildRes, ApiTypeEnum } from '../util/message_util.js';
import { UpdateDeckConfig, GetDeckConfig } from '../database/database.js';

const CardGroupEnum = Object.freeze({
    K1: 0,
    K2: 1,
    K3: 2,
});

export const ConfigRoutes = {
    [ApiTypeEnum.CONFIG_DECK_GET]: (ws, sessionId, apiArgs) => {
        // 请求格式：{}
        // 返回格式：
        // {"status": "success", "deck": { "group": 0, "config": [[int数组], [int数组]]}}
        // {"status": "error", "message": ""}
        // 目前config包含k1、k2
        let deck_config = null;
        let status = "success";
        let message = null;
        if (ws.user.isTourist) {
            deck_config = {
                group: CardGroupEnum.K1,
                config: [
                    GetK1DefaultConfig(),
                    GetK2DefaultConfig()
                ]
            }
        } else {
            const result = GetDeckConfig(ws.user.username);
            if (result.success) {
                deck_config = result.deck_config;
            } else {
                message = result.message;
            }
        }
        if (status == "success") {
            ws.send(BuildRes(sessionId, {status: "success", deck: deck_config}));
        } else {
            ws.send(BuildRes(sessionId, {status: "error", message: message}));
        }
    },
    [ApiTypeEnum.CONFIG_DECK_UPDATE]: (ws, sessionId, apiArgs) => {
        // 请求格式：{"deck": { "group": 0, "config": [[int数组], [int数组]]}}
        // 返回格式：
        // {"status": "success"}
        // {"status": "error", "message": ""}
        const result = UpdateDeckConfig(ws.user.username, apiArgs.deck);
        if (result.success) {
            ws.send(BuildRes(sessionId, {status: "success"}));
        } else {
            ws.send(BuildRes(sessionId, {status: "error", message: message}));
        }
    }
}

function GetK1DefaultConfig()
{
    let configList = [];
    // wood
    configList.push(1002);
    configList.push(1003);
    configList.push(1004);
    configList.push(1007);
    configList.push(1008);
    configList.push(1009);
    configList.push(1013);
    configList.push(1051);
    configList.push(1052);
    // brass
    configList.push(1016);
    configList.push(1021);
    configList.push(1022);
    configList.push(1023);
    configList.push(1024);
    configList.push(1028);
    configList.push(1040);
    configList.push(1044);
    // percussion
    configList.push(1041);
    // util
    configList.push(5002);
    configList.push(5003);
    configList.push(5004);
    // leader
    configList.push(1080);
    return configList;
}

function GetK2DefaultConfig()
{
    let configList = [];
    // wood
    configList.push(2005);
    configList.push(2006);
    configList.push(2007);
    configList.push(2008);
    configList.push(2011);
    configList.push(2012);
    configList.push(2013);
    // brass
    configList.push(2028);
    configList.push(2034);
    configList.push(2035);
    // percussion
    configList.push(2042);
    configList.push(2047);
    configList.push(2048);
    // util
    configList.push(5002);
    configList.push(5003);
    configList.push(5004);
    // leader
    configList.push(2080);
    return configList;
}
