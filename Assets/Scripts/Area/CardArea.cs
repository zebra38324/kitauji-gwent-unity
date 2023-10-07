using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// 卡牌区域，主要用于手牌的排布
public class CardArea : MonoBehaviour
{
    public GameObject cardArea;

    protected float cardAreaWidth;
    protected float cardAreaHeight;

    protected List<GameObject> cardList = new List<GameObject>();

    // Start is called before the first frame update
    void Start()
    {
        cardAreaWidth = cardArea.GetComponent<RectTransform>().rect.width;
        cardAreaHeight = cardArea.GetComponent<RectTransform>().rect.height;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void AddCard(GameObject newCard) // TODO 同时添加多张牌
    {
        cardList.Add(newCard);
        SetCardSize(newCard);
        ReArrange();
    }

    public void RemoveCard(GameObject card)
    {
        cardList.Remove(card);
        ReArrange();
        card.transform.SetParent(null);
    }

    // 添加或移出卡片时，重新排布位置
    private void ReArrange()
    {
        if (cardList.Count == 0) {
            return;
        }
        float cardWidth = cardList[0].transform.localScale.x * cardList[0].GetComponent<RectTransform>().rect.width; // TODO 间隔
        float currentPosition = 0f;
        float gap = 5f; // 左右gap
        bool tooManyCards = (cardWidth + gap) * cardList.Count > cardAreaWidth;
        float step = 0f;
        if (!tooManyCards)
        {
            currentPosition =  -(cardWidth + gap) * cardList.Count / 2;
            step = cardWidth + gap;
        } else
        {
            currentPosition = -(cardAreaWidth - gap) / 2;
            step = (cardAreaWidth - gap - cardWidth) / (cardList.Count - 1); // step * count + (width - step) = area - gap
        }

        // 重新排布位置
        foreach (GameObject card in cardList)
        {
            card.transform.localPosition = new Vector3(currentPosition, 0, 0);
            currentPosition += step;
        }
    }

    private void SetCardSize(GameObject card)
    {
        float originCardHeight = card.GetComponent<RectTransform>().rect.height;
        float gap = 4f; // 上下gap
        float realCardHeight = cardAreaHeight - gap;
        float scale = realCardHeight / originCardHeight;
        card.transform.localScale = Vector3.one * scale;
        card.transform.localPosition = new Vector3(0, 0, 0);
    }
}
