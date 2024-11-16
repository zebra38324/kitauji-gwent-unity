using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

// 卡牌管理器，保存已弃用的卡牌
public class DiscardCardManager
{
    private List<GameObject> cardList = new List<GameObject>();

    public void AddCard(GameObject card)
    {
        cardList.Add(card);
    }

    public void RemoveCard(GameObject card)
    {
        cardList.Remove(card);
    }

    public List<GameObject> GetCardList()
    {
        return new List<GameObject>(cardList);
    }
}
