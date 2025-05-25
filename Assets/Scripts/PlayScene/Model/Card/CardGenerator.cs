using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Collections.Immutable;

/**
 * 卡牌生成管理器
 */
public record CardGenerator
{
    private static string TAG = "CardGenerator";

    private static int hostSalt = 1;
    private static int playerSalt = 2;

    // 实际id为 cardId * 10 + salt，salt范围为[0, 9]
    public int salt { get; init; }

    private int cardId { get; init; } = 1;

    private static ImmutableList<CardInfo> allCardInfoList;

    public CardGenerator(bool isHost)
    {
        salt = isHost ? hostSalt : playerSalt;
        LoadAllCardInfoList();
    }

    // 根据infoId生成卡牌，将赋予id
    public CardGenerator GetCardAndUpdateId(int infoId, out CardModel card)
    {
        lock (this) {
            var newRecord = this;
            CardInfo cardInfo = FindCardInfo(infoId);
            cardInfo.id = cardId * 10 + salt;
            newRecord = newRecord with {
                cardId = cardId + 1
            };
            card = new CardModel(cardInfo);
            return newRecord;
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
    public List<CardModel> GetGroupCardList(CardGroup cardGroup)
    {
        List<CardModel> list = new List<CardModel>();
        foreach (CardInfo cardInfo in allCardInfoList) {
            if (cardInfo.group == cardGroup) {
                list.Add(new CardModel(cardInfo));
            }
        }
        return list;
    }

    private void LoadAllCardInfoList()
    {
        if (allCardInfoList != null) {
            return;
        }
        var list = new List<CardInfo>();
        string[] assetNameList = { @"Statistic\KumikoFirstYear",
            @"Statistic\KumikoSecondYear",
            @"Statistic\NeutralCard" };
        foreach (string assetName in assetNameList) {
            TextAsset cardInfoAsset = KResources.Load<TextAsset>(assetName);
            if (cardInfoAsset == null) {
                KLog.E(TAG, "cardInfoAsset: " + assetName + " is null");
                return;
            }
            list.AddRange(StatisticJsonParse.GetCardInfo(cardInfoAsset.text));
        }
        allCardInfoList = list.ToImmutableList();
    }

    private static CardInfo FindCardInfo(int infoId)
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
