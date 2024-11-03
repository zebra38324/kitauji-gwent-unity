using System.Collections.Generic;
/*
 * 卡牌集合模型，包括手牌区、一行对战区等卡牌集合的逻辑处理
 * 对战区等需要计算的逻辑将后续交由派生类处理
 */
public class CardCollectionModel
{
    private List<CardModel> cardModelList;

    public CardCollectionModel()
    {
        cardModelList = new List<CardModel>();
    }

    // TODO: index应由controller感知，并加在cardinfo中
    public void AddCard(CardInfo cardInfo)
    {
        CardModel cardModel = new CardModel(cardInfo);
        cardModelList.Add(cardModel);
    }

    public void RemoveCard(int index)
    {
        foreach (CardModel cardModel in cardModelList) {
            if (cardModel.GetIndex() == index) {
                cardModelList.Remove(cardModel);
                break;
            }
        }
    }
}