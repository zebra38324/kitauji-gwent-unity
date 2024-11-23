using System.Collections.Generic;
/**
 * 手牌区逻辑
 */
public class HandRowAreaModel : RowAreaModel
{
    public HandRowAreaModel()
    {
        
    }

    public override void AddCardList(List<CardModel> newCardList)
    {
        foreach (CardModel card in newCardList) {
            card.cardLocation = CardLocation.HandArea;
        }
        base.AddCardList(newCardList);
    }

    public override void AddCard(CardModel card)
    {
        card.cardLocation = CardLocation.HandArea;
        base.AddCard(card);
    }
}
