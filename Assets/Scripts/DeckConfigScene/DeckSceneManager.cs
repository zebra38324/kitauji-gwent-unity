using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DeckSceneManager : MonoBehaviour
{
    private static string TAG = "DeckSceneManager";

    public GameObject backupArea;

    public GameObject selectedArea;

    public GameObject cardInfoArea;

    public GameObject leaderCardContainer;

    public GameObject toastView;

    public GameObject selectedCardNumText;

    public GameObject selectedRoleCardNumText;

    public GameObject selectedHeroCardNumText;

    public GameObject selectedUtilCardNumText;

    public GameObject cardPrefeb;

    public GameObject cardGroupSelect;

    public GameObject cardGroupAbilityText;

    private List<int> selectedInfoIdList;

    private CardGroup selectedCardGroup;

    private static int minSelectedCardNum = 15;

    // Start is called before the first frame update
    void Start()
    {
        long startTs = KTime.CurrentMill();
        Init();
        KLog.I(TAG, "Init cost " + (KTime.CurrentMill() - startTs) + " ms");
    }

    // Update is called once per frame
    void Update()
    {

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
                SelectCard(cardModel);
                break;
            }
        }
    }

    public void Save()
    {
        KLog.I(TAG, "Save");
        int selectedCardNum = 0;
        foreach (GameObject card in selectedArea.GetComponent<DeckCardAreaView>().cardList) {
            if (card.GetComponent<CardDisplay>().cardModel.cardInfo.cardType == CardType.Normal ||
                card.GetComponent<CardDisplay>().cardModel.cardInfo.cardType == CardType.Hero ||
                card.GetComponent<CardDisplay>().cardModel.cardInfo.cardType == CardType.Util) {
                selectedCardNum += 1;
            }
        }
        if (selectedCardNum < minSelectedCardNum) {
            KLog.W(TAG, "selectedCardNum = " + selectedCardNum + ", not invalid");
            toastView.GetComponent<ToastView>().ShowToast(string.Format("选择的卡牌数量不足，请至少选择{0}张卡牌", minSelectedCardNum.ToString()));
            return;
        }
        if (leaderCardContainer.GetComponent<SingleCardAreaView>().curCard == null) {
            KLog.W(TAG, "not select leader card, not invalid");
            toastView.GetComponent<ToastView>().ShowToast("请选择指挥牌");
            return;
        }
        KConfig.Instance.UpdateDeckInfoIdList(selectedInfoIdList, selectedCardGroup);
        toastView.GetComponent<ToastView>().ShowToast("保存成功");
    }

    public void Exit()
    {
        KLog.I(TAG, "Exit");
        SceneManager.LoadScene("MainMenuScene");
    }

    public void CardGroupChange()
    {
        int value = cardGroupSelect.GetComponent<TMP_Dropdown>().value;
        KLog.I(TAG, "CardGroupChange: value = " + value);
        selectedCardGroup = (CardGroup)value;
        cardGroupAbilityText.GetComponent<TextMeshProUGUI>().text = CardText.cardGroupAbilityText[value];
    }

    private void Init()
    {
        selectedInfoIdList = KConfig.Instance.deckInfoIdList;
        selectedCardGroup = KConfig.Instance.deckCardGroup;
        CardGenerator cardGenerator = new CardGenerator();
        List<CardModel> allCardModelList = cardGenerator.GetAllCardList();
        List<GameObject> backupList = new List<GameObject>();
        List<GameObject> selectedList = new List<GameObject>();
        foreach (CardModel cardModel in allCardModelList) {
            GameObject card = GameObject.Instantiate(cardPrefeb, null);
            card.GetComponent<CardDisplay>().SetCardModel(cardModel);
            card.GetComponent<CardDisplay>().SendSceneMsgCallback += HandleMessage;
            if (selectedInfoIdList.Contains(cardModel.cardInfo.infoId)) {
                if (cardModel.cardInfo.cardType == CardType.Leader) {
                    leaderCardContainer.GetComponent<SingleCardAreaView>().AddCard(card);
                } else {
                    selectedList.Add(card);
                }
            } else {
                backupList.Add(card);
            }
        }
        selectedArea.GetComponent<DeckCardAreaView>().AddCardList(selectedList);
        backupArea.GetComponent<DeckCardAreaView>().AddCardList(backupList);
        UpdateInfoText();
        cardGroupSelect.GetComponent<TMP_Dropdown>().value = (int)selectedCardGroup;
        cardGroupAbilityText.GetComponent<TextMeshProUGUI>().text = CardText.cardGroupAbilityText[(int)selectedCardGroup];
    }

    private void SelectCard(CardModel cardModel)
    {
        GameObject card = FindCard(cardModel);
        if (selectedInfoIdList.Contains(cardModel.cardInfo.infoId)) {
            selectedInfoIdList.Remove(cardModel.cardInfo.infoId);
            if (cardModel.cardInfo.cardType == CardType.Leader) {
                leaderCardContainer.GetComponent<SingleCardAreaView>().RemoveCard();
            } else {
                selectedArea.GetComponent<DeckCardAreaView>().RemoveCard(card);
            }
            backupArea.GetComponent<DeckCardAreaView>().AddCard(card);
        } else {
            backupArea.GetComponent<DeckCardAreaView>().RemoveCard(card);
            selectedInfoIdList.Add(cardModel.cardInfo.infoId);
            if (cardModel.cardInfo.cardType == CardType.Leader) {
                GameObject curLeaderCard = leaderCardContainer.GetComponent<SingleCardAreaView>().curCard;
                if (curLeaderCard != null) {
                    selectedInfoIdList.Remove(curLeaderCard.GetComponent<CardDisplay>().cardModel.cardInfo.infoId);
                    leaderCardContainer.GetComponent<SingleCardAreaView>().RemoveCard();
                    backupArea.GetComponent<DeckCardAreaView>().AddCard(curLeaderCard);
                }
                leaderCardContainer.GetComponent<SingleCardAreaView>().AddCard(card);
            } else {
                selectedArea.GetComponent<DeckCardAreaView>().AddCard(card);
            }
        }
        UpdateInfoText();
    }

    private GameObject FindCard(CardModel cardModel)
    {
        foreach (GameObject card in selectedArea.GetComponent<DeckCardAreaView>().cardList) {
            if (card.GetComponent<CardDisplay>().cardModel.cardInfo.infoId == cardModel.cardInfo.infoId) {
                return card;
            }
        }
        foreach (GameObject card in backupArea.GetComponent<DeckCardAreaView>().cardList) {
            if (card.GetComponent<CardDisplay>().cardModel.cardInfo.infoId == cardModel.cardInfo.infoId) {
                return card;
            }
        }
        if (leaderCardContainer.GetComponent<SingleCardAreaView>().curCard.GetComponent<CardDisplay>().cardModel.cardInfo.infoId == cardModel.cardInfo.infoId) {
            return leaderCardContainer.GetComponent<SingleCardAreaView>().curCard;
        }
        KLog.E(TAG, "card: " + cardModel.cardInfo.chineseName + " not found");
        return null;
    }

    private void UpdateInfoText()
    {
        int selectedCardNum = 0;
        int selectedRoleCardNum = 0;
        int selectedHeroCardNum = 0;
        int selectedUtilCardNum = 0;
        foreach (GameObject card in selectedArea.GetComponent<DeckCardAreaView>().cardList) {
            if (card.GetComponent<CardDisplay>().cardModel.cardInfo.cardType == CardType.Normal) {
                selectedCardNum += 1;
                selectedRoleCardNum += 1;
            } else if (card.GetComponent<CardDisplay>().cardModel.cardInfo.cardType == CardType.Hero) {
                selectedCardNum += 1;
                selectedRoleCardNum += 1;
                selectedHeroCardNum += 1;
            } else if (card.GetComponent<CardDisplay>().cardModel.cardInfo.cardType == CardType.Util) {
                selectedCardNum += 1;
                selectedUtilCardNum += 1;
            } else if (card.GetComponent<CardDisplay>().cardModel.cardInfo.cardType == CardType.Leader) {
            }
        }
        string selectedCardNumTextColor = selectedCardNum >= minSelectedCardNum ? "white" : "red";
        selectedCardNumText.GetComponent<TextMeshProUGUI>().text = string.Format("已选卡牌总数：<color={0}>{1}</color>/{2}", selectedCardNumTextColor, selectedCardNum.ToString(), minSelectedCardNum.ToString());
        selectedRoleCardNumText.GetComponent<TextMeshProUGUI>().text = string.Format("角色牌数：{0}", selectedRoleCardNum.ToString());
        selectedHeroCardNumText.GetComponent<TextMeshProUGUI>().text = string.Format("英雄牌数：{0}", selectedHeroCardNum.ToString());
        selectedUtilCardNumText.GetComponent<TextMeshProUGUI>().text = string.Format("工具牌数：{0}", selectedUtilCardNum.ToString()); // TODO: 工具牌不能超过十张
    }
}
