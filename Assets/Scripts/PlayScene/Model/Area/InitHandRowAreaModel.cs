﻿using System.Collections.Generic;
/**
 * 初始化时，待重抽的手牌区域逻辑
 */
public class InitHandRowAreaModel: RowAreaModel
{
    private string TAG = "InitHandRowAreaModel";

    public static readonly int RE_DRAW_HAND_CARD_NUM_MAX = 2;

    public List<CardModel> selectedCardList { get; private set; }

    public InitHandRowAreaModel()
    {
        selectedCardList = new List<CardModel>();
    }

    public override void AddCardList(List<CardModel> newCardList)
    {
        foreach (CardModel card in newCardList) {
            card.cardLocation = CardLocation.InitHandArea;
        }
        base.AddCardList(newCardList);
    }

    public override void AddCard(CardModel card)
    {
        card.cardLocation = CardLocation.InitHandArea;
        base.AddCard(card);
    }

    public void SelectCard(CardModel card)
    {
        if (selectedCardList.Contains(card)) {
            selectedCardList.Remove(card);
            card.isSelected = false;
            KLog.I(TAG, "SelectCard: cancel " + card.cardInfo.chineseName);
        } else if (selectedCardList.Count < RE_DRAW_HAND_CARD_NUM_MAX) {
            selectedCardList.Add(card);
            card.isSelected = true;
            KLog.I(TAG, "SelectCard: " + card.cardInfo.chineseName);
        } else {
            KLog.I(TAG, "SelectCard: too many card, can not choose " + card.cardInfo.chineseName);
        }
    }
}
