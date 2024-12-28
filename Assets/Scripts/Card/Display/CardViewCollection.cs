using System;
using System.Collections.Generic;
using UnityEngine;
/**
 * 存储HalfCard的GameObject，利用CardModel获取GameObject
 */
public class CardViewCollection
{
    private static readonly CardViewCollection instance = new CardViewCollection();

    static CardViewCollection() { }

    private CardViewCollection() { }

    public static CardViewCollection Instance {
        get {
            return instance;
        }
    }

    private static GameObject cardPrefab;

    // key: id
    private Dictionary<int, GameObject> cardMap = new Dictionary<int, GameObject>();

    public GameObject Get(CardModel model)
    {
        int id = model.cardInfo.id;
        if (cardMap.ContainsKey(id)) {
            return cardMap[id];
        }
        GameObject card = GenCard(model);
        cardMap[id] = card;
        return card;
    }

    public void Clear()
    {
        cardMap.Clear();
    }

    private GameObject GenCard(CardModel model)
    {
        if (cardPrefab == null) {
            cardPrefab = Resources.Load<GameObject>("Prefabs/HalfCard");
        }
        GameObject card = GameObject.Instantiate(cardPrefab);
        card.GetComponent<CardDisplay>().SetCardModel(model);
        card.GetComponent<CardDisplay>().SendSceneMsgCallback += PlaySceneManager.Instance.HandleMessage;
        return card;
    }
}
