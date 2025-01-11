export const ApiTypeEnum = Object.freeze({
    AUTH_LOGIN: "auth_login",
    CONFIG_DECK_GET: "config_deck_get",
    PVP_MATCH_START: "pvp_match_start",
    PVP_MATCH_CANCEL: "pvp_match_cancel",
    PVP_MATCH_ACTION: "pvp_match_action",
    PVP_MATCH_STOP: "pvp_match_stop",
});

export function ParseReq(reqMsg)
{
    // 请求格式：{"sessionId":1,"sessionData":[byte数组]}
    // 返回格式：{sessionId, apiType, apiArgs}
    const reqMsgJson = JSON.parse(reqMsg);
    const sessionId = reqMsgJson.sessionId;
    const sessionData = Buffer.from(reqMsgJson.sessionData).toString('utf-8');
    
    // 请求格式：{"apiType":"","apiArgs":""}
    const apiData = JSON.parse(sessionData);
    const apiType = apiData.apiType;
    const apiArgs = JSON.parse(apiData.apiArgs);
    return {sessionId, apiType, apiArgs};
}

export function BuildRes(sessionId, resMsg)
{
    // resMsg为json
    // 返回格式：{"sessionId":1,"sessionData":[byte数组]}
    return JSON.stringify({
        sessionId: sessionId,
        sessionData: Array.from(Buffer.from(JSON.stringify(resMsg)))
    });
}
