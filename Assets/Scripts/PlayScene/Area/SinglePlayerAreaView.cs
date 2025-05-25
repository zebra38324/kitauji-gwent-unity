using System;
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
    public GameObject leaderArea;

    private SinglePlayerAreaModel singlePlayerAreaModel;

    // Start is called before the first frame update
    void Start()
    {

    }

    // model变化时，尝试更新ui
    public void UpdateModel(SinglePlayerAreaModel model)
    {
        if (singlePlayerAreaModel == model) {
            return;
        }
        singlePlayerAreaModel = model;
        if (handCardRow != null) {
            handCardRow.GetComponent<RowAreaView>().UpdateModel(singlePlayerAreaModel.handCardAreaModel.handCardListModel);
        }
        offFieldCardsArea.GetComponent<OffFieldCardsAreaView>().UpdateModel(singlePlayerAreaModel.handCardAreaModel);
        woodRow.GetComponent<BattleRowAreaView>().UpdateModel(singlePlayerAreaModel.battleRowAreaList[(int)CardBadgeType.Wood]);
        brassRow.GetComponent<BattleRowAreaView>().UpdateModel(singlePlayerAreaModel.battleRowAreaList[(int)CardBadgeType.Brass]);
        percussionRow.GetComponent<BattleRowAreaView>().UpdateModel(singlePlayerAreaModel.battleRowAreaList[(int)CardBadgeType.Percussion]);
        leaderArea.GetComponent<RowAreaView>().UpdateModel(singlePlayerAreaModel.handCardAreaModel.leaderCardListModel);
    }
}
