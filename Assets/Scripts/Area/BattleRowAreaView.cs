using UnityEngine;
using TMPro;

// 打出的牌所在区域，一个RowArea为一排，包括这一排的分数、指挥牌、普通牌
public class BattleRowAreaView : MonoBehaviour
{
    public GameObject scoreNum;
    public GameObject normalAreaView; // 角色牌对战区

    // 每次操作完，更新ui
    public void UpdateUI()
    {
        normalAreaView.GetComponent<RowAreaView>().UpdateUI();
        scoreNum.GetComponent<TextMeshProUGUI>().text = normalAreaView.GetComponent<RowAreaView>().rowAreaModel.GetCurrentPower().ToString();
    }
}
