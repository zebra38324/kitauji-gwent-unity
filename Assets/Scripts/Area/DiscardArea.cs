using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

// 打出的牌所在区域，一个RowArea为一排，包括这一排的分数、指挥牌、普通牌
public class DiscardArea : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public GameObject discardArea;
    public GameObject row1;
    public GameObject row2;
    public GameObject row3;

    private bool isPointerInside = false;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetMouseButtonDown(0)) {
            if (!isPointerInside) {
                CloseArea();
            }
        }
    }

    public void ShowCard(List<GameObject> cardList)
    {
        foreach(GameObject card in cardList) {
            if (!row1.GetComponent<CardArea>().IsAreaFull(30)) {
                row1.GetComponent<CardArea>().AddCard(card);
            } else if (!row2.GetComponent<CardArea>().IsAreaFull(30)) {
                row2.GetComponent<CardArea>().AddCard(card);
            } else {
                row3.GetComponent<CardArea>().AddCard(card);
            }
        }
    }

    public void HideCard()
    {
        row1.GetComponent<CardArea>().RemoveAllCard();
        row2.GetComponent<CardArea>().RemoveAllCard();
        row3.GetComponent<CardArea>().RemoveAllCard();
    }

    public void RemoveCard(GameObject card)
    {
        Debug.Log("Remove card " + card.GetComponent<CardDisplay>().GetCardInfo().chineseName);
        if (row1.GetComponent<CardArea>().ExistCard(card)) {
            Debug.Log("Remove card in row1");
            row1.GetComponent<CardArea>().RemoveCard(card);
        } else if (row2.GetComponent<CardArea>().ExistCard(card)) {
            Debug.Log("Remove card in row2");
            row2.GetComponent<CardArea>().RemoveCard(card);
        } else if (row3.GetComponent<CardArea>().ExistCard(card)) {
            Debug.Log("Remove card in row3");
            row3.GetComponent<CardArea>().RemoveCard(card);
        }
    }

    public void CloseArea()
    {
        HideCard();
        gameObject.SetActive(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isPointerInside = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isPointerInside = false;
    }
}
