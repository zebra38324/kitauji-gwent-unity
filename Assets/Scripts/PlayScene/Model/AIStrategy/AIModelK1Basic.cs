using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

// 久一年牌组AI逻辑，kumiko 1
class AIModelK1Basic : AIModelInterface
{
    // 初始化并设置牌组
    public AIModelK1Basic(PlaySceneModel playSceneModelParam) : base(playSceneModelParam)
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
        // 如果退部的收益够高，就打退部
        if (TryPlayScorch(10)) {
            return;
        }
        // 如果指挥牌的收益够高，就打指挥牌
        if (TryPlayLeader()) {
            return;
        }
        // 尝试打出间谍
        if (TrySpy()) {
            return;
        }
        // 如果场上已经有bond，那就尽量配对下
        if (TryPlayBond()) {
            return;
        }
        // 尝试打出伞霙，有霙就出伞
        if (TryNozomiMizore()) {
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
        // 没牌打了，再试试指挥牌
        if (TryPlayLeader(1)) {
            return;
        }
        // 都不行就pass吧
        playSceneModel.Pass();
    }

    private void SetDeckInfoIdList()
    {
        // 这套牌组的思路：主打木管、打击乐。点数主要靠bond。
        // 包含的技能：bond、tunning、morale、spy(2)、退部、铜管天气、伞霙
        List<int> infoIdList = new List<int>();
        // wood
        infoIdList.Add(1003);
        infoIdList.Add(1004);
        infoIdList.Add(1005);
        infoIdList.Add(1006);
        infoIdList.Add(1007);
        infoIdList.Add(1008);
        infoIdList.Add(1013);
        // brass
        infoIdList.Add(1021);
        infoIdList.Add(1022);
        infoIdList.Add(1024);
        // percussion
        infoIdList.Add(1019);
        infoIdList.Add(1020);
        infoIdList.Add(1029);
        infoIdList.Add(1030);
        infoIdList.Add(1041);
        // util
        infoIdList.Add(5002);
        infoIdList.Add(5004);
        // leader
        infoIdList.Add(5011);
        playSceneModel.SetBackupCardInfoIdList(infoIdList);
    }

    // 选择要重抽的手牌
    private void ChooseReDrawInitHandCard()
    {
        List<CardModel> cardList = playSceneModel.selfSinglePlayerAreaModel.initHandRowAreaModel.cardList;
        Action<Func<CardModel, bool>> SelectCard = (Func<CardModel, bool> judge) => {
            foreach (CardModel card in cardList) {
                if (judge(card) && !card.isSelected) {
                    playSceneModel.ChooseCard(card);
                    return;
                }
            }
        };
        // 间谍牌共有两张，不满的话就重抽
        int spyCount = 0;
        foreach (CardModel card in cardList) {
            if (card.cardInfo.ability == CardAbility.Spy) {
                spyCount += 1;
            }
        }
        if (spyCount >= 2) {
            return;
        }
        // 按优先级寻找暂时最没用的牌
        // bond没配对的牌
        // 点数最低的牌
        int needSelect = 2;
        Dictionary<string, CardModel> bondCardRecord = new Dictionary<string, CardModel>();
        foreach (CardModel card in cardList) {
            if (card.cardInfo.ability == CardAbility.Bond) {
                if (bondCardRecord.ContainsKey(card.cardInfo.bondType)) {
                    bondCardRecord.Remove(card.cardInfo.bondType);
                } else {
                    bondCardRecord.Add(card.cardInfo.bondType, card);
                }
            }
        }
        // 选择没配对的最小点数牌
        while (needSelect > 0 && bondCardRecord.Count > 0) {
            var minPowerCard = bondCardRecord.Aggregate((x, y) => x.Value.cardInfo.originPower < y.Value.cardInfo.originPower ? x : y);
            SelectCard((CardModel card) => {
                return card.cardInfo.id == minPowerCard.Value.cardInfo.id;
            });
            needSelect -= 1;
            bondCardRecord.Remove(minPowerCard.Key);
        }
        // 选择没配对的最小点数牌
        while (needSelect > 0) {
            var minPowerCard = cardList.Aggregate((x, y) => x.cardInfo.originPower < y.cardInfo.originPower ? x : y);
            SelectCard((CardModel card) => {
                return card.cardInfo.id == minPowerCard.cardInfo.id;
            });
            needSelect -= 1;
        }
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
    private bool TryPlayLeader(int minReturn = 10)
    {
        // 铜管天气牌
        // 计算添加buff后的分数差值
        Func<BattleRowAreaModel, int> CalcPowerDiff = (battleRowAreaModel) => {
            int diff = 0;
            foreach (CardModel card in battleRowAreaModel.cardList) {
                int curPower = card.currentPower;
                CardModel tempCard = new CardModel(card);
                tempCard.SetBuff(CardBuffType.Weather, 1);
                int newPower = tempCard.currentPower;
                diff += newPower - curPower;
            }
            return diff;
        };
        int brassDiff = CalcPowerDiff(playSceneModel.selfSinglePlayerAreaModel.brassRowAreaModel) - CalcPowerDiff(playSceneModel.enemySinglePlayerAreaModel.brassRowAreaModel);
        if (brassDiff >= minReturn) {
            playSceneModel.ChooseCard(playSceneModel.selfSinglePlayerAreaModel.leaderCardAreaModel.cardList[0]);
            return true;
        }
        return false;
    }

    // 尝试打出间谍牌。成功返回true，没打就返回false
    private bool TrySpy()
    {
        List<CardModel> handCardList = playSceneModel.selfSinglePlayerAreaModel.handRowAreaModel.cardList;
        int index = handCardList.FindIndex(card => card.cardInfo.ability == CardAbility.Spy);
        if (index < 0) {
            return false;
        }
        playSceneModel.ChooseCard(handCardList[index]);
        return true;
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

    // 尝试打出配对伞霙，有霙就出伞。成功返回true，没打就返回false
    private bool TryNozomiMizore()
    {
        bool hasMizore = false;
        playSceneModel.selfSinglePlayerAreaModel.CountBattleAreaCard((CardModel targetCard) => {
            if (targetCard.cardInfo.chineseName == "铠冢霙") {
                hasMizore = true;
            }
            return true;
        });
        if (!hasMizore) {
            return false;
        }

        List<CardModel> handCardList = playSceneModel.selfSinglePlayerAreaModel.handRowAreaModel.cardList;
        int index = handCardList.FindIndex(card => card.cardInfo.chineseName == "伞木希美");
        if (index < 0) {
            return false;
        }
        playSceneModel.ChooseCard(handCardList[index]);
        return true;
    }
}
