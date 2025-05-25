using System.Collections.Generic;
/**
 * 弃牌区行区域的逻辑
 */
public class DiscardRowAreaModel : RowAreaModel
{
    // 基类的remove操作会修改cardLocation，不符合需求，因此重写屏蔽基类
    public override void RemoveCard(CardModelOld targetCard)
    {
        if (cardList.Contains(targetCard)) {
            cardList.Remove(targetCard);
        }
    }

    public override void RemoveAllCard()
    {
        cardList.Clear();
    }
}
