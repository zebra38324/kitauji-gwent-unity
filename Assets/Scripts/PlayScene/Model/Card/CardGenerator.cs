using System;
using System.Collections.Generic;
using UnityEngine;
/**
 * 卡牌生成管理器
 */
public class CardGenerator
{
    private static string TAG = "CardGenerator";

    // 实际id为 cardId * 10 + salt，salt范围为[0, 9]
    public int salt { get; set; }

    public static int hostSalt = 1;
    public static int playerSalt = 2;

    private int cardId = 1;

    private static List<CardInfo> allCardInfoList_;

    private static List<CardInfo> allCardInfoList {
        get {
            if (allCardInfoList_ != null) {
                return allCardInfoList_;
            }
            allCardInfoList_ = new List<CardInfo>();
            string[] assetNameList = { @"Statistic\KumikoSecondYear", @"Statistic\NeutralCard" };
            foreach (string assetName in assetNameList) {
                TextAsset cardInfoAsset = KResources.Load<TextAsset>(assetName);
                if (cardInfoAsset == null) {
                    KLog.E(TAG, "cardInfoAsset: " + assetName + " is null");
                    return allCardInfoList_;
                }
                allCardInfoList_.AddRange(StatisticJsonParse.GetCardInfo(cardInfoAsset.text));
            }
            return allCardInfoList_;
        }
        set {

        }
    }

    public CardGenerator(bool isHost = true)
    {
        salt = isHost ? hostSalt : playerSalt;
    }

    // 根据infoId生成卡牌，将赋予id
    public CardModel GetCard(int infoId)
    {
        lock (this) {
            CardInfo cardInfo = FindCardInfo(infoId);
            cardInfo.id = cardId * 10 + salt;
            cardId++;
            return new CardModel(cardInfo);
        }
    }

    // 使用提供的id生成卡牌
    public CardModel GetCard(int infoId, int id)
    {
        // 不需要赋予自增id，就不必要加锁了
        CardInfo cardInfo = FindCardInfo(infoId);
        cardInfo.id = id;
        return new CardModel(cardInfo);
    }

    // deck config调用，不关注cardId
    public List<CardModel> GetAllCardList()
    {
        List<CardModel> list = new List<CardModel>();
        foreach (CardInfo cardInfo in allCardInfoList) {
            list.Add(new CardModel(cardInfo));
        }
        return list;
    }

    private CardInfo FindCardInfo(int infoId)
    {
        foreach (CardInfo cardInfo in allCardInfoList) {
            if (cardInfo.infoId == infoId) {
                return cardInfo;
            }
        }
        KLog.E(TAG, "infoId: " + infoId + " is invalid");
        return new CardInfo();
    }
}
