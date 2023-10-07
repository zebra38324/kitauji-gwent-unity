using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CardInfoDisplay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{    
    public GameObject entireCardPrefeb;

    private GameObject cardInfoArea;

    private GameObject entireCard;

    private bool isCardUp = false; // 卡片是否是抬升状态

    // Start is called before the first frame update
    void Start()
    {
        cardInfoArea = GameObject.Find("CardInfoArea");
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetIsCardUp(bool flag)
    {
        isCardUp = flag;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isCardUp) {
            return;
        }
        isCardUp = true;
        transform.Translate(0, 10, 0); // 鼠标悬浮时，卡片上移
        
        entireCard = GameObject.Instantiate(entireCardPrefeb, cardInfoArea.transform);
        entireCard.GetComponent<CardDisplay>().SetCardInfo(gameObject.GetComponent<CardDisplay>().GetCardInfo());
        float areaWidth = cardInfoArea.GetComponent<RectTransform>().rect.width;
        float areaHeight = cardInfoArea.GetComponent<RectTransform>().rect.height;
        float scale = areaWidth / entireCard.GetComponent<RectTransform>().rect.width;
        float cardHeight = entireCard.GetComponent<RectTransform>().rect.height;
        entireCard.transform.localScale = Vector3.one * scale;
        entireCard.transform.localPosition = new Vector3(-areaWidth / 2, (areaHeight - cardHeight * scale) / 2, 0);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (isCardUp) {
            transform.Translate(0, -10, 0); // 鼠标悬移出时，卡片恢复
        }
        isCardUp = false;
        Destroy(entireCard);
    }
}
