using System.Collections;
using System.Collections.Generic;

// 单例，用于场景转换时，记录对局信息
public class GameConfig
{
    private static GameConfig _instance;

    private GameConfig() { }

    public static GameConfig Instance {
        get {
            if (_instance == null) {
                _instance = new GameConfig();
                _instance.Reset();
            }
            return _instance;
        }
    }

    // 比赛配置
    public string selfName; // 玩家名称

    public string enemyName; // 对方名字

    public CardGroup selfGroup; // 玩家卡组

    public bool isHost; // 是否为房主

    public bool isPVP; // 是否为PVP对局

    public PlaySceneAI.AIType pveAIType; // PVE AI类型

    public int pvpSessionId; // PVP对局的会话ID

    public string fromScene; // 从哪个场景进入的对局

    // 比赛结果
    public bool normalFinish; // 是否正常结束对局

    public int selfScore; // 玩家分数

    public int enemyScore; // 对方分数

    public bool isSelfWin; // 是否为玩家获胜

    public void Reset()
    {
        selfName = "";
        enemyName = "";
        selfGroup = CardGroup.KumikoFirstYear;
        isHost = true;
        isPVP = false;
        pveAIType = PlaySceneAI.AIType.L1K1;
        pvpSessionId = -1;
        fromScene = "MainMenuScene";

        normalFinish = true;
        selfScore = 0;
        enemyScore = 0;
        isSelfWin = false;
    }
}
