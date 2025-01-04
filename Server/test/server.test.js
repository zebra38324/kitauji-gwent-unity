import { expect } from 'chai';
import WebSocket from 'ws';
import { startServer } from '../src/server.js';

const port = 12346; // 测试用的随机端口号
let server;

describe('WebSocket Server', function () {
    this.timeout(3000);

    before(async () => {
        server = startServer(port);
        await new Promise((resolve) => server.on('listening', resolve));
    });

    after(async () => {
        if (server) {
            await new Promise((resolve, reject) => {
                server.close((err) => {
                    if (err) reject(err);
                    else resolve();
                });
            });
        }
    });

    it('WebSocket connection', async () => {
        const ws = new WebSocket(`ws://localhost:${port}/kitauji_api`);

        await new Promise((resolve, reject) => {
            ws.on('open', () => {
                console.log('WebSocket connection established');
                ws.close();
                resolve();
            });

            ws.on('error', (err) => {
                reject(err);
            });
        });
    });

    it('Login', async () => {
        const ws = new WebSocket(`ws://localhost:${port}/kitauji_api`);

        await new Promise((resolve, reject) => {
            ws.on('open', () => {
                const message = JSON.stringify({
                    sessionId: 1,
                    data: Array.from(Buffer.from(JSON.stringify({
                        apiType: 1,
                        msg: JSON.stringify({
                            isTourist: true,
                            name: "",
                            password: ""
                        })
                    }))),
                });
                ws.send(message);
            });

            ws.on('message', (response) => {
                const responseJson = JSON.parse(response);
                expect(responseJson.sessionId).to.equal(1);
                const responseSessionData = Buffer.from(responseJson.data).toString('utf-8');

                console.log('Received response:', responseSessionData);
                expect(responseSessionData).to.be.a('string');

                const msgJson = JSON.parse(responseSessionData);
                expect(msgJson.result).to.equal(true);
                expect(msgJson.userId).to.equal(1234);

                ws.close();
                resolve();
            });

            ws.on('error', (err) => {
                reject(err);
            });
        });
    });
});
