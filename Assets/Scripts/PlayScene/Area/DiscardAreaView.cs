using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

// 打出的牌所在区域，一个RowArea为一排，包括这一排的分数、指挥牌、普通牌
public class DiscardAreaView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public GameObject row0;
    public GameObject row1;
    public GameObject row2;
    public GameObject tipText;

    private static readonly string defaultTip = "点击其他区域关闭";
    private static readonly string medicTip = "请选择要复活的卡牌";

    private bool isPointerInside = false;

    public bool isShowing { get; private set; } = false;

    private bool showSelf = false;

    private bool isMedic = false;

    private DiscardAreaModel selfDiscardAreaModel;

    private DiscardAreaModel enemyDiscardAreaModel;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetMouseButtonDown(0)) {
            if (!isPointerInside) {
                PlaySceneManager.Instance.HandleMessage(SceneMsg.HideDiscardArea);
            }
        }
    }

    // model变化时，尝试更新ui
    public void UpdateModel(DiscardAreaModel selfModel, DiscardAreaModel enemyModel)
    {
        if (selfDiscardAreaModel == selfModel && enemyDiscardAreaModel == enemyModel) {
            return;
        }
        selfDiscardAreaModel = selfModel;
        enemyDiscardAreaModel = enemyModel;
        UpdateUI();
    }

    public void ShowArea(bool showSelf_, bool isMedic_)
    {
        isShowing = true;
        showSelf = showSelf_;
        isMedic = isMedic_;
        UpdateUI();
        if (isMedic) {
            tipText.GetComponent<TextMeshProUGUI>().text = medicTip + "\n" + defaultTip;
        }
        gameObject.SetActive(true);
    }

    public void HideArea()
    {
        isShowing = false;
        // 先把ui移除
        DiscardAreaModel model = showSelf ? selfDiscardAreaModel : enemyDiscardAreaModel;
        foreach (CardModel cardModel in model.cardListModel.cardList) {
            GameObject card = CardViewCollection.Instance.Get(cardModel);
            card.transform.SetParent(null);
        }
        gameObject.SetActive(false);
        tipText.GetComponent<TextMeshProUGUI>().text = defaultTip;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isPointerInside = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isPointerInside = false;
    }

    private void UpdateUI()
    {
        if (!isShowing) {
            return;
        }
        DiscardAreaModel model = showSelf ? selfDiscardAreaModel : enemyDiscardAreaModel;
        List<List<CardModel>> showList = model.GetShowList(isMedic);
        row0.GetComponent<RowAreaView>().UpdateModel(new CardListModel().AddCardList(showList[0].ToImmutableList()));
        row1.GetComponent<RowAreaView>().UpdateModel(new CardListModel().AddCardList(showList[1].ToImmutableList()));
        row2.GetComponent<RowAreaView>().UpdateModel(new CardListModel().AddCardList(showList[2].ToImmutableList()));
    }
}
