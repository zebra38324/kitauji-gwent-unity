using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DeckCardAreaView : MonoBehaviour
{
    private static string TAG = "DeckCardAreaView";

    public GameObject deckCardAreaViewTable;

    public GameObject deckCardAreaViewTableCellPrefab;

    public List<GameObject> cardList { get; set; }

    private List<GameObject> cellList;

    private int cellNum = 0;

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
        UpdateUI();
    }

    public void RemoveCard(GameObject card)
    {
        cardList.Remove(card);
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (cellNum < cardList.Count) {
            int diff = cardList.Count - cellNum;
            for (int i = 0; i < diff; i++) {
                GameObject cell = GameObject.Instantiate(deckCardAreaViewTableCellPrefab, deckCardAreaViewTable.transform);
                cellList.Add(cell);
                cellNum += 1;
            }
        }
        for (int i = 0; i < cardList.Count; i++) {
            GameObject cell = cellList[i];
            GameObject card = cardList[i];
            card.transform.SetParent(cell.transform);

            float originCardHeight = card.GetComponent<RectTransform>().rect.height;
            float gap = 30f; // 上下gap
            float realCardHeight = cell.GetComponent<RectTransform>().rect.height - gap;
            float scale = realCardHeight / originCardHeight;
            card.transform.localScale = Vector3.one * scale;
            float realCardWidth = card.GetComponent<RectTransform>().rect.width * scale;
            card.transform.localPosition = new Vector3(-realCardWidth / 2, 0, 0);
        }
    }
}
