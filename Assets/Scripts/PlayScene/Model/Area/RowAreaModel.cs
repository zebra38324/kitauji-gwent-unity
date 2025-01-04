using System.Collections.Generic;
/**
 * 行区域的逻辑，包括手牌、对战区的行、弃牌区的行等
 */
public class RowAreaModel
{
    public List<CardModel> cardList {  get; protected set; }

    public RowAreaModel()
    {
        cardList = new List<CardModel>();
    }

    public CardModel FindCard(int cardId)
    {
        foreach (CardModel card in cardList) {
            if (card.cardInfo.id == cardId) {
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
        if (cardList.Contains(targetCard)) {
            targetCard.cardLocation = CardLocation.None;
            cardList.Remove(targetCard);
        }
    }

    public virtual void RemoveAllCard()
    {
        foreach (CardModel card in cardList) {
            card.cardLocation = CardLocation.None;
        }
        cardList.Clear();
    }
    public virtual int GetCurrentPower()
    {
        return 0; // BattleRowAreaModel具体实现
    }
}
