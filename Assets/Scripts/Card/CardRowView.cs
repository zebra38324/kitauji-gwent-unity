using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 一行卡牌的区域ui
// 基类不包含数值计算，用于手牌区、弃牌区
// 派生类将添加数值计算逻辑，用于对战区
public class CardRowView : MonoBehaviour
{
    protected float rowWidth;

    protected float rowHeight;

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
        rowWidth = gameObject.GetComponent<RectTransform>().rect.width;
        rowHeight = gameObject.GetComponent<RectTransform>().rect.height;
        hasInit = true;
    }

    public void AddCard(GameObject newCard) // TODO 同时添加多张牌
    {
        Init(); // 这里的init实现实际上有些问题，主要为了兼容弃牌区Start函数调用过晚地问题，TODO: 尝试更合适地解决方案
        newCard.transform.SetParent(gameObject.transform);
        cardList.Add(newCard);
        SetCardSize(newCard);
        ReArrange();
    }

    public void RemoveCard(int cardId)
    {
        foreach (GameObject card in cardList) {
            if (card.GetComponent<CardController>().GetId() == cardId) {
                cardList.Remove(card);
                card.transform.SetParent(null);
                break;
            }
        }
        ReArrange();
    }

    // 是否已放满，放满之后要进行堆叠放置
    private bool IsAreaFull(float gap = 5f)
    {
        if (cardList.Count == 0) {
            return false;
        }
        float cardWidth = cardList[0].transform.localScale.x * cardList[0].GetComponent<RectTransform>().rect.width; // TODO 间隔
        return (cardWidth + gap) * cardList.Count > rowWidth;
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
        float step = 0f;
        if (!IsAreaFull(gap)) {
            currentPosition = -(cardWidth + gap) * cardList.Count / 2;
            step = cardWidth + gap;
        } else {
            currentPosition = -(rowWidth - gap) / 2;
            step = (rowWidth - gap - cardWidth) / (cardList.Count - 1); // step * count + (width - step) = area - gap
        }

        // 重新排布位置
        foreach (GameObject card in cardList) {
            card.transform.localPosition = new Vector3(currentPosition, 0, 0);
            currentPosition += step;
        }
    }

    private void SetCardSize(GameObject card)
    {
        float originCardHeight = card.GetComponent<RectTransform>().rect.height;
        float gap = 4f; // 上下gap
        float realCardHeight = rowHeight - gap;
        float scale = realCardHeight / originCardHeight;
        card.transform.localScale = Vector3.one * scale;
        card.transform.localPosition = new Vector3(0, 0, 0);
    }
}
