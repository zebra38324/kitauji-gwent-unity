using System;
using System.Collections.Generic;
using UnityEngine;

// 单方对战区逻辑
public class SinglePlayerAreaModel
{
    private static string TAG = "SinglePlayerAreaModel";

    private static readonly int initHandCardNum = 10; // 初始手牌数量

    public List<CardModel> backupCardList { get; private set; }

    public HandRowAreaModel handRowAreaModel {  get; private set; }

    public DiscardAreaModel discardAreaModel { get; private set; }

    public BattleRowAreaModel woodRowAreaModel { get; private set; }

    public BattleRowAreaModel brassRowAreaModel { get; private set; }

    public BattleRowAreaModel percussionRowAreaModel { get; private set; }

    private CardGenerator cardGenerator;

    private static List<CardInfo> allCardInfoList_;

    private static List<CardInfo> allCardInfoList {
        get {
            if (allCardInfoList_ != null) {
                return allCardInfoList_;
            }
            TextAsset cardInfoAsset = Resources.Load<TextAsset>(@"Statistic\KumikoSecondYear");
            if (cardInfoAsset == null) {
                KLog.E(TAG, "cardInfoAsset is null");
                return allCardInfoList_;
            }
            allCardInfoList_ = StatisticJsonParse.GetCardInfo(cardInfoAsset.text);
            return allCardInfoList_;
        }
        set {

        }
    }

    public SinglePlayerAreaModel()
    {
        backupCardList = new List<CardModel>();
        handRowAreaModel = new HandRowAreaModel();
        discardAreaModel = new DiscardAreaModel();
        woodRowAreaModel = new BattleRowAreaModel(CardBadgeType.Wood);
        brassRowAreaModel = new BattleRowAreaModel(CardBadgeType.Brass);
        percussionRowAreaModel = new BattleRowAreaModel(CardBadgeType.Percussion);
        cardGenerator = new CardGenerator(CardGenerator.serverSalt); // TODO: 应有个统一管理处，设置是server还是client
    }

    // 初始时调用，设置所有备选卡牌信息
    public void SetBackupCardInfoIdList(List<int> infoIdList)
    {
        Func<List<CardInfo>, int, CardInfo> FindCardInfo = (List<CardInfo> cardInfoList, int infoId) => {
            foreach (CardInfo cardInfo in cardInfoList) {
                if (cardInfo.infoId == infoId) {
                    return cardInfo;
                }
            }
            KLog.E(TAG, "infoId: " + infoId + " is invalid");
            return new CardInfo();
        };
        foreach (int infoId in infoIdList) {
            // 所有备选卡牌生成CardModel并存储
            backupCardList.Add(cardGenerator.GetCard(FindCardInfo(allCardInfoList, infoId)));
        }

        // 抽取十张手牌
        DrawHandCards(initHandCardNum);
    }

    public void DrawHandCards(int num)
    {
        List<CardModel> newCardList = new List<CardModel>();
        for (int i = 0; i < num; i++) {
            if (backupCardList.Count <= 0) {
                KLog.W(TAG, "GetCards: backupCardList not enough, missing " + (num - newCardList.Count).ToString());
                break;
            }
            System.Random ran = new System.Random();
            CardModel newCard = backupCardList[ran.Next(0, backupCardList.Count)];
            newCardList.Add(newCard);
            backupCardList.Remove(newCard);
        }
        handRowAreaModel.AddCardList(newCardList);
    }

    public void AddBattleAreaCard(CardModel card)
    {
        // 先打出卡牌，再结算技能
        switch (card.cardInfo.badgeType) {
            case CardBadgeType.Wood: {
                woodRowAreaModel.AddCard(card);
                break;
            }
            case CardBadgeType.Brass: {
                brassRowAreaModel.AddCard(card);
                break;
            }
            case CardBadgeType.Percussion: {
                percussionRowAreaModel.AddCard(card);
                break;
            }
        }

        // 实施卡牌技能
        switch (card.cardInfo.ability) {
            case CardAbility.Tunning: {
                ApplyTunning();
                break;
            }
            case CardAbility.Bond: {
                UpdateBond(card.cardInfo.bondType);
                break;
            }
            case CardAbility.Muster: {
                ApplyMuster(card.cardInfo.musterType);
                break;
            }
            default: {
                break;
            }
        }
    }

    public int GetCurrentPower()
    {
        return woodRowAreaModel.GetCurrentPower() +
            brassRowAreaModel.GetCurrentPower() + 
            percussionRowAreaModel.GetCurrentPower();
    }

    // 统计符合条件的对战区卡牌数量
    public int CountBattleAreaCard(Func<CardModel, bool> judge)
    {
        int count = 0;
        foreach (CardModel card in woodRowAreaModel.cardList) {
            if (judge(card)) {
                count += 1;
            }
        }
        foreach (CardModel card in brassRowAreaModel.cardList) {
            if (judge(card)) {
                count += 1;
            }
        }
        foreach (CardModel card in percussionRowAreaModel.cardList) {
            if (judge(card)) {
                count += 1;
            }
        }
        return count;
    }

    // 对符合条件的对战区卡牌进行操作
    public void ApplyBattleAreaAction(Func<CardModel, bool> judge, Action<CardModel> action)
    {
        foreach (CardModel card in woodRowAreaModel.cardList) {
            if (judge(card)) {
                action(card);
            }
        }
        foreach (CardModel card in brassRowAreaModel.cardList) {
            if (judge(card)) {
                action(card);
            }
        }
        foreach (CardModel card in percussionRowAreaModel.cardList) {
            if (judge(card)) {
                action(card);
            }
        }
    }

    // 应用Tunning技能
    private void ApplyTunning()
    {
        ApplyBattleAreaAction((CardModel card) => {
            return true;
        }, (CardModel card) => {
            card.RemoveNormalDebuff();
        });
    }

    // 更新bond技能
    private void UpdateBond(string bondType)
    {
        // 统计场上同类型的bond牌数量
        int count = CountBattleAreaCard((CardModel card) => {
            return card.cardInfo.bondType == bondType;
        });
        // 更新bond数据
        ApplyBattleAreaAction((CardModel card) => {
            return card.cardInfo.bondType == bondType;
        }, (CardModel card) => {
            card.SetBuff(CardBuffType.Bond, count - 1);
        });
    }

    // 应用抱团技能
    private void ApplyMuster(string musterType)
    {
        // 从手牌中获取
        List<CardModel> handTargetCardList = new List<CardModel>();
        foreach (CardModel card in handRowAreaModel.cardList) {
            if (card.cardInfo.musterType == musterType) {
                handTargetCardList.Add(card);
            }
        }
        foreach (CardModel card in handTargetCardList) {
            handRowAreaModel.RemoveCard(card);
        }
        // 从备选卡牌中获取
        List<CardModel> backupTargetCardList = new List<CardModel>();
        foreach (CardModel card in backupCardList) {
            if (card.cardInfo.musterType == musterType) {
                backupTargetCardList.Add(card);
            }
        }
        backupCardList.RemoveAll(o => { return o.cardInfo.musterType == musterType; });
        // 添加卡牌。未避免递归调用造成错误，应先把要打出的卡牌选出来再一起打出
        foreach (CardModel card in handTargetCardList) {
            AddBattleAreaCard(card);
        }
        foreach (CardModel card in backupTargetCardList) {
            AddBattleAreaCard(card);
        }
    }
}
