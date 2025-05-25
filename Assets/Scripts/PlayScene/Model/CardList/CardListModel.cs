using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using LanguageExt;

/**
 * 卡牌列表基类逻辑，包括手牌、对战区每行、弃牌区每行等
 */
public record CardListModel
{
    public ImmutableList<CardModel> cardList { get; init; }

    public static readonly Lens<CardListModel, ImmutableList<CardModel>> Lens_CardList = Lens<CardListModel, ImmutableList<CardModel>>.New(
        c => c.cardList,
        cardList => c => c with { cardList = cardList }
    );

    public CardLocation cardLocation { get; init; } // 预期卡牌位置

    public CardListModel(CardLocation location = CardLocation.None)
    {
        cardList = ImmutableList<CardModel>.Empty;
        cardLocation = location;
    }

    public virtual CardListModel AddCardList(ImmutableList<CardModel> newCardList)
    {
        return this with { cardList = cardList.AddRange(newCardList) };
    }

    public virtual CardListModel AddCard(CardModel card)
    {
        return this with { cardList = cardList.Add(card) };
    }

    // 宽松的remove，先尝试寻找targetCard，然后寻找对应id
    public virtual CardListModel RemoveCard(CardModel targetCard, out CardModel removedCard)
    {
        removedCard = targetCard;
        if (!cardList.Contains(targetCard)) {
            targetCard = cardList.Find(o => o.cardInfo.id == targetCard.cardInfo.id);
            if (targetCard == null) {
                return this;
            }
        }
        removedCard = targetCard.ChangeCardLocation(CardLocation.None);
        return this with { cardList = cardList.Remove(targetCard) };
    }

    public virtual CardListModel RemoveAllCard(out List<CardModel> removedCardList)
    {
        removedCardList = cardList.Select(card => card.ChangeCardLocation(CardLocation.None)).ToList();
        return this with { cardList = ImmutableList<CardModel>.Empty };
    }
}
