import { BuildRes, ApiTypeEnum } from '../util/message_util.js';
import { KLog } from '../util/k_log.js';

const TAG = 'PVPMatch';
const matchQueue = [];
let activeGames = []; // 存储进行中的对局

function AddToMatchQueue(username, sessionId)
{
    matchQueue.push({username, sessionId});
    if (matchQueue.length >= 2) {
        const player1 = matchQueue.shift();
        const player2 = matchQueue.shift();
        return [player1, player2];
    }
    return null;
}

function RemoveFromQueue(username)
{
    const index = matchQueue.findIndex((p) => p.username === username);
    if (index !== -1) {
        matchQueue.splice(index, 1);
        return true;
    }
    return false;
};

function CreateGameSession(player1, player2)
{
    const gameSession = {
        player1: player1,
        player2: player2
    }
    activeGames.push(gameSession);
}

function GetGameSession(username)
{
    return activeGames.find((gameSession) => gameSession.player1.username == username || gameSession.player2.username);
}

function RemoveGameSession(username)
{
    const index = activeGames.findIndex((gameSession) => gameSession.player1.username == username || gameSession.player2.username);
    if (index !== -1) {
        activeGames.splice(index, 1);
        return true;
    }
    return false;
}

export const PVPMatchRoutes = {
    [ApiTypeEnum.PVP_MATCH_START]: (ws, activeConns, sessionId, apiArgs) => {
        // 请求格式：{}
        // 返回格式：
        // {"status": "waiting"}
        // {"status": "success", "opponent": "opponent name", "isHost": true}
        KLog.I(TAG, `${ApiTypeEnum.PVP_MATCH_START}: ${ws.user.username}`);
        const matchRet = AddToMatchQueue(ws.user.username, sessionId);
        if (matchRet) {
            const [player1, player2] = matchRet;
            activeConns.get(player1.username)?.send(BuildRes(sessionId, {status:"success", opponent:player2.username, isHost:true}));
            activeConns.get(player2.username)?.send(BuildRes(sessionId, {status:"success", opponent:player1.username, isHost:false}));
            // 绑定
            CreateGameSession(player1, player2);
        } else {
            ws.send(BuildRes(sessionId, {status:"waiting"}));
        }
    },

    [ApiTypeEnum.PVP_MATCH_CANCEL]: (ws, activeConns, sessionId, apiArgs) => {
        // 请求格式：{}
        // 返回格式：
        // {"status": "success"}
        // {"status": "error", "message": "has matched"}
        if (RemoveFromQueue(ws.user.username)) {
            ws.send(BuildRes(sessionId, {status:"success"}));
        } else {
            ws.send(BuildRes(sessionId, {status:"error", message:"has matched"}));
        }
    },

    [ApiTypeEnum.PVP_MATCH_ACTION]: (ws, activeConns, sessionId, apiArgs) => {
        // 请求格式：{"action":""}
        // 返回格式：
        // {"status": "success"}
        // {"status": "error", "message": ""}
        const gameSession = GetGameSession(ws.user.username);
        if (!gameSession) {
            ws.send(BuildRes(sessionId, {status:"error", message:"not in game"}));
            return;
        }

        let self;
        let opponent;
        if (gameSession.player1.username == ws.user.username) {
            self = gameSession.player1;
            opponent = gameSession.player2;
        } else if (gameSession.player2.username == ws.user.username) {
            self = gameSession.player2;
            opponent = gameSession.player1;
        } else {
            ws.send(BuildRes(sessionId, {status:"error", message:"game session error"}));
            return;
        }
        activeConns.get(opponent.username)?.send(BuildRes(opponent.sessionId, apiArgs));
        ws.send(BuildRes(sessionId, {status:"success"}));
    },

    [ApiTypeEnum.PVP_MATCH_STOP]: (ws, activeConns, sessionId, apiArgs) => {
        // 请求格式：{}
        // 返回格式：
        // self：{"status": "success"}
        // opponent：{"action": "stop"}
        // {"status": "error", "message": ""}
        const gameSession = GetGameSession(ws.user.username);
        if (!gameSession) {
            KLog.W(TAG, `${ApiTypeEnum.PVP_MATCH_STOP}: ${ws.user.username}, not in game or game has stopped`);
            ws.send(BuildRes(sessionId, {status:"success"}));
            return;
        }
        let self;
        let opponent;
        if (gameSession.player1.username == ws.user.username) {
            self = gameSession.player1;
            opponent = gameSession.player2;
        } else if (gameSession.player2.username == ws.user.username) {
            self = gameSession.player2;
            opponent = gameSession.player1;
        } else {
            ws.send(BuildRes(sessionId, {status:"error", message:"game session error"}));
            return;
        }
        activeConns.get(opponent.username)?.send(BuildRes(opponent.sessionId, {action:"stop"}));
        ws.send(BuildRes(sessionId, {status:"success"}));
        RemoveGameSession(ws.user.username);
    }
}

// ws断开时，清理pvp相关信息
export function DisConnectPVPMatchClear(ws, activeConns)
{
    KLog.I(TAG, `DisConnectPVPMatchClear: ${ws.user.username}`);
    RemoveFromQueue(ws.user.username);
    const gameSession = GetGameSession(ws.user.username);
    if (!gameSession) {
        return;
    }
    let self;
    let opponent;
    if (gameSession.player1.username == ws.user.username) {
        self = gameSession.player1;
        opponent = gameSession.player2;
    } else if (gameSession.player2.username == ws.user.username) {
        self = gameSession.player2;
        opponent = gameSession.player1;
    } else {
        return;
    }
    activeConns.get(opponent.username)?.send(BuildRes(opponent.sessionId, {action:"stop"}));
    RemoveGameSession(ws.user.username);
}
