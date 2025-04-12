using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

// 久二年牌组AI逻辑，kumiko 2
class AIModelK2Basic : AIModelInterface
{
    // 初始化并设置牌组
    public AIModelK2Basic(PlaySceneModel playSceneModelParam) : base(playSceneModelParam)
    {
        // 设置牌组
        SetDeckInfoIdList();
    }

    // 初始化手牌，并完成重抽手牌操作
    public override void DoInitHandCard()
    {
        playSceneModel.DrawInitHandCard();
        ChooseReDrawInitHandCard();
        playSceneModel.ReDrawInitHandCard();
    }

    // WAIT_SELF_ACTION时的操作
    public override void DoPlayAction()
    {
        // 如果没手牌，指挥牌也没了，就pass
        if (playSceneModel.selfSinglePlayerAreaModel.handRowAreaModel.cardList.Count == 0 && playSceneModel.selfSinglePlayerAreaModel.leaderCardAreaModel.cardList.Count == 0) {
            playSceneModel.Pass();
            return;
        }
        // 如果对面已经pass，且本方分数高，就pass
        if (playSceneModel.tracker.enemyPass && playSceneModel.selfSinglePlayerAreaModel.GetCurrentPower() > playSceneModel.enemySinglePlayerAreaModel.GetCurrentPower()) {
            playSceneModel.Pass();
            return;
        }
        // 先打muster探探路
        if (TryPlayMuster()) {
            return;
        }
        // 如果退部的收益够高，就打退部
        if (TryPlayScorch(10)) {
            return;
        }
        // 如果指挥牌的收益够高，就打指挥牌
        if (TryPlayLeader()) {
            return;
        }
        // 如果场上已经有bond，那就尽量配对下
        if (TryPlayBond()) {
            return;
        }
        // 打个点数最高的手牌
        if (TryPlayMaxRole()) {
            return;
        }
        // 没牌打了，再试试退部
        if (TryPlayScorch(1)) {
            return;
        }
        // 都不行就pass吧
        playSceneModel.Pass();
    }

    private void SetDeckInfoIdList()
    {
        List<int> infoIdList = new List<int>();
        // wood
        infoIdList.Add(2005);
        infoIdList.Add(2006);
        infoIdList.Add(2007);
        infoIdList.Add(2008);
        infoIdList.Add(2011);
        infoIdList.Add(2012);
        infoIdList.Add(2013);
        // brass
        infoIdList.Add(2028);
        infoIdList.Add(2034);
        infoIdList.Add(2035);
        // percussion
        infoIdList.Add(2042);
        infoIdList.Add(2047);
        infoIdList.Add(2048);
        // util
        infoIdList.Add(5002);
        // leader
        infoIdList.Add(2080);
        playSceneModel.SetBackupCardInfoIdList(infoIdList);
    }

    // 选择要重抽的手牌
    private void ChooseReDrawInitHandCard()
    {
        int hasSelectedCount = 0;
        Action<Func<CardModel, bool>> SelectCard = (Func<CardModel, bool> judge) => {
            foreach (CardModel card in playSceneModel.selfSinglePlayerAreaModel.initHandRowAreaModel.cardList) {
                if (judge(card) && !card.isSelected) {
                    playSceneModel.ChooseCard(card);
                    return;
                }
            }
        };
        // 首先看看有没有同一组的Muster，有的话只留一个
        Dictionary<string, int> musterCountDict = new Dictionary<string, int>();
        foreach (CardModel card in playSceneModel.selfSinglePlayerAreaModel.initHandRowAreaModel.cardList) {
            if (card.cardInfo.ability == CardAbility.Muster) {
                if (musterCountDict.ContainsKey(card.cardInfo.musterType)) {
                    musterCountDict[card.cardInfo.musterType] += 1;
                } else {
                    musterCountDict.Add(card.cardInfo.musterType, 1);
                }
            }
        }
        for (int i = 0; i < musterCountDict.Count; i++) {
            string musterType = musterCountDict.Keys.ElementAt(i);
            if (musterCountDict[musterType] == 1) {
                continue;
            }
            while (musterCountDict[musterType] > 1) {
                if (hasSelectedCount >= 2) {
                    return;
                }
                SelectCard((CardModel card) => {
                    return card.cardInfo.musterType == musterType;
                });
                hasSelectedCount += 1;
                musterCountDict[musterType] -= 1;
            }
        }
    }

    // 尝试打出muster牌，成功返回true，没有muster牌返回false
    private bool TryPlayMuster()
    {
        int index = playSceneModel.selfSinglePlayerAreaModel.handRowAreaModel.cardList.FindIndex(card => card.cardInfo.ability == CardAbility.Muster);
        if (index < 0) {
            return false;
        }
        playSceneModel.ChooseCard(playSceneModel.selfSinglePlayerAreaModel.handRowAreaModel.cardList[index]);
        return true;
    }

    // 尝试打出退部牌，如果收益高，就打出。成功返回true，没打就返回false
    private bool TryPlayScorch(int minReturn)
    {
        int index = playSceneModel.selfSinglePlayerAreaModel.handRowAreaModel.cardList.FindIndex(card => card.cardInfo.ability == CardAbility.Scorch);
        if (index < 0) {
            return false;
        }
        if (GetScorchReturn() >= minReturn) {
            // 收益够大，打出
            playSceneModel.ChooseCard(playSceneModel.selfSinglePlayerAreaModel.handRowAreaModel.cardList[index]);
            return true;
        }
        return false;
    }

    // 尝试打出指挥牌，如果收益高，就打出。成功返回true，没打就返回false
    private bool TryPlayLeader()
    {
        if (playSceneModel.selfSinglePlayerAreaModel.leaderCardAreaModel.cardList.Count == 0) {
            return false;
        }
        // 只针对久美子指挥牌
        int brassNormalPower = 0;
        foreach (CardModel card in playSceneModel.selfSinglePlayerAreaModel.brassRowAreaModel.cardList) {
            if (card.cardInfo.cardType == CardType.Normal) {
                brassNormalPower += card.currentPower;
            }
        }
        if (brassNormalPower >= 5) {
            playSceneModel.ChooseCard(playSceneModel.selfSinglePlayerAreaModel.leaderCardAreaModel.cardList[0]);
            return true;
        }
        return false;
    }

    // 尝试打出bond牌配对。成功返回true，没打就返回false
    private bool TryPlayBond()
    {
        List<string> battleBondTypeList = new List<string>();
        playSceneModel.selfSinglePlayerAreaModel.CountBattleAreaCard((CardModel targetCard) => {
            if (targetCard.cardInfo.ability == CardAbility.Bond) {
                battleBondTypeList.Add(targetCard.cardInfo.bondType);
            }
            return true;
        });
        foreach (CardModel card in playSceneModel.selfSinglePlayerAreaModel.handRowAreaModel.cardList) {
            if (card.cardInfo.ability == CardAbility.Bond && battleBondTypeList.Contains(card.cardInfo.bondType)) {
                playSceneModel.ChooseCard(card);
                return true;
            }
        }
        return false;
    }
}
