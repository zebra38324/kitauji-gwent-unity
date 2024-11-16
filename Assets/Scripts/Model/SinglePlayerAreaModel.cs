using System;
using System.Collections.Generic;
using UnityEngine;

// 单方对战区逻辑
public class SinglePlayerAreaModel
{
    private static string TAG = "SinglePlayerAreaModel";

    private List<CardInfo> backupCardInfoList = new List<CardInfo>();

    private HandRowAreaModel handRowAreaModel = new HandRowAreaModel();

    private static readonly int initHandCardNum = 10; // 初始手牌数量

    public SinglePlayerAreaModel()
    {

    }

    // 初始时调用，设置所有备选卡牌信息
    public void SetBackupCardInfoIdList(List<int> infoIdList)
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
        foreach (int infoId in infoIdList) {
            backupCardInfoList.Add(FindCardInfo(allCardInfoList, infoId));
        }
    }

    public void DrawHandCards(int num)
    {
        List<CardModel> handCardList = CardGenerator.Instance.GetCards(backupCardInfoList, num);
        handRowAreaModel.AddCardList(handCardList);
    }

    public HandRowAreaModel GetHandRowAreaModel()
    {
        return handRowAreaModel;
    }
}
