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

    private bool hasInit = false;

    // Start is called before the first frame update
    void Start()
    {
        Init();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void Init()
    {
        if (hasInit) {
            return;
        }
        cardAreaWidth = cardArea.GetComponent<RectTransform>().rect.width;
        cardAreaHeight = cardArea.GetComponent<RectTransform>().rect.height;
        hasInit = true;
    }

    public virtual void AddCard(GameObject newCard) // TODO 同时添加多张牌
    {
        Init(); // 这里的init实现实际上有些问题，主要为了兼容弃牌区Start函数调用过晚地问题，TODO: 尝试更合适地解决方案
        newCard.transform.SetParent(cardArea.transform);
        cardList.Add(newCard);
        SetCardSize(newCard);
        ReArrange();
    }

    public virtual void RemoveCard(GameObject card)
    {
        cardList.Remove(card);
        ReArrange();
        card.transform.SetParent(null);
    }

    // 此处仅弃牌区调用。TODO: 优化，继承一下
    public void RemoveAllCard()
    {
        List<GameObject> tempList = new List<GameObject>();
        foreach(GameObject card in cardList) {
            tempList.Add(card);
        }
        foreach(GameObject card in tempList) {
            RemoveCard(card);
        }
    }

    public bool ExistCard(GameObject card)
    {
        return cardList.Exists(t => t.GetComponent<CardDisplay>().GetCardInfo().englishName == card.GetComponent<CardDisplay>().GetCardInfo().englishName);
    }

    // 是否已放满，放满之后要进行堆叠放置
    public bool IsAreaFull(float gap = 5f)
    {
        if (cardList.Count == 0) {
            return false;
        }
        float cardWidth = cardList[0].transform.localScale.x * cardList[0].GetComponent<RectTransform>().rect.width;
        return (cardWidth + gap) * cardList.Count > cardAreaWidth;
    }

    // 添加或移出卡片时，重新排布位置
    private void ReArrange()
    {
        if (cardList.Count == 0) {
            return;
        }
        float cardWidth = cardList[0].transform.localScale.x * cardList[0].GetComponent<RectTransform>().rect.width;
        float currentPosition = 0f;
        float gap = 5f; // 左右gap
        float step = 0f;
        if (!IsAreaFull(gap))
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
