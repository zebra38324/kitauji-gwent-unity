using LanguageExt;
using static LanguageExt.Prelude;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

/**
 * 弃牌区域的逻辑
 */
public record DiscardAreaModel
{
    public static readonly int ROW_NUM = 3;

    private static readonly int ROW_MAX_CARD_NUM = 10; // 每行最多十张牌

    public CardListModel cardListModel { get; init; } = new CardListModel(CardLocation.DiscardArea);

    public static readonly Lens<DiscardAreaModel, CardListModel> Lens_CardListModel = Lens<DiscardAreaModel, CardListModel>.New(
        d => d.cardListModel,
        cardListModel => d => d with { cardListModel = cardListModel }
    );

    public static readonly Lens<DiscardAreaModel, ImmutableList<CardModel>> Lens_CardListModel_CardList = lens(Lens_CardListModel, CardListModel.Lens_CardList);

    public DiscardAreaModel()
    {

    }

    public DiscardAreaModel AddCard(CardModel card)
    {
        card = card.ChangeCardLocation(CardLocation.DiscardArea);
        return this with {
            cardListModel = cardListModel.AddCard(card)
        };
    }

    public DiscardAreaModel RemoveCard(CardModel card, out CardModel removedCard)
    {
        return this with {
            cardListModel = cardListModel.RemoveCard(card, out removedCard)
        };
    }

    // 获取需要展示的卡牌信息，一共rowNum行，每行至多rowMaxCardNum张牌
    // 如果放不下就尽量平均放
    // onlyNormal为true时只展示非英雄的角色牌
    public List<List<CardModel>> GetShowList(bool onlyNormal)
    {
        var result = Enumerable
            .Range(0, ROW_NUM)
            .Select(_ => new List<CardModel>())
            .ToList();
        var targetCardList = new List<CardModel>();
        foreach (var card in cardListModel.cardList) {
            if (!onlyNormal || card.cardInfo.cardType == CardType.Normal) {
                targetCardList.Add(card);
            }
        }

        int startIndex = 0;
        int remain = targetCardList.Count;
        // 先一行rowMaxCardNum个
        foreach (var row in result) {
            if (remain <= 0) {
                break;
            }
            int num = Math.Min(remain, ROW_MAX_CARD_NUM);
            List<CardModel> rowCardList = targetCardList.GetRange(startIndex, num);
            row.AddRange(rowCardList);
            startIndex += num;
            remain -= num;
        }
        // 还剩下的一行一个平均放
        int rowIndex = 0;
        while (remain > 0) {
            CardModel card = targetCardList[startIndex];
            result[rowIndex].Add(card);
            startIndex += 1;
            remain -= 1;
            rowIndex = (rowIndex + 1) % ROW_NUM;
        }
        return result;
    }
}
