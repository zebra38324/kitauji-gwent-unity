# 北宇治昆特牌
## 介绍
<b>本项目仅作学习交流使用</b>

北宇治昆特牌改编自巫师3昆特牌，包含了一套以动画《吹响吧！上低音号》中的人物为主题的卡牌。

游戏设计、美术素材等来自于[原北宇治昆特牌仓库](https://github.com/kitauji-gwent/kitauji-gwent)以及[京吹官网](https://www.kyotoanimation.co.jp/shop/kitaujisuibu/#character)。

原项目使用js构建，本项目为其unity重置版。

## 如何访问
pc端使用chrome或edge浏览器，访问：https://kitauji-gwent.top/

## 开发环境配置
Windows, Unity 2022.3.50f1c1

### 文件结构
- Assets
    - RemoteRes：远程加载的图片等资源
        - Image/origin-image：卡牌原始图片。此路径下README包含如何修改原始图片的说明
    - Editor
        - Test：单元测试，针对`Scripts/PlayScene/Model`中的逻辑代码进行测试。使用unity editor的test runner运行
    - Resources：图片等资源
    - Scripts：客户端主要代码
        - Common：各场景通用代码
        - DeckConfigScene：牌组配置场景
        - LoginScene：登录场景
        - MainMenuScene：主菜单
        - PlayScene：对战场景
            - Model：与UI剥离的对战逻辑
- Server：服务端模块

## feature list
优先级从高到低
- [x] 账号系统，支持账号登录、牌组配置保存
- [x] 规则说明模块
- [x] 牌组配置模块
- [ ] 基础牌组
   - [x] 久一年
   - [x] 久二年
   - [ ] 久三年
- [ ] 人机对局
   - [x] 简单的久二年ai
   - [ ] 扩展更多牌组、难度的ai
- [ ] 卡牌图鉴
- [ ] 特殊模式
   - [ ] 利兹与青鸟
   - [ ] 合奏比赛

## 效果实例
![对局场景](docs/play_scene.png "对局场景")

![牌组配置](docs/deck_config.png "牌组配置")
