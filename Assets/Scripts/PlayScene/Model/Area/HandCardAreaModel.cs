using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.Immutable;
using LanguageExt;

/**
 * 手牌管理的逻辑，包括手牌与初始阶段的重抽手牌，指挥牌也是广义上的手牌
 */
public record HandCardAreaModel
{
    private static string TAG = "HandCardAreaModel";

    public static readonly int INIT_HAND_CARD_NUM = 10; // 初始手牌数量

    public ImmutableList<CardModel> backupCardList { get; init; } = ImmutableList<CardModel>.Empty;

    public HandCardListModel handCardListModel { get; init; } = new HandCardListModel();

    public InitHandCardListModel initHandCardListModel { get; init; } = new InitHandCardListModel();

    public bool hasReDrawInitHandCard { get; init; } = false;

    public static readonly Lens<HandCardAreaModel, HandCardListModel> Lens_HandCardListModel = Lens<HandCardAreaModel, HandCardListModel>.New(
        h => h.handCardListModel,
        handCardListModel => h => h with { handCardListModel = handCardListModel }
    );

    public static readonly Lens<HandCardAreaModel, InitHandCardListModel> Lens_InitHandCardListModel = Lens<HandCardAreaModel, InitHandCardListModel>.New(
        h => h.initHandCardListModel,
        initHandCardListModel => h => h with { initHandCardListModel = initHandCardListModel }
    );

    public SingleCardListModel leaderCardListModel { get; init; }

    public static readonly Lens<HandCardAreaModel, SingleCardListModel> Lens_LeaderCardListModel = Lens<HandCardAreaModel, SingleCardListModel>.New(
        h => h.leaderCardListModel,
        leaderCardListModel => h => h with { leaderCardListModel = leaderCardListModel }
    );

    public bool isSelf { get; init; } = false;

    private CardGenerator cardGenerator { get; init; }

    public HandCardAreaModel(CardGenerator gen, bool isSelf_ = true)
    {
        cardGenerator = gen;
        isSelf = isSelf_;
        leaderCardListModel = new SingleCardListModel(isSelf ? CardLocation.SelfLeaderCardArea : CardLocation.EnemyLeaderCardArea);
    }

    // 初始时调用，设置所有备选卡牌信息
    // enemy设置时idList由对端决定，因此不为null
    public HandCardAreaModel SetBackupCardInfoIdList(List<int> infoIdList, List<int> idList = null)
    {
        var newRecord = this;
        for (int i = 0; i < infoIdList.Count; i++) {
            // 所有备选卡牌生成CardModel并存储
            CardModel card = null;
            var newCardGenerator = newRecord.cardGenerator;
            if (idList != null) {
                card = newCardGenerator.GetCard(infoIdList[i], idList[i]);
            } else {
                newCardGenerator = newCardGenerator.GetCardAndUpdateId(infoIdList[i], out card);
            }
            newRecord = newRecord with {
                cardGenerator = newCardGenerator
            };
            if (card.cardInfo.cardType == CardType.Leader) {
                card = card.ChangeCardLocation(isSelf ? CardLocation.SelfLeaderCardArea : CardLocation.EnemyLeaderCardArea);
                newRecord = newRecord with {
                    leaderCardListModel = newRecord.leaderCardListModel.AddCard(card) as SingleCardListModel
                };
            } else {
                newRecord = newRecord with {
                    backupCardList = newRecord.backupCardList.Add(card)
                };
            }
        }
        return newRecord;
    }

    // 开局时抽取初始手牌
    public HandCardAreaModel DrawInitHandCard()
    {
        var newRecord = this;
        newRecord = newRecord.DrawRandomHandCardList(INIT_HAND_CARD_NUM, out List<CardModel> newCardList);
        newRecord = newRecord with {
            initHandCardListModel = newRecord.initHandCardListModel.AddCardList(newCardList.ToImmutableList()) as InitHandCardListModel
        };
        return newRecord;
    }

    // reDrawCardList的牌放回备选卡牌区，重新抽取相同数量的手牌，不直接放入手牌列表
    public HandCardAreaModel ReDrawInitHandCard()
    {
        var newRecord = this;
        var selectedCardList = newRecord.initHandCardListModel.selectedCardList;
        var newInitHandCardListModel = newRecord.initHandCardListModel;
        var newBackupCardList = newRecord.backupCardList;
        int reDrawCardNum = selectedCardList.Count;
        // 将选好的牌放回backup
        foreach (CardModel reDrawCard in selectedCardList) {
            KLog.I(TAG, "ReDrawInitHandCard: " + reDrawCard.cardInfo.chineseName);
            CardModel resultCard = null;
            newInitHandCardListModel = newInitHandCardListModel.RemoveCard(reDrawCard, out resultCard) as InitHandCardListModel;
            resultCard = resultCard with {
                isSelected = false
            };
            newBackupCardList = newBackupCardList.Add(resultCard);
        }
        newRecord = newRecord with {
            initHandCardListModel = newInitHandCardListModel,
            backupCardList = newBackupCardList
        };
        // 重抽
        newRecord = newRecord.DrawRandomHandCardList(reDrawCardNum, out List<CardModel> newCardList);
        newInitHandCardListModel = newRecord.initHandCardListModel.AddCardList(newCardList.ToImmutableList()) as InitHandCardListModel;
        newRecord = newRecord with {
            initHandCardListModel = newInitHandCardListModel,
            hasReDrawInitHandCard = true
        };
        return newRecord;
    }

    // 将initHandCardListModel中的牌加载到手牌区
    public HandCardAreaModel LoadHandCard()
    {
        var newRecord = this;
        var newHandCardListModel = newRecord.handCardListModel;
        var newInitHandCardListModel = newRecord.initHandCardListModel;
        newInitHandCardListModel = newInitHandCardListModel.RemoveAllCard(out var handCardList) as InitHandCardListModel;
        newHandCardListModel = newHandCardListModel.AddCardList(handCardList.ToImmutableList()) as HandCardListModel;
        newRecord = newRecord with {
            initHandCardListModel = newInitHandCardListModel,
            handCardListModel = newHandCardListModel
        };
        return newRecord;
    }

    // 随机抽牌到手牌区
    public HandCardAreaModel DrawHandCardsRandom(int num, out List<int> idList)
    {
        idList = new List<int>();
        var newRecord = DrawRandomHandCardList(num, out List<CardModel> newCardList);
        foreach (var card in newCardList) {
            idList.Add(card.cardInfo.id);
        }
        var newHandCardListModel = handCardListModel.AddCardList(newCardList.ToImmutableList()) as HandCardListModel;
        newRecord = newRecord with {
            handCardListModel = newHandCardListModel
        };
        return newRecord;
    }

    // 抽牌到手牌区。enemy设置时，随机抽取的操作在对端进行，因此直接指定idList。以及一些指定牌的场景
    public HandCardAreaModel DrawHandCardsWithoutRandom(List<int> idList)
    {
        List<CardModel> newCardList = new List<CardModel>();
        var newRecord = this;
        var newBackupCardList = newRecord.backupCardList;
        foreach (int id in idList) {
            CardModel card = newRecord.backupCardList.Find(o => {
                return o.cardInfo.id == id;
            });
            if (card == null) {
                KLog.E(TAG, "DrawHandCards: invalid id: " + id);
                return newRecord;
            }
            newCardList.Add(card);
            newBackupCardList = newBackupCardList.Remove(card);
        }
        newRecord = newRecord with {
            backupCardList = newBackupCardList,
            handCardListModel = newRecord.handCardListModel.AddCardList(newCardList.ToImmutableList()) as HandCardListModel
        };
        return newRecord;
    }

    // 获取抱团技能的牌
    public HandCardAreaModel RemoveAndGetMuster(string musterType, out List<CardModel> musterCardList)
    {
        var newRecord = this;
        musterCardList = new List<CardModel>();
        // 从手牌中获取
        foreach (CardModel card in newRecord.handCardListModel.cardList) {
            if (card.cardInfo.musterType == musterType) {
                newRecord = newRecord with {
                    handCardListModel = newRecord.handCardListModel.RemoveCard(card, out var removedCard) as HandCardListModel
                };
                musterCardList.Add(removedCard);
            }
        }
        // 从备选卡牌中获取
        foreach (CardModel card in newRecord.backupCardList) {
            if (card.cardInfo.musterType == musterType) {
                newRecord = newRecord with {
                    backupCardList = newRecord.backupCardList.Remove(card)
                };
                musterCardList.Add(card);
            }
        }
        return newRecord;
    }


    // 替换手牌和备选卡牌，ai模拟时使用
    public HandCardAreaModel ReplaceHandAndBackupCard(List<int> handInfoIdList, List<int> handIdList, List<int> backupInfoIdList, List<int> backupIdList)
    {
        var newRecord = this;
        newRecord = newRecord with {
            handCardListModel = newRecord.handCardListModel.RemoveAllCard(out var removedHandCardList) as HandCardListModel,
            backupCardList = newRecord.backupCardList.Clear()
        };
        for (int i = 0; i < handInfoIdList.Count; i++) {
            CardModel card = newRecord.cardGenerator.GetCard(handInfoIdList[i], handIdList[i]);
            newRecord = newRecord with {
                handCardListModel = newRecord.handCardListModel.AddCard(card) as HandCardListModel
            };
        }
        for (int i = 0; i < backupInfoIdList.Count; i++) {
            CardModel card = newRecord.cardGenerator.GetCard(backupInfoIdList[i], backupIdList[i]);
            newRecord = newRecord with {
                backupCardList = newRecord.backupCardList.Add(card)
            };
        }
        return newRecord;
    }

    // 从backup中随机抽取一些牌，但不放入别的地方
    private HandCardAreaModel DrawRandomHandCardList(int num, out List<CardModel> newCardList)
    {
        var newRecord = this;
        newCardList = new List<CardModel>();
        for (int i = 0; i < num; i++) {
            if (newRecord.backupCardList.Count <= 0) {
                KLog.W(TAG, "GetCards: backupCardList not enough, need " + num + ", missing " + (num - newCardList.Count).ToString());
                break;
            }
            System.Random ran = new System.Random();
            CardModel newCard = newRecord.backupCardList[ran.Next(0, newRecord.backupCardList.Count)];
            newCardList.Add(newCard);
            var newBackupCardList = newRecord.backupCardList.Remove(newCard);
            newRecord = newRecord with {
                backupCardList = newBackupCardList
            };
        }
        return newRecord;
    }
}
