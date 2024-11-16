using System.Collections.Generic;
/**
 * 行区域的逻辑，包括手牌、对战区的行、弃牌区的行等
 */
public class RowAreaModel
{
    protected List<CardModel> cardList;

    public RowAreaModel()
    {
        cardList = new List<CardModel>();
    }

    public List<CardModel> GetCardList()
    {
        return cardList;
    }

    public CardModel FindCard(int cardId)
    {
        foreach (CardModel card in cardList) {
            if (card.GetCardInfo().id == cardId) {
                return card;
            }
        }
        return null;
    }

    public virtual void AddCardList(List<CardModel> newCardList)
    {
        cardList.AddRange(newCardList);
    }

    public virtual void AddCard(CardModel card)
    {
        cardList.Add(card);
    }

    public virtual void RemoveCard(CardModel targetCard)
    {
        foreach (CardModel card in cardList) {
            if (card == targetCard) {
                card.cardLocation = CardLocation.None;
                cardList.Remove(card);
                break;
            }
        }
    }
}
