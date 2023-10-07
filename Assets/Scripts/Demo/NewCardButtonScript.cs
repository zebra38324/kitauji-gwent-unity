using System.Collections;
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
        Debug.Log("cardsInfo num = " + cardsInfo.Count);
        Debug.Log("imageName =  " + cardsInfo[0].imageName + " badgeType: " + cardsInfo[0].badgeType);

        GameObject newCard = GameObject.Instantiate(cardPrefab, handArea.transform);
        newCard.GetComponent<CardDisplay>().SetCardInfo(cardsInfo[Random.Range(0, cardsInfo.Count)]);

        handArea.GetComponent<HandArea>().AddCard(newCard);
}
}
