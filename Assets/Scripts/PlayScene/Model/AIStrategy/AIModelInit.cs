using System;
using System.Collections.Generic;
using System.Linq;

// AI逻辑模块初始化策略，决定重抽手牌
public class AIModelInit
{
    private static string TAG = "AIModelInit";

    private PlaySceneModel playSceneModel;

    // 预期收益，有些牌是组合统计的
    private class CombineCard {
        public List<CardModel> cardList;
        public int avgExpectPower;
        public CombineCard(List<CardModel> cardList_, int avg)
        {
            cardList = cardList_;
            avgExpectPower = avg;
        }
    };

    // 初始化并设置牌组
    public AIModelInit(PlaySceneModel playSceneModel_)
    {
        playSceneModel = playSceneModel_;
    }

    public void ChooseReDrawInitHandCard()
    {
        // 遍历所有选择情况，共 1 + 10 + 45 = 56 种选择
        // 每种选择情况，遍历从backup中随机抽取的组合，计算每种选择情况的期望
        // 将每种选择情况排序，按照一个概率分布进行选择
        KLog.I(TAG, $"ChooseReDrawInitHandCard: origin hand card: {string.Join(", ", playSceneModel.wholeAreaModel.selfSinglePlayerAreaModel.handCardAreaModel.initHandCardListModel.cardList.Select(x => $"{x.cardInfo.chineseName}-{x.cardInfo.ability}"))}");
        var allActionList = new List<List<int>>();
        var expectationDict = new Dictionary<List<int>, int>();
        allActionList.Add(new List<int>()); // 0个选择的情况
        for (int i = 0; i < HandCardAreaModel.INIT_HAND_CARD_NUM; i++) {
            allActionList.Add(new List<int> { i }); // 1个选择的情况
            for (int j = i + 1; j < HandCardAreaModel.INIT_HAND_CARD_NUM; j++) {
                allActionList.Add(new List<int> { i, j }); // 2个选择的情况
            }
        }
        foreach (var action in allActionList) {
            int expectation = CalculateExpectation(action);
            expectationDict[action] = expectation;
        }
        allActionList.Sort((x, y) => expectationDict[y].CompareTo(expectationDict[x])); // 按照期望值降序排序
        int index = AIBase.GetIndexExponentialProbabilities(10); // 简化处理，只考虑前十个
        KLog.I(TAG, $"ChooseReDrawInitHandCard: selected index: {index}, expectation: {expectationDict[allActionList[index]]}");
        foreach (var i in allActionList[index]) {
            playSceneModel.ChooseCard(playSceneModel.wholeAreaModel.selfSinglePlayerAreaModel.handCardAreaModel.initHandCardListModel.cardList[i]);
        }
    }

    // 选择selectedIndexList中的index，长度可能为0，1，2  
    // 计算这种选择情况下的手牌收益期望  
    private int CalculateExpectation(List<int> selectedIndexList)
    {
        var originHandList = playSceneModel.wholeAreaModel.selfSinglePlayerAreaModel.handCardAreaModel.initHandCardListModel.cardList;
        var originBackupList = playSceneModel.wholeAreaModel.selfSinglePlayerAreaModel.handCardAreaModel.backupCardList.ToList();
        var baseHandList = originHandList.Where((item, index) => !selectedIndexList.Contains(index)).ToList();
        var baseBackupList = new List<CardModel>(originBackupList);
        foreach (var index in selectedIndexList) {
            baseBackupList.Add(originHandList[index]);
        }
        var allHandList = new List<List<CardModel>>();
        var allBackupList = new List<List<CardModel>>();
        if (selectedIndexList.Count == 0) {
            allHandList.Add(baseHandList);
            allBackupList.Add(baseBackupList);
            //KLog.I(TAG, "CalculateExpectation: no select");
        } else if (selectedIndexList.Count == 1) {
            //KLog.I(TAG, $"CalculateExpectation: select = {originHandList[selectedIndexList[0]].cardInfo.chineseName}");
            foreach (var card in baseBackupList) {
                var curHandList = new List<CardModel>(baseHandList);
                curHandList.Add(card);
                allHandList.Add(curHandList);
                var curBackupList = new List<CardModel>(baseBackupList);
                curBackupList.Remove(card);
                allBackupList.Add(curBackupList);
            }
        } else if (selectedIndexList.Count == 2) {
            //KLog.I(TAG, $"CalculateExpectation: select = {originHandList[selectedIndexList[0]].cardInfo.chineseName} {originHandList[selectedIndexList[1]].cardInfo.chineseName}");
            for (int i = 0; i < baseBackupList.Count; i++) {
                for (int j = i + 1; j < baseBackupList.Count; j++) {
                    var curHandList = new List<CardModel>(baseHandList);
                    curHandList.Add(baseBackupList[i]);
                    curHandList.Add(baseBackupList[j]);
                    allHandList.Add(curHandList);
                    var curBackupList = new List<CardModel>(baseBackupList);
                    curBackupList.Remove(baseBackupList[i]);
                    curBackupList.Remove(baseBackupList[j]);
                    allBackupList.Add(curBackupList);
                }
            }
        } else {
            throw new ArgumentException("CalculateExpectation: selectedIndexList length should be 0, 1 or 2");
        }

        int sum = 0;
        for (int i = 0; i < allHandList.Count; i++) {
            int benifit = GetHandCardBenifit(allHandList[i], allBackupList[i]);
            sum += benifit;
        }
        int expectation = sum / allHandList.Count;
        //KLog.I(TAG, $"CalculateExpectation: expectation = {expectation}");
        return expectation;
    }

    /**
     * 手牌组预期收益
     * 由于不少卡牌的收益与其他卡片相关联，很难对单独卡牌准确计算，因此对于手牌组进行收益的整体计算
     * TODO: 这些随机的收益上下限，后续可以更科学一些
     * 
     * 这里的代码或许是个重要的节点，希望能保留这种兴奋感
     */
    private int GetHandCardBenifit(List<CardModel> handCardList, List<CardModel> backupCardList)
    {
        int sum = 0;
        foreach (var card in handCardList) {
            Random random = new Random();
            int baseBenifit = card.cardInfo.originPower;
            int extra = card.cardInfo.cardType == CardType.Hero ? random.Next(3, 10) : 0; // 英雄牌在计算收益时，随机添加一些
            switch (card.cardInfo.ability) {
                case CardAbility.Spy: {
                    // spy优先级最高
                    baseBenifit = -baseBenifit;
                    extra = 500;
                    break;
                }
                case CardAbility.Attack: {
                    extra = card.cardInfo.attackNum;
                    break;
                }
                case CardAbility.Tunning: {
                    extra = random.Next(0, 5);
                    break;
                }
                case CardAbility.Bond: {
                    string bondType = card.cardInfo.bondType;
                    int count = handCardList.Count(x => x.cardInfo.bondType == bondType);
                    extra = baseBenifit * (count - 1);
                    break;
                }
                case CardAbility.ScorchWood: {
                    extra = random.Next(5, 20);
                    break;
                }
                case CardAbility.Muster: {
                    string musterType = card.cardInfo.musterType;
                    int handCount = handCardList.Count(x => x.cardInfo.musterType == musterType);
                    extra = -(handCount - 1) * 100; // 相当于少手牌，收益大幅降低
                    extra += (handCardList.Sum(x => x.cardInfo.musterType == musterType ? x.cardInfo.originPower : 0) +
                        backupCardList.Sum(x => x.cardInfo.musterType == musterType ? x.cardInfo.originPower : 0)) / handCount;
                    break;
                }
                case CardAbility.Morale: {
                    extra = random.Next(3, 6);
                    break;
                }
                case CardAbility.Medic: {
                    extra = random.Next(20, 30);
                    break;
                }
                case CardAbility.Horn: {
                    extra = random.Next(15, 30);
                    break;
                }
                case CardAbility.Decoy: {
                    extra = 100;
                    break;
                }
                case CardAbility.Scorch: {
                    extra = random.Next(10, 30);
                    break;
                }
                case CardAbility.SunFes: {
                    extra = random.Next(10, 30);
                    break;
                }
                case CardAbility.Daisangakushou: {
                    extra = random.Next(10, 25);
                    break;
                }
                case CardAbility.Drumstick: {
                    extra = random.Next(10, 20);
                    break;
                }
                case CardAbility.ClearWeather: {
                    extra = random.Next(0, 20);
                    break;
                }
                case CardAbility.HornUtil:
                case CardAbility.HornBrass: {
                    extra = random.Next(0, 20);
                    break;
                }
                case CardAbility.Lip: {
                    extra = random.Next(0, 4);
                    break;
                }
                case CardAbility.Guard: {
                    bool hasTarget = handCardList.Any(x => x.cardInfo.chineseName == card.cardInfo.relatedCard);
                    extra = hasTarget ? 4 : 0;
                    break;
                }
                case CardAbility.Monaka: {
                    extra = 2;
                    break;
                }
                case CardAbility.Kasa: {
                    bool hasMizore = handCardList.Any(x => x.cardInfo.chineseName == "铠冢霙");
                    if (hasMizore) {
                        extra = 5 + random.Next(0, 4);
                    } else {
                        extra = 0;
                    }
                    break;
                }
                case CardAbility.K5Leader: {
                    int k5Count = handCardList.Count(x => x.cardInfo.grade == 1);
                    float ratio = random.Next(2, 4) / 3;
                    extra = (int)(2 * (k5Count - 1) * ratio);
                    break;
                }
                case CardAbility.SalutdAmour: {
                    bool hasSapphire = handCardList.Any(x => x.cardInfo.chineseName == "川岛绿辉");
                    extra = hasSapphire ? 3 : 0;
                    break;
                }
                case CardAbility.Pressure: {
                    extra = random.Next(-5, 10);
                    break;
                }
                case CardAbility.Defend: {
                    extra = random.Next(5, 20);
                    break;
                }
                case CardAbility.PowerFirst: {
                    extra = random.Next(0, 5);
                    break;
                }
            }
            sum += baseBenifit + extra;
        }
        //KLog.I(TAG, $"GetHandCardBenifit: hand card list: {string.Join(", ", handCardList.Select(x => x.cardInfo.chineseName))}");
        //KLog.I(TAG, $"GetHandCardBenifit: result: {sum}");
        return sum;
    }
}
