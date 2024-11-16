using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal.Internal;
/**
 * 游戏对局场景的逻辑管理，单例
 */
public class PlaySceneModel
{
    private static string TAG = "PlaySceneModel";

    private static readonly PlaySceneModel instance = new PlaySceneModel();

    static PlaySceneModel() { }

    private PlaySceneModel() { }

    public static PlaySceneModel Instance {
        get {
            return instance;
        }
    }

    private List<CardInfo> selfCardInfoList = new List<CardInfo>();

    private List<CardInfo> enemyCardInfoList = new List<CardInfo>();

    private static readonly int initHandCardNum = 10; // 初始手牌数量

    // 设置双方所有可用卡牌
    public void SetAllCardInfoIdList(List<int> selfInfoIdList, List<int> enemyInfoIdList)
    {
        TextAsset cardInfoAsset = Resources.Load<TextAsset>(@"Statistic\KumikoSecondYear");
        if (cardInfoAsset == null) {
            KLog.E(TAG, "cardInfoAsset is null");
            return;
        }
        List<CardInfo> allCardInfoList = StatisticJsonParse.GetCardInfo(cardInfoAsset.text);
        Func<List<CardInfo>, int, CardInfo> FindCardInfo = (List<CardInfo> cardInfoList, int infoId) => {
            foreach (CardInfo cardInfo in cardInfoList) {
                if (cardInfo.infoId == infoId) {
                    return cardInfo;
                }
            }
            KLog.E(TAG, "infoId: " + infoId + " is invalid");
            return new CardInfo();
        };
        foreach (int infoId in selfInfoIdList) {
            selfCardInfoList.Add(FindCardInfo(allCardInfoList, infoId));
        }
        foreach (int infoId in enemyInfoIdList) {
            enemyCardInfoList.Add(FindCardInfo(allCardInfoList, infoId));
        }
    }

    public void DrawHandCards()
    {
        List<CardModel> selfHandCardList = CardGenerator.Instance.GetCards(selfCardInfoList, initHandCardNum);
        List<CardModel> enemyHandCardList = CardGenerator.Instance.GetCards(enemyCardInfoList, initHandCardNum);
    }

    public List<CardModel> GetSelfHandCards()
    {
        // 暂时测试实现，后续修改
        List<CardModel> cards = new List<CardModel>();
        foreach (CardInfo info in selfCardInfoList) {
            cards.Add(CardGenerator.Instance.GetCard(info));
        }
        return cards;
    }
}
