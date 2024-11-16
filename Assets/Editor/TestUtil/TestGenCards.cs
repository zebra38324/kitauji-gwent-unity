using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class TestGenCards
{
    private static string TAG = "TestGenCards";

    private static List<CardInfo> allCardInfoList_;

    private static List<CardInfo> allCardInfoList {
        get {
            TextAsset cardInfoAsset = Resources.Load<TextAsset>(@"Statistic\KumikoSecondYear");
            if (cardInfoAsset == null) {
                KLog.E(TAG, "cardInfoAsset is null");
                return allCardInfoList_;
            }
            allCardInfoList_ = StatisticJsonParse.GetCardInfo(cardInfoAsset.text);
            return allCardInfoList_;
        }
        set {

        }
    }

    public static List<CardModel> GetCardList(List<int> infoIdList)
    {
        List<CardModel> result = new List<CardModel>();
        foreach (int infoId in infoIdList) {
            result.Add(GetCard(infoId));
        }
        return result;
    }

    public static CardModel GetCard(int infoId)
    {
        Func<List<CardInfo>, int, CardInfo> FindCardInfo = (List<CardInfo> cardInfoList, int infoId) => {
            foreach (CardInfo cardInfo in cardInfoList) {
                if (cardInfo.infoId == infoId) {
                    return cardInfo;
                }
            }
            KLog.E(TAG, "infoId: " + infoId + " is invalid");
            return new CardInfo();
        };
        return CardGenerator.Instance.GetCard(FindCardInfo(allCardInfoList, infoId));
    }
}
