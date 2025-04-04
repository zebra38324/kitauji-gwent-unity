import { BuildRes, ApiTypeEnum } from '../util/message_util.js';
import { KLog } from '../util/k_log.js';
import { StatStartPVP, StatStartPVE } from '../database/database.js';

const TAG = 'Heartbeat';

const UserStatus = Object.freeze({
    IDLE: 0,
    PVP_MATCHING: 1,
    PVP_GAMING: 2,
    PVE_GAMING: 3,
});

export const HeartbeatRoutes = {
    [ApiTypeEnum.HEARTBEAT]: (ws, activeConns, sessionId, apiArgs) => {
        // 格式可能随着功能开发有频繁变化
        // client -> server
        // 格式：{"user_status":0}，参考UserStatus
        // 无直接回复
        // server -> client
        // 格式：{"all_users":[1,1,0,2]}，列出每个UserStatus的人数
        // 无直接回复
        // client发送第一条心跳后，双方将使用这同一个sessionId
        ws.user.user_status = apiArgs.user_status;
        if (ws.user.has_heartbeat == undefined) {
            ws.user.has_heartbeat = true;
            SendHeartbeatToClient(ws, activeConns, sessionId);
        }
        if (apiArgs.user_status == UserStatus.PVP_GAMING) {
            StatStartPVP(ws.connId, ws.user.username);
        } else if (apiArgs.user_status == UserStatus.PVE_GAMING) {
            StatStartPVE(ws.connId, ws.user.username);
        }
    }
}

function SendHeartbeatToClient(ws, activeConns, sessionId) {
    let timeInterval = 10000;
    if (process.env.NODE_ENV == 'test') {
        timeInterval = 20;
    }
    const sendLambda = () => {
        if (ws.user.stop_heartbeat) {
            KLog.I(TAG, `${ws.user.username} stop`);
            clearInterval(interval);
            return;
        }
        const statusCount = Object.keys(UserStatus).length;
        let all_users_status = new Array(statusCount).fill(0);
        for (const userWs of activeConns.values()) {
            if (userWs.user.user_status == undefined) {
                continue;
            }
            all_users_status[userWs.user.user_status] += 1;
        }
        ws.send(BuildRes(sessionId, {all_users: all_users_status}));
    };
    sendLambda();
    const interval = setInterval(() => {
        sendLambda();
    }, timeInterval);
}
