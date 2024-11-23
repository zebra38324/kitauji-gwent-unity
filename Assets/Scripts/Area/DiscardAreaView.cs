using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

// 打出的牌所在区域，一个RowArea为一排，包括这一排的分数、指挥牌、普通牌
public class DiscardAreaView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private static string TAG = "DiscardAreaView";
    public GameObject row0;
    public GameObject row1;
    public GameObject row2;
    public GameObject tipText;

    private static readonly string defaultTip = "点击其他区域关闭";
    private static readonly string medicTip = "请选择要复活的卡牌";

    private bool isPointerInside = false;

    private DiscardAreaModel model_;
    private DiscardAreaModel model {
        get {
            return model_;
        }
        set {
            model_ = value;
            if (model_ == null) {
                return;
            }
            row0.GetComponent<RowAreaView>().rowAreaModel = model_.rowAreaList[0];
            row1.GetComponent<RowAreaView>().rowAreaModel = model_.rowAreaList[1];
            row2.GetComponent<RowAreaView>().rowAreaModel = model_.rowAreaList[2];
        }
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetMouseButtonDown(0)) {
            if (!isPointerInside) {
                PlaySceneManager.Instance.HandleMessage(PlaySceneManager.PlaySceneMsg.HideDiscardArea);
            }
        }
    }

    // 是否是Medic技能触发的展示
    public void ShowArea(DiscardAreaModel discardAreaModel, bool isMedic)
    {
        model = discardAreaModel;
        row0.GetComponent<RowAreaView>().UpdateUI();
        row1.GetComponent<RowAreaView>().UpdateUI();
        row2.GetComponent<RowAreaView>().UpdateUI();
        if (isMedic) {
            tipText.GetComponent<TextMeshProUGUI>().text = medicTip + "\n" + defaultTip;
        }
        gameObject.SetActive(true);
    }

    public void HideArea()
    {
        row0.GetComponent<RowAreaView>().UpdateUI();
        row1.GetComponent<RowAreaView>().UpdateUI();
        row2.GetComponent<RowAreaView>().UpdateUI();
        gameObject.SetActive(false);
        tipText.GetComponent<TextMeshProUGUI>().text = defaultTip;
        model = null;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isPointerInside = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isPointerInside = false;
    }
}
