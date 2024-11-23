using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestEnemyButtonScript : MonoBehaviour
{
    public GameObject enemyArea;
    public GameObject cardPrefab;

    public TextAsset cardInfoStatistic;

    private List<CardInfo> cardsInfo;
    private int cardsIndex = 0;

    // Start is called before the first frame update
    void Start()
    {
        cardsInfo = StatisticJsonParse.GetCardInfo(cardInfoStatistic.text);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetEnemyCard()
    {
        //GameObject newCard = GameObject.Instantiate(cardPrefab, enemyArea.transform);
        //newCard.GetComponent<CardDisplay>().SetCardInfo(cardsInfo[cardsIndex]);
        //newCard.GetComponent<CardAction>().cardLocation = CardLocation.EnemyBattleArea;
        //enemyArea.GetComponent<SinglePlayerArea>().AddNormalCard(newCard);
        //cardsIndex += 1;
        //if (cardsIndex >= cardsInfo.Count) {
        //    cardsIndex = 0;
        //}
    }
}
