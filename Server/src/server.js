import express from 'express';
import expressWs from 'express-ws';
import { UserManager } from './user_manager.js';

const app = express();
expressWs(app); // 将 WebSocket 功能附加到 Express 实例
app.use(express.json());

const port = 12323;

const userManager = new UserManager();

app.ws('/kitauji_api', function connection(ws) {
    userManager.AddConn(ws);
});

let server; // 保存服务器实例，用于关闭

// 启动服务器的函数
function startServer(customPort = port) {
    server = app.listen(customPort, () => {
        console.log(`listening at http://localhost:${customPort}`);
    });
    return server;
}

// 检测是否直接运行
if (process.argv[1] && process.argv[1].endsWith('\\server.js')) {
    startServer();
}

export { startServer };
