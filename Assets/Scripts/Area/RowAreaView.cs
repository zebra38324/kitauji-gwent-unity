using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * 行区域的逻辑，包括手牌、对战区的行、弃牌区的行等
 */
public class RowAreaView : MonoBehaviour
{
    private RowAreaModel rowAreaModel_;
    public RowAreaModel rowAreaModel {
        get {
            return rowAreaModel_;
        }
        set {
            rowAreaModel_ = value;
            Init();
        }
    }

    protected float areaWidth;
    protected float areaHeight;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // 每次操作完，更新ui
    public void UpdateUI()
    {
        foreach (CardModel cardModel in rowAreaModel.cardList) {
            GameObject card = CardViewCollection.Instance.Get(cardModel);
            card.transform.SetParent(gameObject.transform);
            SetCardSize(card);
            card.GetComponent<CardDisplay>().UpdateUI();
        }
        ReArrange();
    }

    private void Init()
    {
        areaWidth = gameObject.GetComponent<RectTransform>().rect.width;
        areaHeight = gameObject.GetComponent<RectTransform>().rect.height;
    }

    // 根据area尺寸调整card尺寸
    private void SetCardSize(GameObject card)
    {
        float originCardHeight = card.GetComponent<RectTransform>().rect.height;
        float gap = 4f; // 上下gap
        float realCardHeight = areaHeight - gap;
        float scale = realCardHeight / originCardHeight;
        card.transform.localScale = Vector3.one * scale;
    }

    // 添加或移出卡片时，重新排布位置
    private void ReArrange()
    {
        if (rowAreaModel.cardList.Count == 0) {
            return;
        }
        GameObject firstCard = CardViewCollection.Instance.Get(rowAreaModel.cardList[0]);
        float cardWidth = firstCard.transform.localScale.x * firstCard.GetComponent<RectTransform>().rect.width;
        float currentPosition = 0f;
        float gap = 5f; // 左右gap
        float step = 0f;
        if (!IsAreaFull(gap)) {
            currentPosition = -(cardWidth + gap) * rowAreaModel.cardList.Count / 2;
            step = cardWidth + gap;
        } else {
            currentPosition = -(areaWidth - gap) / 2;
            step = (areaWidth - gap - cardWidth) / (rowAreaModel.cardList.Count - 1); // step * count + (width - step) = area - gap
        }

        // 重新排布位置
        foreach (CardModel cardModel in rowAreaModel.cardList) {
            GameObject card = CardViewCollection.Instance.Get(cardModel);
            card.GetComponent<CardDisplay>().UpdatePosition(new Vector3(currentPosition, 0, 0));
            currentPosition += step;
        }
    }

    // 是否已放满，放满之后要进行堆叠放置
    private bool IsAreaFull(float gap = 5f)
    {
        if (rowAreaModel.cardList.Count == 0) {
            return false;
        }
        GameObject firstCard = CardViewCollection.Instance.Get(rowAreaModel.cardList[0]);
        float cardWidth = firstCard.transform.localScale.x * firstCard.GetComponent<RectTransform>().rect.width;
        return (cardWidth + gap) * rowAreaModel.cardList.Count > areaWidth;
    }
}
