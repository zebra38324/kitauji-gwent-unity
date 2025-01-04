import { UserContext } from './user_context.js';
// websocket全局管理
export class UserManager
{
    #userMap;

    constructor()
    {
        this.#userMap = new Map();
    }

    // 新连接，登录成功后进行记录
    AddConn(ws)
    {
        var userContext = new UserContext(ws, () => {
            this.#userMap.set("1", userContext);
        });
    }
}
