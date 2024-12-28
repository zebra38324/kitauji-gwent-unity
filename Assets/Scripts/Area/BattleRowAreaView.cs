using UnityEngine;
using TMPro;
using System.Collections.Generic;

// 打出的牌所在区域，一个RowArea为一排，包括这一排的分数、指挥牌、普通牌
public class BattleRowAreaView : MonoBehaviour
{
    private static string TAG = "BattleRowAreaView";

    public GameObject scoreNum;
    public GameObject hornAreaView; // horn区
    public GameObject hornAreaViewButton; // horn区按钮
    public GameObject normalAreaView; // 角色牌对战区
    public GameObject weatherEffect;

    private BattleRowAreaModel battleRowAreaModel_;
    public BattleRowAreaModel battleRowAreaModel {
        get {
            return battleRowAreaModel_;
        }
        set {
            battleRowAreaModel_ = value;
            hornAreaView.GetComponent<RowAreaView>().rowAreaModel = battleRowAreaModel_.hornUtilCardArea;
            normalAreaView.GetComponent<RowAreaView>().rowAreaModel = battleRowAreaModel_;
        }
    }

    // 每次操作完，更新ui
    public void UpdateUI()
    {
        hornAreaView.GetComponent<RowAreaView>().UpdateUI();
        normalAreaView.GetComponent<RowAreaView>().UpdateUI();
        RemoveUnuseCard();
        scoreNum.GetComponent<TextMeshProUGUI>().text = normalAreaView.GetComponent<RowAreaView>().rowAreaModel.GetCurrentPower().ToString();
        weatherEffect.SetActive(battleRowAreaModel.hasWeatherBuff);
    }

    public void HornAreaViewButtonOnClick()
    {
        KLog.I(TAG, "HornAreaViewButtonOnClick");
        PlaySceneManager.Instance.HandleMessage(SceneMsg.ClickHornAreaViewButton, battleRowAreaModel);
    }

    public void ShowHornAreaViewButton()
    {
        hornAreaViewButton.SetActive(true);
    }

    public void HideHornAreaViewButton()
    {
        hornAreaViewButton.SetActive(false);
    }

    private void RemoveUnuseCard()
    {
        List<GameObject> removeList = new List<GameObject>();
        for (int i = 0; i < normalAreaView.transform.childCount; i++) {
            GameObject child = normalAreaView.transform.GetChild(i).gameObject;
            if (child.GetComponent<CardDisplay>().cardModel.cardLocation != CardLocation.BattleArea) {
                removeList.Add(child);
            }
        }
        foreach (GameObject card in removeList) {
            // location不为BattleArea，需要从ui上移除
            card.transform.SetParent(null);
        }
    }
}
