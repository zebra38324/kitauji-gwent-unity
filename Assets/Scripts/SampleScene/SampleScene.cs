using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 做视频用的，正式游戏中不用
public class SampleScene : MonoBehaviour
{
    private static string TAG = "SampleScene";

    public GameObject cardPrefab;

    public GameObject cellPrefab;

    private List<GameObject> cardList = new List<GameObject>();

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(GenAllCards());
    }

    // Update is called once per frame
    void Update()
    {

    }

    private IEnumerator GenAllCards()
    {
        yield return new WaitForSeconds(1);
        CardGenerator cardGenerator = new CardGenerator(true);
        List<CardModel> allCardModelList = cardGenerator.GetGroupCardList(CardGroup.KumikoFirstYear);
        allCardModelList.AddRange(cardGenerator.GetGroupCardList(CardGroup.Neutral));
        allCardModelList.AddRange(cardGenerator.GetGroupCardList(CardGroup.KumikoSecondYear));
        foreach (CardModel cardModel in allCardModelList) {
            GameObject cell = GameObject.Instantiate(cellPrefab, gameObject.transform);
            GameObject card = Instantiate(cardPrefab, null);
            card.GetComponent<CardDisplay>().SetCardModel(cardModel);
            cell.GetComponent<SingleCardAreaView>().AddCard(card);
            card.SetActive(false);
            cardList.Add(card);
        }
        // 补齐，好看一些
        for (int i = 30; i < 34; i++) {
            CardModel cardModel = allCardModelList[i];
            GameObject cell = GameObject.Instantiate(cellPrefab, gameObject.transform);
            GameObject card = Instantiate(cardPrefab, null);
            card.GetComponent<CardDisplay>().SetCardModel(cardModel);
            cell.GetComponent<SingleCardAreaView>().AddCard(card);
            card.SetActive(false);
            cardList.Add(card);
        }

        KLog.I(TAG, "start wait");
        yield return new WaitForSeconds(3);
        float totalDuration = 10f;
        float interval = totalDuration / cardList.Count;

        while (cardList.Count > 0) {
            int showCount = Random.Range(1, Mathf.Min(3, cardList.Count));
            while (showCount > 0) {
                int index = Random.Range(0, cardList.Count);
                GameObject card = cardList[index];
                cardList.RemoveAt(index);
                card.SetActive(true);
                showCount -= 1;
            }
            yield return new WaitForSeconds(Random.Range(0, interval));
        }
    }
}
