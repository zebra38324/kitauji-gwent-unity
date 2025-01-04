using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DeckCardAreaView : MonoBehaviour
{
    public GameObject deckCardAreaViewTable;

    public GameObject deckCardAreaViewTableCellPrefab;

    public List<GameObject> cardList { get; private set; }

    private List<GameObject> cellList;

    // Start is called before the first frame update
    void Start()
    {
        cardList = new List<GameObject>();
        cellList = new List<GameObject>();
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void AddCard(GameObject card)
    {
        cardList.Add(card);
        cardList.Sort((GameObject x, GameObject y) => x.GetComponent<CardDisplay>().cardModel.cardInfo.infoId.CompareTo(y.GetComponent<CardDisplay>().cardModel.cardInfo.infoId));
        UpdateUI();
    }

    public void RemoveCard(GameObject card)
    {
        cardList.Remove(card);
        UpdateUI();
    }

    private void UpdateUI()
    {
        while (cellList.Count < cardList.Count) {
            GameObject cell = GameObject.Instantiate(deckCardAreaViewTableCellPrefab, deckCardAreaViewTable.transform);
            cellList.Add(cell);
        }
        for (int i = 0; i < cardList.Count; i++) {
            GameObject cell = cellList[i];
            GameObject card = cardList[i];
            cell.GetComponent<SingleCardAreaView>().AddCard(card);
        }
        for (int i = cardList.Count; i < cellList.Count; i++) {
            GameObject cell = cellList[i];
            cell.GetComponent<SingleCardAreaView>().RemoveCard();
        }
    }
}
