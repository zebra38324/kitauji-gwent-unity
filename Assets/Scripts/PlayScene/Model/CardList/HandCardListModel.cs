using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

/**
 * 手牌区卡牌列表逻辑
 */
public record HandCardListModel : CardListModel
{
    public HandCardListModel()
    {
        cardLocation = CardLocation.HandArea;
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
}
