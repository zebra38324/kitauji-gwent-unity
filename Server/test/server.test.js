import { expect } from 'chai';
import WebSocket from 'ws';
import { startServer } from '../src/server.js';
import { KLog } from '../src/util/k_log.js';
import { KSleep } from '../src/util/k_time.js';
import { ResetDatabase } from '../src/database/database.js';

const port = 12327; // 测试用的随机端口号
const TAG = 'ServerTest';
let server;

describe('WebSocket Server', function () {
    this.timeout(3000);

    beforeEach(async () => {
        ResetDatabase();
        server = startServer(false);
        await new Promise((resolve) => server.on('listening', resolve));
    });

    afterEach(async () => {
        if (server) {
            await new Promise((resolve, reject) => {
                server.close((err) => {
                    if (err) reject(err);
                    else resolve();
                });
            });
        }
    });

    const DefaultTouristLogin = (ws) => {
        ws.send(JSON.stringify({
            sessionId: 1,
            sessionData: Array.from(Buffer.from(JSON.stringify({
                apiType: "auth_login",
                apiArgs: JSON.stringify({
                    isTourist: true,
                    username: "",
                    password: ""
                })
            }))),
        }));
    }

    const ParseMsg = (response) => {
        const responseJson = JSON.parse(response);
        const sessionId = responseJson.sessionId;
        const sessionDataJson = JSON.parse(Buffer.from(responseJson.sessionData).toString('utf-8'));
        return {sessionId, sessionDataJson};
    }

    it('WebSocket connection', async () => {
        const ws = new WebSocket(`ws://localhost:${port}/kitauji_api`);

        await new Promise((resolve, reject) => {
            ws.on('open', () => {
                KLog.I(TAG, 'WebSocket connection established');
                ws.close();
                resolve();
            });

            ws.on('error', (err) => {
                reject(err);
            });
        });
    });

    it('Register', async () => {
        const ws = new WebSocket(`ws://localhost:${port}/kitauji_api`);
        await new Promise((resolve, reject) => {
            ws.on('open', () => {
                ws.send(JSON.stringify({
                    sessionId: 1,
                    sessionData: Array.from(Buffer.from(JSON.stringify({
                        apiType: "register",
                        apiArgs: JSON.stringify({
                            username: "久美子",
                            password: "123456"
                        })
                    }))),
                }));
                ws.send(JSON.stringify({
                    sessionId: 2,
                    sessionData: Array.from(Buffer.from(JSON.stringify({
                        apiType: "register",
                        apiArgs: JSON.stringify({
                            username: "久美子",
                            password: "456789"
                        })
                    }))),
                }));
            });

            ws.on('message', (response) => {
                const {sessionId, sessionDataJson} = ParseMsg(response);
                if (sessionId == 1) {
                    expect(sessionDataJson.status).to.equal("success");
                } else {
                    expect(sessionDataJson.status).to.equal("error");
                    ws.close();
                    resolve();
                }
            });

            ws.on('error', (err) => {
                reject(err);
            });
        });
    });

    it('LoginTourist', async () => {
        const ws = new WebSocket(`ws://localhost:${port}/kitauji_api`);
        await new Promise((resolve, reject) => {
            ws.on('open', () => {
                DefaultTouristLogin(ws);
            });

            ws.on('message', (response) => {
                const {sessionId, sessionDataJson} = ParseMsg(response);
                expect(sessionId).to.equal(1);
                expect(sessionDataJson.status).to.equal("success");

                ws.close();
                resolve();
            });

            ws.on('error', (err) => {
                reject(err);
            });
        });
    });

    it('LoginUser', async () => {
        const ws = new WebSocket(`ws://localhost:${port}/kitauji_api`);
        await new Promise((resolve, reject) => {
            ws.on('open', () => {
                ws.send(JSON.stringify({
                    sessionId: 1,
                    sessionData: Array.from(Buffer.from(JSON.stringify({
                        apiType: "auth_login",
                        apiArgs: JSON.stringify({
                            isTourist: false,
                            username: "久美子",
                            password: "123456"
                        })
                    }))),
                }));
                ws.send(JSON.stringify({
                    sessionId: 2,
                    sessionData: Array.from(Buffer.from(JSON.stringify({
                        apiType: "register",
                        apiArgs: JSON.stringify({
                            username: "久美子",
                            password: "123456"
                        })
                    }))),
                }));
                ws.send(JSON.stringify({
                    sessionId: 3,
                    sessionData: Array.from(Buffer.from(JSON.stringify({
                        apiType: "auth_login",
                        apiArgs: JSON.stringify({
                            isTourist: false,
                            username: "久美子",
                            password: "123456"
                        })
                    }))),
                }));
            });

            ws.on('message', (response) => {
                const {sessionId, sessionDataJson} = ParseMsg(response);
                if (sessionId == 1) {
                    expect(sessionDataJson.status).to.equal("error");
                } else if (sessionId == 2) {
                    expect(sessionDataJson.status).to.equal("success");
                } else {
                    expect(sessionDataJson.status).to.equal("success");
                    ws.close();
                    resolve();
                }
            });

            ws.on('error', (err) => {
                reject(err);
            });
        });
    });

    // 禁止顶号
    it('LoginUserDup', async () => {
        const ws = new WebSocket(`ws://localhost:${port}/kitauji_api`);
        await new Promise((resolve, reject) => {
            ws.on('open', () => {
                ws.send(JSON.stringify({
                    sessionId: 1,
                    sessionData: Array.from(Buffer.from(JSON.stringify({
                        apiType: "register",
                        apiArgs: JSON.stringify({
                            username: "久美子",
                            password: "123456"
                        })
                    }))),
                }));
                ws.send(JSON.stringify({
                    sessionId: 2,
                    sessionData: Array.from(Buffer.from(JSON.stringify({
                        apiType: "auth_login",
                        apiArgs: JSON.stringify({
                            isTourist: false,
                            username: "久美子",
                            password: "123456"
                        })
                    }))),
                }));
                ws.send(JSON.stringify({
                    sessionId: 3,
                    sessionData: Array.from(Buffer.from(JSON.stringify({
                        apiType: "auth_login",
                        apiArgs: JSON.stringify({
                            isTourist: false,
                            username: "久美子",
                            password: "123456"
                        })
                    }))),
                }));
            });

            ws.on('message', (response) => {
                const {sessionId, sessionDataJson} = ParseMsg(response);
                if (sessionId == 1) {
                    expect(sessionDataJson.status).to.equal("success");
                } else if (sessionId == 2) {
                    expect(sessionDataJson.status).to.equal("success");
                } else {
                    expect(sessionDataJson.status).to.equal("error");
                    ws.close();
                    resolve();
                }
            });

            ws.on('error', (err) => {
                reject(err);
            });
        });
    });

    it('CallOtherApiBeforeLogin', async () => {
        const ws = new WebSocket(`ws://localhost:${port}/kitauji_api`);
        await new Promise((resolve, reject) => {
            ws.on('open', () => {
                ws.send(JSON.stringify({
                    sessionId: 1,
                    sessionData: Array.from(Buffer.from(JSON.stringify({
                        apiType: "config_deck_get",
                        apiArgs: "{}"
                    }))),
                }));
            });

            ws.on('message', (response) => {
                const responseJson = JSON.parse(response);
                expect(responseJson.sessionId).to.equal(1);

                const msgJson = JSON.parse(Buffer.from(responseJson.sessionData).toString('utf-8'));
                expect(msgJson.status).to.equal("error");
                expect(msgJson.message).to.equal("unauthorized");

                ws.close();
                resolve();
            });

            ws.on('error', (err) => {
                reject(err);
            });
        });
    });

    it('ConfigDeckGet', async () => {
        const ws = new WebSocket(`ws://localhost:${port}/kitauji_api`);
        await new Promise((resolve, reject) => {
            ws.on('open', () => {
                DefaultTouristLogin(ws);
            });

            ws.on('message', (response) => {
                const {sessionId, sessionDataJson} = ParseMsg(response);
                if (sessionId == 1) {
                    expect(sessionDataJson.status).to.equal("success");
                    ws.send(JSON.stringify({
                        sessionId: 2,
                        sessionData: Array.from(Buffer.from(JSON.stringify({
                            apiType: "config_deck_get",
                            apiArgs: "{}"
                        }))),
                    }));
                    return;
                } else if (sessionId == 2) {
                    // 返回格式：{"status": "success", "deck": { "group": 0, "config": [[int数组], [int数组]]}}
                    expect(sessionDataJson.status).to.equal("success");
                    expect(sessionDataJson.deck.group).to.equal(0);
                    expect(sessionDataJson.deck.config.length).to.equal(2);
                    ws.close();
                    resolve();
                }
            });

            ws.on('error', (err) => {
                reject(err);
            });
        });
    });

    it('ConfigDeckGetUser', async () => {
        const ws = new WebSocket(`ws://localhost:${port}/kitauji_api`);
        await new Promise((resolve, reject) => {
            ws.on('open', () => {
                ws.send(JSON.stringify({
                    sessionId: 1,
                    sessionData: Array.from(Buffer.from(JSON.stringify({
                        apiType: "register",
                        apiArgs: JSON.stringify({
                            username: "久美子",
                            password: "123456"
                        })
                    }))),
                }));
                ws.send(JSON.stringify({
                    sessionId: 2,
                    sessionData: Array.from(Buffer.from(JSON.stringify({
                        apiType: "auth_login",
                        apiArgs: JSON.stringify({
                            isTourist: false,
                            username: "久美子",
                            password: "123456"
                        })
                    }))),
                }));
                ws.send(JSON.stringify({
                    sessionId: 3,
                    sessionData: Array.from(Buffer.from(JSON.stringify({
                        apiType: "config_deck_get",
                        apiArgs: "{}"
                    }))),
                }));
            });

            ws.on('message', (response) => {
                const {sessionId, sessionDataJson} = ParseMsg(response);
                if (sessionId == 1) {
                    expect(sessionDataJson.status).to.equal("success");
                } else if (sessionId == 2) {
                    expect(sessionDataJson.status).to.equal("success");
                } else {
                    // 返回格式：{"status": "success", "deck": { "group": 0, "config": [[int数组], [int数组]]}}
                    expect(sessionDataJson.status).to.equal("success");
                    expect(sessionDataJson.deck.group).to.equal(0);
                    expect(sessionDataJson.deck.config.length).to.equal(2);
                    ws.close();
                    resolve();
                }
            });

            ws.on('error', (err) => {
                reject(err);
            });
        });
    });

    it('ConfigDeckUpdate', async () => {
        const ws = new WebSocket(`ws://localhost:${port}/kitauji_api`);
        await new Promise((resolve, reject) => {
            ws.on('open', () => {
                ws.send(JSON.stringify({
                    sessionId: 1,
                    sessionData: Array.from(Buffer.from(JSON.stringify({
                        apiType: "register",
                        apiArgs: JSON.stringify({
                            username: "久美子",
                            password: "123456"
                        })
                    }))),
                }));
                ws.send(JSON.stringify({
                    sessionId: 2,
                    sessionData: Array.from(Buffer.from(JSON.stringify({
                        apiType: "auth_login",
                        apiArgs: JSON.stringify({
                            isTourist: false,
                            username: "久美子",
                            password: "123456"
                        })
                    }))),
                }));
                ws.send(JSON.stringify({
                    sessionId: 3,
                    sessionData: Array.from(Buffer.from(JSON.stringify({
                        apiType: "config_deck_update",
                        apiArgs: JSON.stringify({
                            deck: {
                                group: 1,
                                config: [
                                    [1, 2]
                                ]
                            }
                        })
                    }))),
                }));
                ws.send(JSON.stringify({
                    sessionId: 4,
                    sessionData: Array.from(Buffer.from(JSON.stringify({
                        apiType: "config_deck_get",
                        apiArgs: "{}"
                    }))),
                }));
            });

            ws.on('message', (response) => {
                const {sessionId, sessionDataJson} = ParseMsg(response);
                if (sessionId == 1) {
                    expect(sessionDataJson.status).to.equal("success");
                } else if (sessionId == 2) {
                    expect(sessionDataJson.status).to.equal("success");
                } else if (sessionId == 3) {
                    expect(sessionDataJson.status).to.equal("success");
                } else {
                    // 返回格式：{"status": "success", "deck": { "group": 0, "config": [[int数组], [int数组]]}}
                    expect(sessionDataJson.status).to.equal("success");
                    expect(sessionDataJson.deck.group).to.equal(1);
                    expect(sessionDataJson.deck.config.length).to.equal(1);
                    ws.close();
                    resolve();
                }
            });

            ws.on('error', (err) => {
                reject(err);
            });
        });
    });

    it('CompetitionConfig', async () => {
        const ws = new WebSocket(`ws://localhost:${port}/kitauji_api`);
        await new Promise((resolve, reject) => {
            ws.on('open', () => {
                ws.send(JSON.stringify({
                    sessionId: 1,
                    sessionData: Array.from(Buffer.from(JSON.stringify({
                        apiType: "register",
                        apiArgs: JSON.stringify({
                            username: "久美子",
                            password: "123456"
                        })
                    }))),
                }));
                ws.send(JSON.stringify({
                    sessionId: 2,
                    sessionData: Array.from(Buffer.from(JSON.stringify({
                        apiType: "auth_login",
                        apiArgs: JSON.stringify({
                            isTourist: false,
                            username: "久美子",
                            password: "123456"
                        })
                    }))),
                }));
                ws.send(JSON.stringify({
                    sessionId: 3,
                    sessionData: Array.from(Buffer.from(JSON.stringify({
                        apiType: "config_competition_get",
                        apiArgs: "{}"
                    }))),
                }));
                ws.send(JSON.stringify({
                    sessionId: 4,
                    sessionData: Array.from(Buffer.from(JSON.stringify({
                        apiType: "config_competition_update",
                        apiArgs: JSON.stringify({
                            competition_config: "test"
                        })
                    }))),
                }));
                ws.send(JSON.stringify({
                    sessionId: 5,
                    sessionData: Array.from(Buffer.from(JSON.stringify({
                        apiType: "config_competition_get",
                        apiArgs: "{}"
                    }))),
                }));
            });

            ws.on('message', (response) => {
                const {sessionId, sessionDataJson} = ParseMsg(response);
                if (sessionId == 1) {
                    expect(sessionDataJson.status).to.equal("success");
                } else if (sessionId == 2) {
                    expect(sessionDataJson.status).to.equal("success");
                } else if (sessionId == 3) {
                    // 返回格式：{"status": "error", "message": ""}
                    expect(sessionDataJson.status).to.equal("success");
                    expect(sessionDataJson.competition_config).to.equal(null);
                } else if (sessionId == 4) {
                    expect(sessionDataJson.status).to.equal("success");
                } else {
                    // 返回格式：{"status": "success", "competition_config": ""}
                    expect(sessionDataJson.status).to.equal("success");
                    expect(sessionDataJson.competition_config).to.equal("test");
                    ws.close();
                    resolve();
                }
            });

            ws.on('error', (err) => {
                reject(err);
            });
        });
    });

    it('CompetitionConfigTourist', async () => {
        const ws = new WebSocket(`ws://localhost:${port}/kitauji_api`);
        await new Promise((resolve, reject) => {
            ws.on('open', () => {
                ws.send(JSON.stringify({
                    sessionId: 1,
                    sessionData: Array.from(Buffer.from(JSON.stringify({
                        apiType: "auth_login",
                        apiArgs: JSON.stringify({
                            isTourist: true,
                            username: "",
                            password: ""
                        })
                    }))),
                }));
                ws.send(JSON.stringify({
                    sessionId: 2,
                    sessionData: Array.from(Buffer.from(JSON.stringify({
                        apiType: "config_competition_get",
                        apiArgs: "{}"
                    }))),
                }));
                ws.send(JSON.stringify({
                    sessionId: 3,
                    sessionData: Array.from(Buffer.from(JSON.stringify({
                        apiType: "config_competition_update",
                        apiArgs: JSON.stringify({
                            competition_config: "test"
                        })
                    }))),
                }));
            });

            ws.on('message', (response) => {
                const {sessionId, sessionDataJson} = ParseMsg(response);
                if (sessionId == 1) {
                    expect(sessionDataJson.status).to.equal("success");
                } else if (sessionId == 2) {
                    expect(sessionDataJson.status).to.equal("error");
                    expect(sessionDataJson.message).to.equal("tourist not support");
                } else {
                    expect(sessionDataJson.status).to.equal("error");
                    expect(sessionDataJson.message).to.equal("tourist not support");
                    ws.close();
                    resolve();
                }
            });

            ws.on('error', (err) => {
                reject(err);
            });
        });
    });

    it('PVPMatch', async () => {
        const ws1 = new WebSocket(`ws://localhost:${port}/kitauji_api`);
        const ws2 = new WebSocket(`ws://localhost:${port}/kitauji_api`);
        const ws3 = new WebSocket(`ws://localhost:${port}/kitauji_api`);
        await new Promise(async (resolve, reject) => {
            ws1.on('open', () => {
                DefaultTouristLogin(ws1);
            });
            ws2.on('open', async () => {
                await KSleep(10);
                DefaultTouristLogin(ws2);
            });
            ws3.on('open', async () => {
                await KSleep(10);
                DefaultTouristLogin(ws3);
            });

            const DefaultPVPMatch = (ws, response, init_session_id) => {
                const {sessionId, sessionDataJson} = ParseMsg(response);
                if (sessionId == 1) {
                    // login
                    expect(sessionDataJson.status).to.equal("success");
                    const username = sessionDataJson.username;
                    ws.user = { username };
                    ws.send(JSON.stringify({
                        sessionId: init_session_id,
                        sessionData: Array.from(Buffer.from(JSON.stringify({
                            apiType: "pvp_match_start",
                            apiArgs: JSON.stringify({})
                        }))),
                    }));
                } else if (sessionId == init_session_id) {
                    if (sessionDataJson.status == "waiting") {
                        KLog.I(TAG, `DefaultPVPMatch: ${ws.user.username} waiting`);
                        return;
                    }
                    if (ws.user.hasMatched == undefined) {
                        expect(sessionDataJson.status).to.equal("success");
                        KLog.I(TAG, `DefaultPVPMatch: ${ws.user.username} success`);
                        ws.user.hasMatched = true;
                        ws.send(JSON.stringify({
                            sessionId: init_session_id,
                            sessionData: Array.from(Buffer.from(JSON.stringify({
                                apiType: "pvp_match_action",
                                apiArgs: JSON.stringify({action:"test action"})
                            }))),
                        }));
                        return;
                    }
                    expect(sessionDataJson.status).to.not.equal("error");
                    if (sessionDataJson.status == "success" && ws.user.hasSendStop == undefined) {
                        // 发送的反馈，不用管
                        return;
                    } else if (sessionDataJson.action == "test action") {
                        ws.send(JSON.stringify({
                            sessionId: init_session_id,
                            sessionData: Array.from(Buffer.from(JSON.stringify({
                                apiType: "pvp_match_stop",
                                apiArgs: JSON.stringify({})
                            }))),
                        }));
                        ws.user.hasSendStop = true;
                    } else if (sessionDataJson.action == "stop" || sessionDataJson.status == "success") {
                        ws.close();
                    }
                }
            }

            ws1.on('message', (response) => {
                const {sessionId, sessionDataJson} = ParseMsg(response);
                if (sessionId == 1) {
                    // login
                    expect(sessionDataJson.status).to.equal("success");
                    const username = sessionDataJson.username;
                    ws1.user = { username };
                    ws1.send(JSON.stringify({
                        sessionId: 2,
                        sessionData: Array.from(Buffer.from(JSON.stringify({
                            apiType: "pvp_match_start",
                            apiArgs: JSON.stringify({})
                        }))),
                    }));
                } else if (sessionId == 2) {
                    expect(sessionDataJson.status).to.equal("waiting");
                    KLog.I(TAG, `DefaultPVPMatch: ${ws1.user.username} waiting`);
                    ws1.send(JSON.stringify({
                        sessionId: 3,
                        sessionData: Array.from(Buffer.from(JSON.stringify({
                            apiType: "pvp_match_cancel",
                            apiArgs: JSON.stringify({})
                        }))),
                    }));
                } else if (sessionId == 3) {
                    expect(sessionDataJson.status).to.equal("success");
                    ws1.close();
                }
            });
            ws2.on('message', (response) => {
                DefaultPVPMatch(ws2, response, 2);
            });
            ws3.on('message', (response) => {
                DefaultPVPMatch(ws3, response, 3);
            });

            ws1.on('error', (err) => {
                reject(err);
            });
            ws2.on('error', (err) => {
                reject(err);
            });
            ws3.on('error', (err) => {
                reject(err);
            });

            const CheckAllConnClosed = setInterval(() => {
                if (ws1.readyState == WebSocket.CLOSED &&
                    ws2.readyState == WebSocket.CLOSED &&
                    ws3.readyState == WebSocket.CLOSED) {
                    resolve();
                    clearInterval(CheckAllConnClosed);
                }
            }, 1);
            CheckAllConnClosed();
        });
    });

    it('PVPMatchDisconnect', async () => {
        const ws1 = new WebSocket(`ws://localhost:${port}/kitauji_api`);
        const ws2 = new WebSocket(`ws://localhost:${port}/kitauji_api`);
        const ws3 = new WebSocket(`ws://localhost:${port}/kitauji_api`);
        await new Promise(async (resolve, reject) => {
            ws1.on('open', () => {
                DefaultTouristLogin(ws1);
            });
            ws2.on('open', async () => {
                await KSleep(10);
                DefaultTouristLogin(ws2);
            });
            ws3.on('open', async () => {
                await KSleep(10);
                DefaultTouristLogin(ws3);
            });
            ws1.on('message', (response) => {
                const {sessionId, sessionDataJson} = ParseMsg(response);
                if (sessionId == 1) {
                    expect(sessionDataJson.status).to.equal("success");
                    const username = sessionDataJson.username;
                    ws1.user = { username };
                    ws1.send(JSON.stringify({
                        sessionId: 2,
                        sessionData: Array.from(Buffer.from(JSON.stringify({
                            apiType: "pvp_match_start",
                            apiArgs: JSON.stringify({})
                        }))),
                    }));
                } else if (sessionId == 2) {
                    expect(sessionDataJson.status).to.equal("waiting");
                    KLog.I(TAG, `PVPMatchDisconnect: ${ws1.user.username} waiting`);
                    ws1.close();
                }
            });
            ws2.on('message', (response) => {
                const {sessionId, sessionDataJson} = ParseMsg(response);
                if (sessionId == 1) {
                    expect(sessionDataJson.status).to.equal("success");
                    const username = sessionDataJson.username;
                    ws2.user = { username };
                    ws2.send(JSON.stringify({
                        sessionId: 2,
                        sessionData: Array.from(Buffer.from(JSON.stringify({
                            apiType: "pvp_match_start",
                            apiArgs: JSON.stringify({})
                        }))),
                    }));
                } else if (sessionId == 2) {
                    if (sessionDataJson.status == "waiting") {
                        return;
                    }
                    expect(sessionDataJson.status).to.equal("success");
                    KLog.I(TAG, `PVPMatchDisconnect: ${ws2.user.username} success`);
                    ws2.close();
                }
            });
            ws3.on('message', (response) => {
                const {sessionId, sessionDataJson} = ParseMsg(response);
                if (sessionId == 1) {
                    expect(sessionDataJson.status).to.equal("success");
                    const username = sessionDataJson.username;
                    ws3.user = { username };
                    ws3.send(JSON.stringify({
                        sessionId: 2,
                        sessionData: Array.from(Buffer.from(JSON.stringify({
                            apiType: "pvp_match_start",
                            apiArgs: JSON.stringify({})
                        }))),
                    }));
                } else if (sessionId == 2) {
                    if (sessionDataJson.status == "waiting") {
                        return;
                    }
                    if (ws3.user.hasMatched == undefined) {
                        expect(sessionDataJson.status).to.equal("success");
                        ws3.user.hasMatched = true;
                        KLog.I(TAG, `PVPMatchDisconnect: ${ws3.user.username} success`);
                        return;
                    }
                    expect(sessionDataJson.action).to.equal("stop");
                    ws3.close();
                }
            });

            ws1.on('error', (err) => {
                reject(err);
            });
            ws2.on('error', (err) => {
                reject(err);
            });
            ws3.on('error', (err) => {
                reject(err);
            });

            const CheckAllConnClosed = setInterval(() => {
                if (ws1.readyState == WebSocket.CLOSED &&
                    ws2.readyState == WebSocket.CLOSED &&
                    ws3.readyState == WebSocket.CLOSED) {
                    resolve();
                    clearInterval(CheckAllConnClosed);
                }
            }, 1);
            CheckAllConnClosed();
        });
    });

    const SendHeartbeat = (ws, sessionId, user_status) => {
        ws.send(JSON.stringify({
            sessionId: sessionId,
            sessionData: Array.from(Buffer.from(JSON.stringify({
                apiType: "heartbeat",
                apiArgs: JSON.stringify({
                    user_status: user_status
                })
            }))),
        }));
    }

    it('Heartbeat', async () => {
        const ws1 = new WebSocket(`ws://localhost:${port}/kitauji_api`);
        const ws2 = new WebSocket(`ws://localhost:${port}/kitauji_api`);
        const ws3 = new WebSocket(`ws://localhost:${port}/kitauji_api`);
        await new Promise((resolve, reject) => {
            ws1.on('open', () => {
                DefaultTouristLogin(ws1);
            });
            ws2.on('open', async () => {
                DefaultTouristLogin(ws2);
            });
            ws3.on('open', async () => {
                DefaultTouristLogin(ws3);
            });

            ws1.on('message', async (response) => {
                const {sessionId, sessionDataJson} = ParseMsg(response);
                if (sessionId == 1) {
                    SendHeartbeat(ws1, 2, 1);
                } else if (sessionId == 2) {
                    expect(sessionDataJson.all_users.length).to.equal(4);
                    expect(sessionDataJson.all_users[1]).to.equal(1);
                    await KSleep(10);
                    ws1.close();
                }
            });
            ws2.on('message', async (response) => {
                const {sessionId, sessionDataJson} = ParseMsg(response);
                if (sessionId == 1) {
                    await KSleep(10);
                    SendHeartbeat(ws2, 2, 2);
                } else if (sessionId == 2) {
                    expect(sessionDataJson.all_users.length).to.equal(4);
                    expect(sessionDataJson.all_users[1]).to.equal(1);
                    expect(sessionDataJson.all_users[2]).to.equal(1);
                    await KSleep(10);
                    ws2.close();
                }
            });
            ws3.on('message', async (response) => {
                const {sessionId, sessionDataJson} = ParseMsg(response);
                if (sessionId == 1) {
                    await KSleep(10);
                    SendHeartbeat(ws3, 2, 3);
                } else if (sessionId == 2) {
                    expect(sessionDataJson.all_users.length).to.equal(4);
                    expect(sessionDataJson.all_users[1]).to.equal(1);
                    expect(sessionDataJson.all_users[3]).to.equal(1);
                    await KSleep(10);
                    ws3.close();
                }
            });

            ws1.on('error', (err) => {
                reject(err);
            });
            ws2.on('error', (err) => {
                reject(err);
            });
            ws3.on('error', (err) => {
                reject(err);
            });
            const CheckAllConnClosed = setInterval(() => {
                if (ws1.readyState == WebSocket.CLOSED &&
                    ws2.readyState == WebSocket.CLOSED &&
                    ws3.readyState == WebSocket.CLOSED) {
                    resolve();
                    clearInterval(CheckAllConnClosed);
                }
            }, 1);
            //CheckAllConnClosed();
        });
    });
});
