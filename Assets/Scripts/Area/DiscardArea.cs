using System.Collections.Generic;
using UnityEngine;

// 打出的牌所在区域，一个RowArea为一排，包括这一排的分数、指挥牌、普通牌
public class DiscardArea : MonoBehaviour
{
    public GameObject discardArea;
    public GameObject row1;
    public GameObject row2;
    public GameObject row3;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ShowCard(bool isSelf)
    {
        DiscardCardManager manager = SelfDiscardCardManager.Instance;
        if (!isSelf) {
            manager = EnemyDiscardCardManager.Instance;
        }
        List<GameObject> cardList = manager.GetCardList();
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
}
