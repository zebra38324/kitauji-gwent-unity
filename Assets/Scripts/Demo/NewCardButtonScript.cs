using System;
using System.Collections.Generic;
using UnityEngine;

public class NewCardButtonScript : MonoBehaviour
{
    public GameObject handArea;
    public GameObject cardPrefab;

    public TextAsset cardInfoStatistic;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void CreateNewCard()
    {
        List<CardInfo> cardsInfo = StatisticJsonParse.GetCardInfo(cardInfoStatistic.text);
        BackupCardManager.Instance.SetCardInfoList(cardsInfo);

        List<CardInfo> loadCardInfo = BackupCardManager.Instance.GetCardInfos(20);
        foreach (CardInfo cardInfo in loadCardInfo) {
            GameObject newCard = GameObject.Instantiate(cardPrefab, handArea.transform);
            newCard.GetComponent<CardDisplay>().SetCardInfo(cardInfo);
            handArea.GetComponent<HandArea>().AddCard(newCard);
        }
    }
}
