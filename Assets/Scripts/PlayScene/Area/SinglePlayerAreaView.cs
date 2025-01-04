using System.Collections.Generic;
using TMPro;
using UnityEngine;

// 包含三行打出的牌，及分数区域
public class SinglePlayerAreaView : MonoBehaviour
{
    public GameObject woodRow;
    public GameObject brassRow;
    public GameObject percussionRow;
    public GameObject handCardRow; // self时不为空，enemy为空
    public GameObject offFieldCardsArea;
    public GameObject playStat;
    public GameObject leaderArea;

    private SinglePlayerAreaModel model_;
    public SinglePlayerAreaModel model {
        get {
            return model_;
        }
        set {
            model_ = value;
            woodRow.GetComponent<BattleRowAreaView>().battleRowAreaModel = model_.woodRowAreaModel;
            brassRow.GetComponent<BattleRowAreaView>().battleRowAreaModel = model_.brassRowAreaModel;
            percussionRow.GetComponent<BattleRowAreaView>().battleRowAreaModel = model_.percussionRowAreaModel;
            if (handCardRow != null ) {
                handCardRow.GetComponent<RowAreaView>().rowAreaModel = model_.handRowAreaModel;
            }
            offFieldCardsArea.GetComponent<OffFieldCardsAreaView>().model = model_;
            leaderArea.GetComponent<RowAreaView>().rowAreaModel = model_.leaderCardAreaModel;
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
        leaderArea.GetComponent<RowAreaView>().UpdateUI();
    }
}
