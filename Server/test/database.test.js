import { expect } from 'chai';
import { RegisterUser, AuthUser, ResetDatabase, UpdateDeckConfig, GetDeckConfig } from '../src/database/database.js';

describe('Database', function () {
    before(async () => {
        ResetDatabase();
    });

    it('注册验证', () => {
        let result = RegisterUser('testuser', 'password123');
        expect(result.success).to.be.false;
        result = RegisterUser('user1', 'password123');
        expect(result.success).to.be.true;
        // 同一个用户名，不能重复注册
        result = RegisterUser('user1', 'password123');
        expect(result.success).to.be.false;
        // 同一个用户名，不能重复注册
        result = RegisterUser('user1', 'password456');
        expect(result.success).to.be.false;

        result = AuthUser('user1', 'password123');
        expect(result.success).to.be.true;
        result = AuthUser('user1', 'password456');
        expect(result.success).to.be.false;
    });

    it('牌组配置', () => {
        const username = 'user1';
        let result = RegisterUser(username, 'password123');
        result = GetDeckConfig(username);
        expect(result.success).to.be.true;
        expect(result.deck_config.group).to.equal(0);
        expect(result.deck_config.config.length).to.equal(2);

        let newConfig = result.deck_config;
        newConfig.group = 1;
        result = UpdateDeckConfig(username, newConfig);
        result = GetDeckConfig(username);
        expect(result.deck_config.group).to.equal(1);
    });
});

