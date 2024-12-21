using System.Collections.Generic;
/**
 * 只能放一张牌的row区域逻辑
 */
public class SingleCardRowAreaModel: RowAreaModel
{
    private static string TAG = "SingleCardRowAreaModel";

    public SingleCardRowAreaModel()
    {

    }

    public override void AddCardList(List<CardModel> newCardList)
    {
        KLog.E(TAG, "AddCardList invalid");
    }

    public override void AddCard(CardModel card)
    {
        base.RemoveAllCard();
        base.AddCard(card);
    }
}
