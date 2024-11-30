using System;
using System.Collections.Generic;
using System.Linq;

// kitauji config
// 全局配置读取
public class KConfig
{
    private static readonly KConfig instance = new KConfig();

    private KConfig playSceneModel;

    static KConfig() { }

    private KConfig() { }

    public static KConfig Instance {
        get {
            return instance;
        }
    }

    // 获取游戏使用的牌组信息配置
    public List<int> GetBattleCardInfoIdList()
    {
        // List<int> cardInfoIdList = new List<int>();
        List<int> cardInfoIdList = Enumerable.Range(2001, 15).ToList();
        return cardInfoIdList;
    }
}