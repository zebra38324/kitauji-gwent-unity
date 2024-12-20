using System.Collections.Generic;
using TMPro;
using UnityEngine;

// 包含三行打出的牌，及分数区域
public class SinglePlayerAreaView : MonoBehaviour
{
    private static string TAG = "SinglePlayerAreaView";
    public GameObject woodRow;
    public GameObject brassRow;
    public GameObject percussionRow;
    public GameObject handCardRow; // self时不为空，enemy为空
    public GameObject offFieldCardsArea;
    public GameObject playStat;

    private SinglePlayerAreaModel model_;
    public SinglePlayerAreaModel model {
        get {
            return model_;
        }
        set {
            model_ = value;
            woodRow.GetComponent<BattleRowAreaView>().normalAreaView.GetComponent<RowAreaView>().rowAreaModel = model_.woodRowAreaModel;
            brassRow.GetComponent<BattleRowAreaView>().normalAreaView.GetComponent<RowAreaView>().rowAreaModel = model_.brassRowAreaModel;
            percussionRow.GetComponent<BattleRowAreaView>().normalAreaView.GetComponent<RowAreaView>().rowAreaModel = model_.percussionRowAreaModel;
            if (handCardRow != null ) {
                handCardRow.GetComponent<RowAreaView>().rowAreaModel = model_.handRowAreaModel;
            }
            offFieldCardsArea.GetComponent<OffFieldCardsAreaView>().model = model_;
        }
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    // 每次操作完，更新ui
    public void UpdateUI()
    {
        if (handCardRow != null) {
            handCardRow.GetComponent<RowAreaView>().UpdateUI();
        }
        offFieldCardsArea.GetComponent<OffFieldCardsAreaView>().UpdateUI();
        woodRow.GetComponent<BattleRowAreaView>().UpdateUI();
        brassRow.GetComponent<BattleRowAreaView>().UpdateUI();
        percussionRow.GetComponent<BattleRowAreaView>().UpdateUI();
        playStat.GetComponent<PlayStatAreaView>().UpdateUI();

        // 移除卡牌
        foreach (CardModel cardModel in model.discardAreaModel.cardList) {
            GameObject card = CardViewCollection.Instance.Get(cardModel);
            if (card.transform.parent == woodRow.GetComponent<BattleRowAreaView>().normalAreaView.transform ||
                card.transform.parent == brassRow.GetComponent<BattleRowAreaView>().normalAreaView.transform ||
                card.transform.parent == percussionRow.GetComponent<BattleRowAreaView>().normalAreaView.transform) {
                card.transform.SetParent(null);
            }
        }
    }
}
