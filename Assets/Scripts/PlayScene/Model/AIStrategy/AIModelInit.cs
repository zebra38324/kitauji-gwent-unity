using System;
using System.Collections.Generic;
using System.Linq;

// AI逻辑模块初始化策略，决定重抽手牌
class AIModelInit
{
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
        // 根据牌组决策出最好的情况，然后与实际抽到的进行对比
        var bestList = GetBestHandCard();
        var musterIndex = new Dictionary<string, List<int>>();
        for (int index = 0; index < bestList.Count; index++) {
            var card = bestList[index];
            if (card.cardInfo.ability != CardAbility.Muster) {
                continue;
            }
            if (musterIndex.ContainsKey(card.cardInfo.musterType)) {
                musterIndex[card.cardInfo.musterType].Add(index);
            } else {
                musterIndex.Add(card.cardInfo.musterType, new List<int> { index });
            }
        }

        var GetExpectIndex = new Func<CardModel, int>(card => {
            if (card.cardInfo.ability != CardAbility.Muster) {
                return bestList.FindIndex(o => o.cardInfo.id == card.cardInfo.id);
            } else {
                int index = musterIndex[card.cardInfo.musterType][0];
                musterIndex[card.cardInfo.musterType].RemoveAt(0);
                return index;
            }
        });

        List<KeyValuePair<CardModel, int>> cardList = playSceneModel.wholeAreaModel.selfSinglePlayerAreaModel.handCardAreaModel.initHandCardListModel.cardList
            .Select(o => new KeyValuePair<CardModel, int>(o, GetExpectIndex(o))).ToList();
        cardList.Sort((x, y) => y.Value.CompareTo(x.Value));
        // 越往前的收益越低
        int handCardNum = 10;
        int canSelect = 2;
        foreach (var pair in cardList) {
            var card = pair.Key;
            if (canSelect == 0 || pair.Value <= handCardNum) {
                break;
            }
            var select = playSceneModel.wholeAreaModel.selfSinglePlayerAreaModel.handCardAreaModel.initHandCardListModel.cardList.Find(o => o.cardInfo.id == card.cardInfo.id);
            playSceneModel.ChooseCard(select);
            canSelect -= 1;
        }
    }

    private List<CardModel> GetBestHandCard()
    {
        List<CardModel> bestHandCardList = new List<CardModel>();
        List<CardModel> allCardList = playSceneModel.wholeAreaModel.selfSinglePlayerAreaModel.handCardAreaModel.initHandCardListModel.cardList.ToList();
        allCardList.AddRange(playSceneModel.wholeAreaModel.selfSinglePlayerAreaModel.handCardAreaModel.backupCardList);
        var expectPowerList = GetExpectPowerList(allCardList); // 预期收益，有些牌是组合统计的
        var backList = new List<CardModel>();

        foreach (var combineCard in expectPowerList) {
            if (combineCard.cardList[0].cardInfo.ability == CardAbility.Muster) {
                var cardList = new List<CardModel>(combineCard.cardList);
                bestHandCardList.Add(cardList[0]);
                cardList.RemoveAt(0);
                backList.AddRange(cardList);
            } else {
                var cardList = new List<CardModel>(combineCard.cardList);
                cardList.Sort((x, y) => y.cardInfo.originPower.CompareTo(x.cardInfo.originPower));
                bestHandCardList.AddRange(cardList);
            }
        }
        bestHandCardList.AddRange(backList);
        return bestHandCardList;
    }

    // 获取预期收益列表，降序排序
    // TODO: 这些随机的收益上下限，后续可以更科学一些
    private List<CombineCard> GetExpectPowerList(List<CardModel> allCardList)
    {
        var expectPowerList = new List<CombineCard>();
        Random random = new Random();
        foreach (CardModel card in allCardList) {
            int heroExtra = random.Next(3, 10); // 英雄牌在计算收益时，随机添加一些
            int cardPower = card.cardInfo.cardType == CardType.Hero ? card.cardInfo.originPower + heroExtra : card.cardInfo.originPower;
            switch (card.cardInfo.ability) {
                case CardAbility.None: {
                    if (card.cardInfo.chineseName == "铠冢霙" && card.cardInfo.cardType == CardType.Normal) {
                        var kasa = expectPowerList.Find(o => o.cardList[0].cardInfo.ability == CardAbility.Kasa);
                        if (kasa == null) {
                            expectPowerList.Add(new CombineCard(new List<CardModel> { card }, cardPower));
                        } else {
                            int extra = random.Next(0, 4);
                            kasa.avgExpectPower = (kasa.cardList[0].cardInfo.originPower + card.cardInfo.originPower + extra + 5) / 2;
                            kasa.cardList.Add(card);
                        }
                    } else {
                        expectPowerList.Add(new CombineCard(new List<CardModel> { card }, cardPower));
                    }
                    break;
                }
                case CardAbility.Spy: {
                    // spy优先级最高
                    expectPowerList.Add(new CombineCard(new List<CardModel> { card }, 500 - cardPower));
                    break;
                }
                case CardAbility.Attack: {
                    expectPowerList.Add(new CombineCard(new List<CardModel> { card }, cardPower + card.cardInfo.attackNum));
                    break;
                }
                case CardAbility.Tunning: {
                    int extra = random.Next(0, 5);
                    expectPowerList.Add(new CombineCard(new List<CardModel> { card }, cardPower + extra));
                    break;
                }
                case CardAbility.Bond: {
                    var bond = expectPowerList.Find(o => o.cardList[0].cardInfo.bondType == card.cardInfo.bondType);
                    if (bond == null) {
                        expectPowerList.Add(new CombineCard(new List<CardModel> { card }, cardPower));
                    } else {
                        bond.cardList.Add(card);
                        bond.avgExpectPower = cardPower * bond.cardList.Count;
                    }
                    break;
                }
                case CardAbility.ScorchWood: {
                    int extra = random.Next(10, 20);
                    expectPowerList.Add(new CombineCard(new List<CardModel> { card }, cardPower + extra));
                    break;
                }
                case CardAbility.Muster: {
                    var muster = expectPowerList.Find(o => o.cardList[0].cardInfo.musterType == card.cardInfo.musterType);
                    if (muster == null) {
                        expectPowerList.Add(new CombineCard(new List<CardModel> { card }, cardPower));
                    } else {
                        muster.cardList.Add(card);
                        int extra = random.Next(-5, 0);
                        muster.avgExpectPower = muster.cardList.Count * cardPower + extra;
                    }
                    break;
                }
                case CardAbility.Morale: {
                    int extra = random.Next(3, 6);
                    expectPowerList.Add(new CombineCard(new List<CardModel> { card }, cardPower + extra));
                    break;
                }
                case CardAbility.Medic: {
                    int extra = random.Next(5, 10);
                    expectPowerList.Add(new CombineCard(new List<CardModel> { card }, cardPower + extra));
                    break;
                }
                case CardAbility.Horn: {
                    int extra = random.Next(10, 20);
                    expectPowerList.Add(new CombineCard(new List<CardModel> { card }, cardPower + extra));
                    break;
                }
                case CardAbility.Decoy: {
                    int extra = random.Next(0, 10);
                    expectPowerList.Add(new CombineCard(new List<CardModel> { card }, cardPower + extra));
                    break;
                }
                case CardAbility.Scorch: {
                    int extra = random.Next(10, 30);
                    expectPowerList.Add(new CombineCard(new List<CardModel> { card }, cardPower + extra));
                    break;
                }
                case CardAbility.SunFes: {
                    int extra = random.Next(10, 30);
                    expectPowerList.Add(new CombineCard(new List<CardModel> { card }, cardPower + extra));
                    break;
                }
                case CardAbility.Daisangakushou: {
                    int extra = random.Next(10, 25);
                    expectPowerList.Add(new CombineCard(new List<CardModel> { card }, cardPower + extra));
                    break;
                }
                case CardAbility.Drumstick: {
                    int extra = random.Next(10, 20);
                    expectPowerList.Add(new CombineCard(new List<CardModel> { card }, cardPower + extra));
                    break;
                }
                case CardAbility.ClearWeather: {
                    int extra = random.Next(0, 20);
                    expectPowerList.Add(new CombineCard(new List<CardModel> { card }, cardPower + extra));
                    break;
                }
                case CardAbility.HornUtil:
                case CardAbility.HornBrass: {
                    int extra = random.Next(10, 20);
                    expectPowerList.Add(new CombineCard(new List<CardModel> { card }, cardPower + extra));
                    break;
                }
                case CardAbility.Lip: {
                    int extra = random.Next(0, 6);
                    expectPowerList.Add(new CombineCard(new List<CardModel> { card }, cardPower + extra));
                    break;
                }
                case CardAbility.Guard: {
                    int extra = random.Next(0, 4);
                    expectPowerList.Add(new CombineCard(new List<CardModel> { card }, cardPower + extra));
                    break;
                }
                case CardAbility.Monaka: {
                    expectPowerList.Add(new CombineCard(new List<CardModel> { card }, cardPower + 2));
                    break;
                }
                case CardAbility.Kasa: {
                    var kasa = expectPowerList.Find(o => o.cardList[0].cardInfo.chineseName == "铠冢霙" && o.cardList[0].cardInfo.cardType == CardType.Normal);
                    if (kasa == null) {
                        expectPowerList.Add(new CombineCard(new List<CardModel> { card }, cardPower));
                    } else {
                        int extra = random.Next(0, 4);
                        kasa.avgExpectPower = (kasa.cardList[0].cardInfo.originPower + card.cardInfo.originPower + extra + 5) / 2;
                        kasa.cardList.Add(card);
                    }
                    break;
                }
            }
        }
        expectPowerList.Sort((x, y) => y.avgExpectPower.CompareTo(x.avgExpectPower));
        return expectPowerList;
    }
}
