import bcrypt from 'bcryptjs';
import Database from 'better-sqlite3';
import { KLog } from '../util/k_log.js';
import { filter } from 'sensitive-word-filter';

const TAG = 'Database';
let db = null;

const userTable = "users";
const statTable = "stat";

export const DatabaseEvent = Object.freeze({
    VISIT: "visit", // 访问，新增ws连接。conn_id
    LOGIN: "login", // 登录。conn_id，username，cur_user_num（算上当前退出用户）
    START_MATCH: "start_match", // 开始匹配。conn_id，username
    START_PVP: "start_pvp", // 开始pvp对局。conn_id，username
    START_PVE: "start_pve", // 开始pve对局。conn_id，username
    QUIT: "quit", // 退出，断开ws连接。conn_id，username，cur_user_num（排除当前退出用户）
});

// 初始化数据库，允许传入不同的数据库路径
function initDatabase(dbPath = 'users.db') {
    KLog.I(TAG, `initDatabase: ${dbPath}`);
    if (db) {
        return;
    }
    db = new Database(dbPath);
    db.prepare(`
        CREATE TABLE IF NOT EXISTS ${userTable} (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            timestamp TIMESTAMP DEFAULT (datetime('now', '+8 hours')),
            username TEXT UNIQUE NOT NULL,
            password_hash TEXT NOT NULL,
            deck_config TEXT
        )
    `).run();

    try {
        db.prepare(`
            ALTER TABLE ${userTable}
            ADD COLUMN competition_config TEXT
        `).run();
        KLog.I(TAG, `initDatabase: add competition_config`);
    } catch (err) {
        if (err.message.includes('duplicate column name')) {
            KLog.I(TAG, `initDatabase: competition_config exist, skip`);
        } else {
            KLog.I(TAG, `initDatabase: error: ${err.message}`);
        }
    }

    db.prepare(`
        CREATE TABLE IF NOT EXISTS ${statTable} (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            timestamp TIMESTAMP DEFAULT (datetime('now', '+8 hours')),
            event TEXT,
            conn_id INTEGER,
            username TEXT,
            cur_user_num INTEGER
        )
    `).run();
}

if (process.env.NODE_ENV == 'prod') {
    initDatabase('users.db'); 
} else if (process.env.NODE_ENV == 'dev') {
    initDatabase('users_dev.db'); 
} else if (process.env.NODE_ENV == 'test') {
    initDatabase(':memory:'); 
} else {
    KLog.E(TAG, `unknown process.env.NODE_ENV: ${process.env.NODE_ENV}`);
}

// 以下为数据库对外接口
// 返回格式
// 成功：{ success: true, user_data }
// 失败：{ success: false, message: "" }
// 除了RegisterUser与AuthUser，其余接口默认仅在完成登录后才可调用，不需再传password

// 注册用户
export const RegisterUser = (username, password) => {
    const exists = db.prepare(`SELECT id FROM ${userTable} WHERE username = ?`).get(username);
    if (exists) {
        return { success: false, message: "用户名已被注册" };
    }
    const filterUserName = filter(username, null);
    if (filterUserName !== username) {
        KLog.W(TAG, `RegisterUser: filter name = ${username}`)
        return { success: false, message: "用户名已被注册" };
    }

    const hash = bcrypt.hashSync(password, 10);
    db.prepare(`INSERT INTO ${userTable} (username, password_hash) VALUES (?, ?)`).run(username, hash);
    InitDeckConfig(username);
    return { success: true, message: "注册成功" };
};

// 验证用户密码
export const AuthUser = (username, password) => {
    const user = db.prepare(`SELECT * FROM ${userTable} WHERE username = ?`).get(username);
    if (!user || !bcrypt.compareSync(password, user.password_hash)) {
        KLog.W(TAG, `AuthUser: not match ${username}`)
        return { success: false, message: "用户名或密码错误" };
    }
    return { success: true };
};

// competition_config为json_str
export const UpdateCompetitionConfig = (username, competition_config) => {
    const result = db.prepare(`UPDATE ${userTable} SET competition_config = ? WHERE username = ?`).run(competition_config, username);
    if (result.changes === 0) {
        return { success: false, message: "用户不存在" };
    }
    return { success: true };
}

// competition_config为json_str
export const GetCompetitionConfig = (username) => {
    const result = db.prepare(`SELECT competition_config FROM ${userTable} WHERE username = ?`).get(username);
    if (!result) {
        return { success: false, message: "用户不存在" };
    }
    return { success: true, competition_config: result.competition_config };
}

// deck_config为json
export const UpdateDeckConfig = (username, deck_config) => {
    const configStr = JSON.stringify(deck_config);
    const result = db.prepare(`UPDATE ${userTable} SET deck_config = ? WHERE username = ?`).run(configStr, username);
    if (result.changes === 0) {
        return { success: false, message: "用户不存在" };
    }
    return { success: true };
}

// 返回一个json
export const GetDeckConfig = (username) => {
    let maxTryTime = 2;
    while (maxTryTime > 0) {
        maxTryTime -= 1;
        const result = db.prepare(`SELECT deck_config FROM ${userTable} WHERE username = ?`).get(username);
        if (!result) {
            return { success: false, message: "用户不存在" };
        }
        let deck_config = null;
        try {
            deck_config = JSON.parse(result.deck_config);
        } catch (err) {
            KLog.E(TAG, `GetDeckConfig: ${err}, ${result.deck_config}`);
            InitDeckConfig(username);
            continue;
        }
        return { success: true, deck_config };
    }
    return { success: false, message: "配置异常" }
}

// 初始化deck_config字段，或deck_config字段异常时，重新初始化数据
function InitDeckConfig(username) {
    KLog.I(TAG, `InitDeckConfig: ${username}`);
    // "deck_config": { "group": 0, "config": [[int数组], [int数组]]}}
    let defaultConfig = {
        group: 0,
        config: [
            [
                1002, 1003, 1004, 1007, 1008, 1009, 1013, 1051, 1052,
                1016, 1021, 1022, 1023, 1024, 1028, 1040, 1044,
                1041,
                5002, 5003, 5004,
                1080
            ],
            [
                2005, 2006, 2007, 2008, 2011, 2012, 2013,
                2028, 2034, 2035,
                2042, 2047, 2048,
                5002, 5003, 5004,
                2080
            ]
        ]
    }
    UpdateDeckConfig(username, defaultConfig);
}

export const ResetDatabase = () => {
    db.prepare(`DELETE FROM ${userTable}`).run();
    db.prepare(`DELETE FROM ${statTable}`).run();
};

// ============================== 以下为statTable =============================
export const StatVisit = (conn_id) => {
    db.prepare(`INSERT INTO ${statTable} (event, conn_id) VALUES (?, ?)`).run(DatabaseEvent.VISIT, conn_id);
}

export const StatLogin = (conn_id, username, cur_user_num) => {
    db.prepare(`INSERT INTO ${statTable} (event, conn_id, username, cur_user_num) VALUES (?, ?, ?, ?)`).run(DatabaseEvent.LOGIN, conn_id, username, cur_user_num);
}

export const StatStartMatch = (conn_id, username) => {
    db.prepare(`INSERT INTO ${statTable} (event, conn_id, username) VALUES (?, ?, ?)`).run(DatabaseEvent.START_MATCH, conn_id, username);
}

export const StatStartPVP = (conn_id, username) => {
    db.prepare(`INSERT INTO ${statTable} (event, conn_id, username) VALUES (?, ?, ?)`).run(DatabaseEvent.START_PVP, conn_id, username);
}

export const StatStartPVE = (conn_id, username) => {
    db.prepare(`INSERT INTO ${statTable} (event, conn_id, username) VALUES (?, ?, ?)`).run(DatabaseEvent.START_PVE, conn_id, username);
}

export const StatQuit = (conn_id, username, cur_user_num) => {
    db.prepare(`INSERT INTO ${statTable} (event, conn_id, username, cur_user_num) VALUES (?, ?, ?, ?)`).run(DatabaseEvent.QUIT, conn_id, username, cur_user_num);
}
