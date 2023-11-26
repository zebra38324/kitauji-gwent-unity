using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CardInfoArea : MonoBehaviour
{
    public GameObject cardInfoTextArea;
    public GameObject cardInfoText;
    public GameObject entireCardPrefeb;

    private GameObject entireCard;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private string GetCardInfoText(CardInfo cardInfo)
    {
        string result = "";
        if (cardInfo.cardType == CardType.Hero) {
            result += "天王：吹奏实力不受其他因素影响。\n\n";
        }
        if (cardInfo.ability != CardAbility.None) {
            result += CardText.cardAbilityText[(int)cardInfo.ability] + "\n";
            if (cardInfo.ability == CardAbility.Attack) {
                result += "攻击能力：" + cardInfo.attackNum;
            }
            if (cardInfo.ability == CardAbility.Bond || cardInfo.ability == CardAbility.Muster) {
                result += "相关卡牌：" + cardInfo.relatedCard + "\n";
            }
        }
        return result;
    }

    public void ShowInfo(CardInfo cardInfo)
    {
        entireCard = GameObject.Instantiate(entireCardPrefeb, gameObject.transform);
        entireCard.GetComponent<CardDisplay>().SetCardInfo(cardInfo);
        float areaWidth = gameObject.GetComponent<RectTransform>().rect.width;
        float areaHeight = gameObject.GetComponent<RectTransform>().rect.height;
        float scale = areaWidth / entireCard.GetComponent<RectTransform>().rect.width;
        float cardHeight = entireCard.GetComponent<RectTransform>().rect.height;
        entireCard.transform.localScale = Vector3.one * scale;
        entireCard.transform.localPosition = new Vector3(-areaWidth / 2, (areaHeight - cardHeight * scale) / 2, 0);

        cardInfoText.GetComponent<TextMeshProUGUI>().text = GetCardInfoText(cardInfo);
        cardInfoTextArea.SetActive(true);
    }

    public void HideInfo()
    {
        Destroy(entireCard);
        entireCard = null;

        cardInfoTextArea.SetActive(false);
        cardInfoText.GetComponent<TextMeshProUGUI>().text = "";
    }
}
