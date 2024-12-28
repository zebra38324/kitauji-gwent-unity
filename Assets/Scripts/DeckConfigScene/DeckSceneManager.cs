using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeckSceneManager : MonoBehaviour
{
    public GameObject backupArea;

    public GameObject selectedArea;

    public GameObject cardInfoArea;

    public GameObject cardPrefeb;

    private long lastTs = 0;

    private int index = 2001;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (KTime.CurrentMill() - lastTs > 500) {
            lastTs = KTime.CurrentMill();
            Test(index);
            index += 1;
        }
    }

    public void HandleMessage(SceneMsg msg, params object[] list)
    {
        switch (msg) {
            case SceneMsg.ShowCardInfo: {
                CardInfo info = (CardInfo)list[0];
                cardInfoArea.GetComponent<CardInfoAreaView>().ShowInfo(info);
                break;
            }
            case SceneMsg.HideCardInfo: {
                cardInfoArea.GetComponent<CardInfoAreaView>().HideInfo();
                break;
            }
            case SceneMsg.ChooseCard: {
                CardModel cardModel = (CardModel)list[0];
                break;
            }
        }
    }

    private void Test(int infoId)
    {
        CardGenerator cardGenerator = new CardGenerator();
        GameObject card = GameObject.Instantiate(cardPrefeb, null);
        card.GetComponent<CardDisplay>().SetCardModel(cardGenerator.GetCard(infoId));
        card.GetComponent<CardDisplay>().SendSceneMsgCallback += HandleMessage;
        backupArea.GetComponent<DeckCardAreaView>().AddCard(card);
    }
}
