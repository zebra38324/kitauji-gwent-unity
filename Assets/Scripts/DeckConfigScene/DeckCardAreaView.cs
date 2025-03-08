using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class DeckCardAreaView : MonoBehaviour
{
    public GameObject deckCardAreaViewTable;

    public GameObject deckCardAreaViewTableCellPrefab;

    public enum ShowType
    {
        All = 0,
        NormalRoleCard,
        HeroRoleCard,
        UtilCard,
        LearCard,
        Count,
    }

    private List<bool> showTypeList;

    public List<GameObject> cardList { get; private set; }

    private List<GameObject> showCardList; // 用于展示的卡牌列表

    private List<GameObject> cellList;

    void Awake()
    {
        cardList = new List<GameObject>();
        showCardList = new List<GameObject>();
        cellList = new List<GameObject>();
        showTypeList = Enumerable.Repeat(true, (int)ShowType.Count).ToList();
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void AddCardList(List<GameObject> newCardList)
    {
        if (newCardList.Count == 0) {
            return;
        }
        cardList.AddRange(newCardList);
        UpdateUI();
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

    public void RemoveAllCard()
    {
        foreach (GameObject cell in cellList) {
            Destroy(cell);
        }
        foreach (GameObject card in cardList) {
            Destroy(card);
        }
        foreach (GameObject card in showCardList) {
            Destroy(card);
        }
        cellList.Clear();
        cardList.Clear();
        showCardList.Clear();
    }

    public void UpdateShowType(List<bool> newShowTypeList)
    {
        showTypeList = new List<bool>(newShowTypeList.ToArray());
        UpdateUI();
    }

    private void UpdateUI()
    {
        cardList.Sort((GameObject x, GameObject y) => x.GetComponent<CardDisplay>().cardModel.cardInfo.infoId.CompareTo(y.GetComponent<CardDisplay>().cardModel.cardInfo.infoId));
        UpdateShowCardList();

        while (cellList.Count < showCardList.Count) {
            GameObject cell = GameObject.Instantiate(deckCardAreaViewTableCellPrefab, deckCardAreaViewTable.transform);
            cellList.Add(cell);
        }
        while (cellList.Count > showCardList.Count) {
            // 销毁多余的cell，销毁前把card移除，以免把card也销毁了
            GameObject cell = cellList[cellList.Count - 1];
            cell.GetComponent<SingleCardAreaView>().RemoveCard();
            Destroy(cell);
            cellList.RemoveAt(cellList.Count - 1);
        }
        for (int i = 0; i < showCardList.Count; i++) {
            GameObject cell = cellList[i];
            GameObject card = showCardList[i];
            cell.GetComponent<SingleCardAreaView>().AddCard(card);
        }
        for (int i = showCardList.Count; i < cellList.Count; i++) {
            GameObject cell = cellList[i];
            cell.GetComponent<SingleCardAreaView>().RemoveCard();
        }
    }

    private void UpdateShowCardList()
    {
        List<GameObject> newShowCardList = new List<GameObject>();
        foreach (GameObject card in cardList) {
            CardModel cardModel = card.GetComponent<CardDisplay>().cardModel;
            bool needShow = false;
            switch (cardModel.cardInfo.cardType) {
                case CardType.Normal: {
                    needShow = showTypeList[(int)ShowType.All] || showTypeList[(int)ShowType.NormalRoleCard];
                    break;
                }
                case CardType.Hero: {
                    needShow = showTypeList[(int)ShowType.All] || showTypeList[(int)ShowType.HeroRoleCard];
                    break;
                }
                case CardType.Util: {
                    needShow = showTypeList[(int)ShowType.All] || showTypeList[(int)ShowType.UtilCard];
                    break;
                }
                case CardType.Leader: {
                    needShow = showTypeList[(int)ShowType.All] || showTypeList[(int)ShowType.LearCard];
                    break;
                }
            }
            if (needShow) {
                newShowCardList.Add(card);
            }
        }
        showCardList = newShowCardList;
        showCardList.Sort((GameObject x, GameObject y) => x.GetComponent<CardDisplay>().cardModel.cardInfo.infoId.CompareTo(y.GetComponent<CardDisplay>().cardModel.cardInfo.infoId));
    }
}
