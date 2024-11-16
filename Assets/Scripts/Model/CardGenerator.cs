using System;
using System.Collections.Generic;
/**
 * 卡牌生成管理器，单例
 */
public class CardGenerator
{
    private static string TAG = "CardGenerator";

    private static readonly CardGenerator instance = new CardGenerator();

    static CardGenerator() { }

    private CardGenerator() { }

    public static CardGenerator Instance {
        get {
            return instance;
        }
    }

    private object cardIdLock = new object();

    private int cardId = 1;

    // 根据cardInfo生成卡牌
    public CardModel GetCard(CardInfo cardInfo)
    {
        lock (cardIdLock) {
            cardInfo.id = cardId;
            cardId++;
            return new CardModel(cardInfo);
        }
    }

    // 从cardInfoList中随机生成数个卡牌，并将对应的cardInfo移除
    public List<CardModel> GetCards(List<CardInfo> cardInfoList, int num)
    {
        List<CardModel> result = new List<CardModel>();
        for (int i = 0; i < num; i++) {
            if (cardInfoList.Count <= 0) {
                KLog.W(TAG, "GetCards: cardInfoList not enough, missing " + (num - result.Count).ToString());
                break;
            }
            System.Random ran = new System.Random();
            CardInfo info = cardInfoList[ran.Next(0, cardInfoList.Count)];
            result.Add(GetCard(info));
            cardInfoList.Remove(info);
        }
        return result;
    }
}
