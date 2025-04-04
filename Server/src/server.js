import express from 'express';
import expressWs from 'express-ws';
import fs from 'fs';
import https from 'https';
import { StatVisit, StatQuit } from "./database/database.js";
import { RegisterRoutes } from "./routes/register.js";
import { AuthRoutes } from "./routes/auth.js";
import { ConfigRoutes } from "./routes/config.js";
import { PVPMatchRoutes, DisConnectPVPMatchClear } from "./routes/pvp_match.js";
import { HeartbeatRoutes } from "./routes/heartbeat.js";
import { ParseReq, BuildRes, ApiTypeEnum } from './util/message_util.js';
import { KLog } from './util/k_log.js';

const app = express();
expressWs(app); // 将 WebSocket 功能附加到 Express 实例
app.use(express.json());

const TAG = 'Server';
const activeConns = new Map();
let uniqueConnId = 1;

app.ws('/kitauji_api', function connection(ws) {
    KLog.I(TAG, "New client connected");
    ws.connId = uniqueConnId;
    uniqueConnId += 1;
    StatVisit(ws.connId);

    ws.on("message", (reqMsg) => {
        try {
            const {sessionId, apiType, apiArgs} = ParseReq(reqMsg);
            // TODO: password log
            KLog.I(TAG, `username = ${ws.user?.username}, sessionId = ${sessionId}, apiType = ${apiType}, apiArgs = ${JSON.stringify(apiArgs)}`);
            // 判断是否已登录
            if (apiType != ApiTypeEnum.REGISTER && apiType != ApiTypeEnum.AUTH_LOGIN && ws.user == undefined) {
                ws.send(BuildRes(sessionId, {status:"error", message:"unauthorized"}));
                return;
            }
            if (RegisterRoutes[apiType]) {
                RegisterRoutes[apiType](ws, sessionId, apiArgs);
            } else if (AuthRoutes[apiType]) {
                AuthRoutes[apiType](ws, activeConns, sessionId, apiArgs);
                if (ws.user !== undefined) {
                    KLog.I(TAG, `New client connected, active conns: ${activeConns.size}`);
                }
            } else if (ConfigRoutes[apiType]) {
                ConfigRoutes[apiType](ws, sessionId, apiArgs);
            } else if (PVPMatchRoutes[apiType]) {
                PVPMatchRoutes[apiType](ws, activeConns, sessionId, apiArgs);
            } else if (HeartbeatRoutes[apiType]) {
                HeartbeatRoutes[apiType](ws, activeConns, sessionId, apiArgs);
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
        let username = "";
        if (ws.user !== undefined) {
            ws.user.stop_heartbeat = true;
            DisConnectPVPMatchClear(ws, activeConns);
            KLog.I(TAG, `remove active conn: ${ws.user.username}`);
            username = ws.user.username;
            activeConns.delete(ws.user.username);
        }
        StatQuit(ws.connId, username, activeConns.size);
        KLog.I(TAG, `Client disconnected, active conns: ${activeConns.size}`);
    });
});

let server; // 保存服务器实例，用于关闭

// 启动服务器的函数
function startServer(isSsl) {
    let customPort = 0;
    if (process.env.NODE_ENV == 'prod') {
        customPort = 12323;
    } else if (process.env.NODE_ENV == 'dev') {
        customPort = 12325;
    } else if (process.env.NODE_ENV == 'test') {
        customPort = 12327;
    } else {
        KLog.E(TAG, `unknown process.env.NODE_ENV: ${process.env.NODE_ENV}`);
        return;
    }
    if (isSsl) {
        // 加载 SSL/TLS 证书
        const privateKey = fs.readFileSync('/etc/nginx/ssl/kitauji-gwent.com.key', 'utf8');
        const certificate = fs.readFileSync('/etc/nginx/ssl/kitauji-gwent.com_bundle.crt', 'utf8');
        const ca = fs.readFileSync('/etc/nginx/ssl/kitauji-gwent.com_bundle.pem', 'utf8');
        const credentials = { key: privateKey, cert: certificate, ca: ca };
        const httpsServer = https.createServer(credentials, app);
        expressWs(app, httpsServer);
        server = httpsServer.listen(customPort, () => {
            KLog.I(TAG, `listening at port:${customPort}`);
        });
    } else {
        server = app.listen(customPort, () => {
            KLog.I(TAG, `listening at port:${customPort}`);
        });
    }
    return server;
}

// 检测是否为test TODO: pm2可不可用
if (process.env.NODE_ENV != 'test') {
    startServer(process.env.USE_SSL);
}

export { startServer };
