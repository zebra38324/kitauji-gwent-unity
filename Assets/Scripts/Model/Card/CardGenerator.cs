using System;
using System.Collections.Generic;
/**
 * 卡牌生成管理器
 */
public class CardGenerator
{
    private static string TAG = "CardGenerator";

    // 实际id为 cardId * 10 + salt，salt范围为[0, 9]
    public int salt { get; set; }

    public static int serverSalt = 1;
    public static int clientSalt = 2;

    private object cardIdLock = new object();

    private int cardId = 1;

    public CardGenerator(int idSalt)
    {
        salt = idSalt;
    }

    // 根据cardInfo生成卡牌
    public CardModel GetCard(CardInfo cardInfo)
    {
        lock (cardIdLock) {
            cardInfo.id = cardId * 10 + salt;
            cardId++;
            return new CardModel(cardInfo);
        }
    }
}
