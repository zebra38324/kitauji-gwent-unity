using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

/**
 * 只能放一张牌的卡牌列表逻辑
 */
public record SingleCardListModel : CardListModel
{
    private string TAG = "SingleCardListModel";

    public SingleCardListModel(CardLocation location) : base(location)
    {

    }

    public override CardListModel AddCardList(ImmutableList<CardModel> newCardList)
    {
        KLog.E(TAG, "AddCardList invalid");
        return this;
    }

    // 如原不为空，会将之前的牌丢弃掉
    public override CardListModel AddCard(CardModel card)
    {
        if (!cardList.IsEmpty) {
            KLog.I(TAG, "cardList not empty");
            var newRecord = this;
            newRecord = newRecord.RemoveAllCard(out var removedCardList) as SingleCardListModel;
            return newRecord.AddCard(card);
        }
        return base.AddCard(card);
    }
}
