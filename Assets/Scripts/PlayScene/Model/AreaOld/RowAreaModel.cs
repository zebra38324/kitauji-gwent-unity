using System.Collections.Generic;
/**
 * 行区域的逻辑，包括手牌、对战区的行、弃牌区的行等
 */
public class RowAreaModel
{
    public List<CardModelOld> cardList {  get; protected set; }

    public RowAreaModel()
    {
        cardList = new List<CardModelOld>();
    }

    public CardModelOld FindCard(int cardId)
    {
        foreach (CardModelOld card in cardList) {
            if (card.cardInfo.id == cardId) {
                return card;
            }
        }
        return null;
    }

    public virtual void AddCardList(List<CardModelOld> newCardList)
    {
        cardList.AddRange(newCardList);
    }

    public virtual void AddCard(CardModelOld card)
    {
        cardList.Add(card);
    }

    public virtual void RemoveCard(CardModelOld targetCard)
    {
        if (cardList.Contains(targetCard)) {
            targetCard.cardLocation = CardLocation.None;
            cardList.Remove(targetCard);
        }
    }

    public virtual void RemoveAllCard()
    {
        foreach (CardModelOld card in cardList) {
            card.cardLocation = CardLocation.None;
        }
        cardList.Clear();
    }
    public virtual int GetCurrentPower()
    {
        return 0; // BattleRowAreaModelOld具体实现
    }
}
