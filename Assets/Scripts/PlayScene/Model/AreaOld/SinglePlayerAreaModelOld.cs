using System;
using System.Collections.Generic;

// 单方对战区逻辑
public class SinglePlayerAreaModelOld // TODO: 考虑拆分
{
    private static string TAG = "SinglePlayerAreaModelOld";

    public static readonly int initHandCardNum = 10; // 初始手牌数量

    public List<CardModelOld> backupCardList { get; private set; }

    public HandRowAreaModel handRowAreaModel {  get; private set; }

    public InitHandRowAreaModel initHandRowAreaModel { get; private set; }

    public DiscardAreaModelOld discardAreaModel { get; private set; }

    public BattleRowAreaModelOld woodRowAreaModel { get; private set; }

    public BattleRowAreaModelOld brassRowAreaModel { get; private set; }

    public BattleRowAreaModelOld percussionRowAreaModel { get; private set; }

    // 指挥牌
    public SingleCardRowAreaModel leaderCardAreaModel { get; private set; }

    private CardGeneratorOld cardGenerator;

    public SinglePlayerAreaModelOld(bool isHost = true)
    {
        backupCardList = new List<CardModelOld>();
        handRowAreaModel = new HandRowAreaModel();
        initHandRowAreaModel = new InitHandRowAreaModel();
        discardAreaModel = new DiscardAreaModelOld();
        woodRowAreaModel = new BattleRowAreaModelOld(CardBadgeType.Wood);
        brassRowAreaModel = new BattleRowAreaModelOld(CardBadgeType.Brass);
        percussionRowAreaModel = new BattleRowAreaModelOld(CardBadgeType.Percussion);
        leaderCardAreaModel = new SingleCardRowAreaModel();
        cardGenerator = new CardGeneratorOld(isHost); // TODO: 应有个统一管理处，设置是server还是client
    }

    // 初始时调用，设置所有备选卡牌信息
    // enemy设置时idList由对端决定，因此不为null
    public void SetBackupCardInfoIdList(List<int> infoIdList, List<int> idList = null)
    {
        for (int i = 0; i < infoIdList.Count; i++) {
            // 所有备选卡牌生成CardModelOld并存储
            CardModelOld card = null;
            if (idList != null) {
                card = cardGenerator.GetCard(infoIdList[i], idList[i]);
            } else {
                card = cardGenerator.GetCard(infoIdList[i]);
            }
            if (card.cardInfo.cardType == CardType.Leader) {
                card.cardLocation = CardLocation.LeaderCardArea;
                leaderCardAreaModel.AddCard(card);
            } else {
                backupCardList.Add(card);
            }
        }
    }

    // self抽取初始手牌时调用
    public void DrawInitHandCard()
    {
        List<CardModelOld> newCardList = DrawRandomHandCardList(initHandCardNum);
        initHandRowAreaModel.AddCardList(newCardList);
    }

    // reDrawCardList的牌放回备选卡牌区，重新抽取相同数量的手牌
    public void ReDrawInitHandCard()
    {
        foreach (CardModelOld reDrawCard in initHandRowAreaModel.selectedCardList) {
            KLog.I(TAG, "ReDrawInitHandCard: " + reDrawCard.cardInfo.chineseName);
            initHandRowAreaModel.RemoveCard(reDrawCard);
            backupCardList.Add(reDrawCard);
        }
        List<CardModelOld> handCardList = new List<CardModelOld>();
        foreach (CardModelOld card in initHandRowAreaModel.cardList) {
            handCardList.Add(card);
        }
        foreach (CardModelOld handCard in handCardList) {
            initHandRowAreaModel.RemoveCard(handCard);
            handRowAreaModel.AddCard(handCard);
        }
        DrawHandCards(initHandRowAreaModel.selectedCardList.Count);
    }

    public void DrawHandCards(int num)
    {
        List<CardModelOld> newCardList = DrawRandomHandCardList(num);
        handRowAreaModel.AddCardList(newCardList);
    }

    // enemy设置时，随机抽取的操作在对端进行，因此直接指定idList
    public void DrawHandCards(List<int> idList)
    {
        List<CardModelOld> newCardList = new List<CardModelOld>();
        foreach (int id in idList) {
            CardModelOld card = backupCardList.Find(o => { return o.cardInfo.id == id; });
            if (card == null) {
                KLog.E(TAG, "DrawHandCards: invalid id: " + id);
                return;
            }
            newCardList.Add(card);
            backupCardList.Remove(card);
        }
        handRowAreaModel.AddCardList(newCardList);
    }

    public void AddBattleAreaCard(CardModelOld card)
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
            case CardAbility.HornBrass: {
                brassRowAreaModel.AddCard(card);
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
    public int CountBattleAreaCard(Func<CardModelOld, bool> judge)
    {
        int count = 0;
        foreach (CardModelOld card in woodRowAreaModel.cardList) {
            if (judge(card)) {
                count += 1;
            }
        }
        foreach (CardModelOld card in brassRowAreaModel.cardList) {
            if (judge(card)) {
                count += 1;
            }
        }
        foreach (CardModelOld card in percussionRowAreaModel.cardList) {
            if (judge(card)) {
                count += 1;
            }
        }
        return count;
    }

    // 对符合条件的对战区卡牌进行操作
    public void ApplyBattleAreaAction(Func<CardModelOld, bool> judge, Action<CardModelOld> action)
    {
        foreach (CardModelOld card in woodRowAreaModel.cardList) {
            if (judge(card)) {
                action(card);
            }
        }
        foreach (CardModelOld card in brassRowAreaModel.cardList) {
            if (judge(card)) {
                action(card);
            }
        }
        foreach (CardModelOld card in percussionRowAreaModel.cardList) {
            if (judge(card)) {
                action(card);
            }
        }
    }

    // 从手牌区、弃牌区、对战区尝试找到id对应的卡牌
    public CardModelOld FindCard(int id)
    {
        CardModelOld card = handRowAreaModel.FindCard(id);
        if (card != null) {
            return card;
        }
        card = woodRowAreaModel.FindCard(id);
        if (card != null) {
            return card;
        }
        card = brassRowAreaModel.FindCard(id);
        if (card != null) {
            return card;
        }
        card = percussionRowAreaModel.FindCard(id);
        if (card != null) {
            return card;
        }
        card = discardAreaModel.cardList.Find(o => { return o.cardInfo.id == id; });
        if (card != null) {
            return card;
        }
        card = leaderCardAreaModel.cardList.Find(o => { return o.cardInfo.id == id; });
        return card;
    }


    // 小局结束，将对战区的牌移至弃牌区
    public void RemoveAllBattleCard()
    {
        List<CardModelOld> allBattleCardList = new List<CardModelOld>();
        allBattleCardList.AddRange(woodRowAreaModel.cardList);
        allBattleCardList.AddRange(brassRowAreaModel.cardList);
        allBattleCardList.AddRange(percussionRowAreaModel.cardList);
        woodRowAreaModel.RemoveAllCard();
        brassRowAreaModel.RemoveAllCard();
        percussionRowAreaModel.RemoveAllCard();
        foreach (CardModelOld card in allBattleCardList) {
            if (card.cardInfo.cardType == CardType.Util) {
                continue; // 工具牌不进入弃牌区
            }
            discardAreaModel.AddCard(card);
        }
    }

    // 从备选卡牌中随机抽取一些牌
    private List<CardModelOld> DrawRandomHandCardList(int num)
    {
        List<CardModelOld> newCardList = new List<CardModelOld>();
        for (int i = 0; i < num; i++) {
            if (backupCardList.Count <= 0) {
                KLog.W(TAG, "GetCards: backupCardList not enough, missing " + (num - newCardList.Count).ToString());
                break;
            }
            System.Random ran = new System.Random();
            CardModelOld newCard = backupCardList[ran.Next(0, backupCardList.Count)];
            newCardList.Add(newCard);
            backupCardList.Remove(newCard);
        }
        return newCardList;
    }

    // 应用Tunning技能
    private void ApplyTunning()
    {
        ApplyBattleAreaAction((CardModelOld card) => {
            return true;
        }, (CardModelOld card) => {
            card.RemoveNormalDebuff();
        });
    }

    // 更新bond技能
    private void UpdateBond(string bondType)
    {
        // 统计场上同类型的bond牌数量
        int count = CountBattleAreaCard((CardModelOld card) => {
            return card.cardInfo.bondType == bondType;
        });
        // 更新bond数据
        ApplyBattleAreaAction((CardModelOld card) => {
            return card.cardInfo.bondType == bondType;
        }, (CardModelOld card) => {
            card.SetBuff(CardBuffType.Bond, count - 1);
        });
    }

    // 应用抱团技能
    private void ApplyMuster(string musterType)
    {
        // 从手牌中获取
        List<CardModelOld> handTargetCardList = new List<CardModelOld>();
        foreach (CardModelOld card in handRowAreaModel.cardList) {
            if (card.cardInfo.musterType == musterType) {
                handTargetCardList.Add(card);
            }
        }
        foreach (CardModelOld card in handTargetCardList) {
            handRowAreaModel.RemoveCard(card);
        }
        // 从备选卡牌中获取
        List<CardModelOld> backupTargetCardList = new List<CardModelOld>();
        foreach (CardModelOld card in backupCardList) {
            if (card.cardInfo.musterType == musterType) {
                backupTargetCardList.Add(card);
            }
        }
        backupCardList.RemoveAll(o => { return o.cardInfo.musterType == musterType; });
        // 添加卡牌。未避免递归调用造成错误，应先把要打出的卡牌选出来再一起打出
        foreach (CardModelOld card in handTargetCardList) {
            AddBattleAreaCard(card);
        }
        foreach (CardModelOld card in backupTargetCardList) {
            AddBattleAreaCard(card);
        }
    }
}
