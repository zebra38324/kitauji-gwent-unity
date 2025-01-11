import express from 'express';
import expressWs from 'express-ws';
import { AuthRoutes } from "./routes/auth.js";
import { ConfigRoutes } from "./routes/config.js";
import { PVPMatchRoutes, DisConnectPVPMatchClear } from "./routes/pvp_match.js";
import { ParseReq, BuildRes, ApiTypeEnum } from './util/message_util.js';
import { KLog } from './util/k_log.js';

const app = express();
expressWs(app); // 将 WebSocket 功能附加到 Express 实例
app.use(express.json());

const port = 12323;
const TAG = 'Server';
const activeConns = new Map();

app.ws('/kitauji_api', function connection(ws) {
    KLog.I(TAG, "New client connected");
    ws.on("message", (reqMsg) => {
        try {
            const {sessionId, apiType, apiArgs} = ParseReq(reqMsg);
            KLog.I(TAG, `username = ${ws.user?.username}, sessionId = ${sessionId}, apiType = ${apiType}, apiArgs = ${JSON.stringify(apiArgs)}`);
            // 判断是否已登录
            if (apiType != ApiTypeEnum.AUTH_LOGIN && ws.user == undefined) {
                ws.send(BuildRes(sessionId, {status:"error", message:"unauthorized"}));
                return;
            }
            if (AuthRoutes[apiType]) {
                AuthRoutes[apiType](ws, activeConns, sessionId, apiArgs);
            } else if (ConfigRoutes[apiType]) {
                ConfigRoutes[apiType](ws, sessionId);
            } else if (PVPMatchRoutes[apiType]) {
                PVPMatchRoutes[apiType](ws, activeConns, sessionId, apiArgs);
            } else {
                ws.send(BuildRes(sessionId, {status:"error", message:"invalid api type"}));
            }
        } catch (err) {
            KLog.E(TAG, `Error handling message: ${err}`);
            ws.send(BuildRes(-1, {status:"error", message:"invalid req"}));
        }
    });

    ws.on("close", () => {
        KLog.I(TAG, "Client disconnected");
        if (ws.user !== undefined) {
            DisConnectPVPMatchClear(ws, activeConns);
            KLog.I(TAG, `remove active conn: ${ws.user.username}`);
            activeConns.delete(ws.user.username);
        }
    });
});

let server; // 保存服务器实例，用于关闭

// 启动服务器的函数
function startServer(customPort = port) {
    server = app.listen(customPort, () => {
        KLog.I(TAG, `listening at http://localhost:${customPort}`);
    });
    return server;
}

// 检测是否直接运行
if (process.argv[1] && process.argv[1].endsWith('\\server.js')) {
    startServer();
}

export { startServer };
