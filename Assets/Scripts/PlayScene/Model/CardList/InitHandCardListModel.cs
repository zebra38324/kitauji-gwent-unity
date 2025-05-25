using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

/**
 * 初始化时，待重抽的手牌区域卡牌列表逻辑
 */
public record InitHandCardListModel : CardListModel
{
    private string TAG = "InitHandCardListModel";

    public static readonly int RE_DRAW_HAND_CARD_NUM_MAX = 2;

    public ImmutableList<CardModel> selectedCardList { get; init; }

    public InitHandCardListModel()
    {
        selectedCardList = ImmutableList<CardModel>.Empty;
        cardLocation = CardLocation.InitHandArea;
    }

    public override CardListModel AddCardList(ImmutableList<CardModel> newCardList)
    {
        newCardList = newCardList
            .Select(card => card.ChangeCardLocation(cardLocation))
            .ToImmutableList();
        return base.AddCardList(newCardList);
    }

    public override CardListModel AddCard(CardModel card)
    {
        card = card.ChangeCardLocation(cardLocation);
        return base.AddCard(card);
    }

    public InitHandCardListModel SelectCard(CardModel card)
    {
        var newSelectedCardList = selectedCardList;
        var newCardList = cardList;
        if (newSelectedCardList.Contains(card)) {
            newSelectedCardList = newSelectedCardList.Remove(card); // 应该删除old card
            var newCard = card with { isSelected = false };
            newCardList = cardList.Replace(card, newCard);
            KLog.I(TAG, "SelectCard: cancel " + card.cardInfo.chineseName);
        } else if (selectedCardList.Count < RE_DRAW_HAND_CARD_NUM_MAX) {
            var newCard = card with { isSelected = true };
            newCardList = cardList.Replace(card, newCard);
            newSelectedCardList = newSelectedCardList.Add(newCard); // 应该添加newCard
            KLog.I(TAG, "SelectCard: " + card.cardInfo.chineseName);
        } else {
            KLog.I(TAG, "SelectCard: too many card, can not choose " + card.cardInfo.chineseName);
        }
        return this with {
            cardList = newCardList,
            selectedCardList = newSelectedCardList,
        };
    }
}
