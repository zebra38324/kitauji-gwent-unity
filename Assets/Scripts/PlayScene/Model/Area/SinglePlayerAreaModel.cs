using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.Immutable;
using LanguageExt;
using static LanguageExt.Prelude;

// 单方对战区逻辑
public record SinglePlayerAreaModel
{
    private static string TAG = "SinglePlayerAreaModel";

    public DiscardAreaModel discardAreaModel { get; init; } = new DiscardAreaModel();

    public static readonly Lens<SinglePlayerAreaModel, DiscardAreaModel> Lens_DiscardAreaModel = Lens<SinglePlayerAreaModel, DiscardAreaModel>.New(
        s => s.discardAreaModel,
        discardAreaModel => s => s with { discardAreaModel = discardAreaModel }
    );

    public static readonly Lens<SinglePlayerAreaModel, ImmutableList<CardModel>> Lens_DiscardAreaModel_CardListModel_Cardlist = lens(Lens_DiscardAreaModel, DiscardAreaModel.Lens_CardListModel_CardList);

    public ImmutableList<BattleRowAreaModel> battleRowAreaList { get; init; }

    public HandCardAreaModel handCardAreaModel { get; init; }

    public static readonly Lens<SinglePlayerAreaModel, HandCardAreaModel> Lens_HandCardAreaModel = Lens<SinglePlayerAreaModel, HandCardAreaModel>.New(
        s => s.handCardAreaModel,
        handCardAreaModel => s => s with { handCardAreaModel = handCardAreaModel }
    );

    public static readonly Lens<SinglePlayerAreaModel, HandCardListModel> Lens_HandCardAreaModel_HandCardListModel = lens(Lens_HandCardAreaModel, HandCardAreaModel.Lens_HandCardListModel);

    public static readonly Lens<SinglePlayerAreaModel, SingleCardListModel> Lens_HandCardAreaModel_LeaderCardListModel = lens(Lens_HandCardAreaModel, HandCardAreaModel.Lens_LeaderCardListModel);

    public SinglePlayerAreaModel(CardGenerator gen, bool isSelf = true)
    {
        battleRowAreaList = ImmutableList<BattleRowAreaModel>.Empty;
        for (CardBadgeType cardBadgeType = CardBadgeType.Wood; cardBadgeType <= CardBadgeType.Percussion; cardBadgeType++) {
            battleRowAreaList = battleRowAreaList.Add(new BattleRowAreaModel(cardBadgeType));
        }
        handCardAreaModel = new HandCardAreaModel(gen, isSelf);
    }

    public SinglePlayerAreaModel AddBattleAreaCard(CardModel card)
    {
        var newRecord = this;
        // 先打出卡牌，再结算技能
        var rowArea = newRecord.battleRowAreaList[(int)card.cardInfo.badgeType];
        var newRowArea = rowArea.AddCard(card);
        newRecord = newRecord with {
            battleRowAreaList = newRecord.battleRowAreaList.SetItem((int)card.cardInfo.badgeType, newRowArea)
        };

        // 实施卡牌技能
        card = newRowArea.FindCard(card.cardInfo.id);
        switch (card.cardInfo.ability) {
            case CardAbility.Tunning: {
                newRecord = newRecord.ApplyTunning();
                break;
            }
            case CardAbility.Bond: {
                newRecord = newRecord.UpdateBond(card.cardInfo.bondType);
                break;
            }
            case CardAbility.Muster: {
                newRecord = newRecord.ApplyMuster(card.cardInfo.musterType);
                break;
            }
            case CardAbility.HornBrass: {
                var oldBrassRow = newRecord.battleRowAreaList[(int)CardBadgeType.Brass];
                var newBrassRow = oldBrassRow.AddCard(card);
                newRecord = newRecord with {
                    battleRowAreaList = newRecord.battleRowAreaList.Replace(oldBrassRow, newBrassRow)
                };
                break;
            }
            case CardAbility.K5Leader: {
                newRecord = newRecord.ApplyK5Leader(card);
                break;
            }
            case CardAbility.Pressure: {
                newRecord = newRecord.ApplyPressure();
                break;
            }
        }
        return newRecord;
    }

    // 伞击技能，木管行总点数大于10，将点数最高的卡牌设置Scorch
    public SinglePlayerAreaModel ApplyScorchWood()
    {
        var newRecord = this;
        var newWoodRow = newRecord.battleRowAreaList[(int)CardBadgeType.Wood].ApplyScorchWood();
        newRecord = newRecord with {
            battleRowAreaList = newRecord.battleRowAreaList.SetItem((int)CardBadgeType.Wood, newWoodRow)
        };
        return newRecord;
    }

    public int GetCurrentPower()
    {
        int sum = 0;
        foreach (BattleRowAreaModel row in battleRowAreaList) {
            sum += row.GetCurrentPower();
        }
        return sum;
    }

    // 获取非英雄牌中的最大点数
    public int GetMaxNormalPower()
    {
        int max = 0;
        foreach (BattleRowAreaModel row in battleRowAreaList) {
            foreach (CardModel card in row.cardListModel.cardList) {
                if (card.cardInfo.cardType == CardType.Normal && card.currentPower > max) {
                    max = card.currentPower;
                }
            }
        }
        return max;
    }

    // 退部技能，移除点数为targetPower的卡牌设置Scorch
    public SinglePlayerAreaModel ApplyScorch(int targetPower)
    {
        return BattleApplyAction(card => {
            return card.cardInfo.cardType == CardType.Normal && card.currentPower == targetPower;
        }, card => {
            return card.SetScorch();
        }, out var actionCardList);
    }

    // 某些技能会导致卡牌退部，将他们统一移至弃牌区，并返回移除的卡牌列表。注意返回的card并不是实际存在discard中的card
    public SinglePlayerAreaModel RemoveDeadCard(out List<CardModel> removedCardList)
    {
        var newRecord = this;
        var newDiscardAreaModel = newRecord.discardAreaModel;
        removedCardList = new List<CardModel>();
        foreach (BattleRowAreaModel row in newRecord.battleRowAreaList) {
            var newRow = row;
            foreach (CardModel card in row.cardListModel.cardList) {
                if (card.IsDead()) {
                    KLog.I(TAG, "RemoveDeadCard: remove card: " + card.cardInfo.chineseName);
                    newRow = newRow.RemoveCard(card, out var removedCard);
                    removedCardList.Add(removedCard);
                    newDiscardAreaModel = newDiscardAreaModel.AddCard(removedCard);
                }
            }
            newRecord = newRecord with {
                battleRowAreaList = newRecord.battleRowAreaList.Replace(row, newRow)
            };
        }
        newRecord = newRecord with {
            discardAreaModel = newDiscardAreaModel
        };
        foreach (CardModel card in removedCardList) {
            if (card.cardInfo.ability == CardAbility.Bond) {
                newRecord = newRecord.UpdateBond(card.cardInfo.bondType);
            }
        }
        return newRecord;
    }

    // Lip技能，男性卡牌点数降低2，并返回攻击的卡牌列表。注意返回的card并不是实际存在model中的card
    public SinglePlayerAreaModel ApplyLip(out List<CardModel> attackCardList)
    {
        return BattleApplyAction(card => {
            return card.cardInfo.cardType == CardType.Normal && card.cardInfo.isMale;
        }, card => {
            return card.AddBuff(CardBuffType.Attack2, 1);
        }, out attackCardList);
    }

    // 准备可攻击目标，并返回可攻击目标的卡牌列表。注意返回的card并不是实际存在model中的card
    public SinglePlayerAreaModel PrepareAttackTarget(out List<CardModel> targetCardList)
    {
        List<bool> hasDefend = battleRowAreaList.Select(row => {
            foreach (CardModel card in row.cardListModel.cardList) {
                if (card.cardInfo.ability == CardAbility.Defend) {
                    return true;
                }
            }
            return false;
        }).ToList();
        return BattleApplyAction(card => {
            return card.cardInfo.cardType == CardType.Normal && !hasDefend[(int)card.cardInfo.badgeType];
        }, card => {
            return card.ChangeCardSelectType(CardSelectType.WithstandAttack);
        }, out targetCardList);
    }

    // 准备可复活目标，并返回可复活目标的卡牌列表。注意返回的card并不是实际存在model中的card
    public SinglePlayerAreaModel PrepareMedicTarget(out List<CardModel> targetCardList)
    {
        return DiscardApplyAction(card => {
            return card.cardInfo.cardType == CardType.Normal;
        }, card => {
            return card.ChangeCardSelectType(CardSelectType.PlayCard);
        }, out targetCardList);
    }

    // 准备可撤回目标，并返回可撤回目标的卡牌列表。注意返回的card并不是实际存在model中的card
    public SinglePlayerAreaModel PrepareDecoyTarget(out List<CardModel> targetCardList)
    {
        return BattleApplyAction(card => {
            return card.cardInfo.cardType == CardType.Normal;
        }, card => {
            return card.ChangeCardSelectType(CardSelectType.DecoyWithdraw);
        }, out targetCardList);
    }

    // 使用decoyCard替换targetCard，并将targetCard放回手牌区
    public SinglePlayerAreaModel DecoyWithdrawCard(CardModel targetCard, CardModel decoyCard)
    {
        var newRecord = this;
        int rowIndex = (int)targetCard.cardInfo.badgeType;
        var newRow = newRecord.battleRowAreaList[rowIndex].ReplaceCard(targetCard, decoyCard);
        newRecord = newRecord with {
            battleRowAreaList = newRecord.battleRowAreaList.SetItem(rowIndex, newRow),
        };
        targetCard = targetCard.RemoveAllBuff();
        newRecord = Lens_HandCardAreaModel_HandCardListModel.Set(newRecord.handCardAreaModel.handCardListModel.AddCard(targetCard) as HandCardListModel, newRecord);
        return newRecord;
    }

    public bool HasCardInBattleArea(string chineseName)
    {
        foreach (BattleRowAreaModel row in battleRowAreaList) {
            foreach (CardModel card in row.cardListModel.cardList) {
                if (card.cardInfo.chineseName == chineseName) {
                    return true;
                }
            }
            foreach (CardModel card in row.hornCardListModel.cardList) {
                if (card.cardInfo.chineseName == chineseName) {
                    return true;
                }
            }
        }
        return false;
    }

    // 准备monaka目标，并返回monaka目标的卡牌列表。注意返回的card并不是实际存在model中的card
    public SinglePlayerAreaModel PrepareMonakaTarget(out List<CardModel> targetCardList)
    {
        return BattleApplyAction(card => {
            return card.cardInfo.cardType == CardType.Normal;
        }, card => {
            return card.ChangeCardSelectType(CardSelectType.Monaka);
        }, out targetCardList);
    }

    // 重置battle区与discard区的选择状态
    public SinglePlayerAreaModel ResetCardSelectType()
    {
        var newRecord = this;

        newRecord = newRecord.BattleApplyAction(card => {
            return card.cardInfo.cardType == CardType.Normal;
        }, card => {
            return card.ChangeCardSelectType(CardSelectType.None);
        }, out var targetCardList);

        newRecord = newRecord.DiscardApplyAction(card => {
            return card.cardInfo.cardType == CardType.Normal;
        }, card => {
            return card.ChangeCardSelectType(CardSelectType.None);
        }, out targetCardList);
        return newRecord;
    }

    // 小局结束，将对战区的牌移除，角色牌移入弃牌区
    public SinglePlayerAreaModel RemoveAllBattleCard()
    {
        var newRecord = this;
        var newDiscardAreaModel = newRecord.discardAreaModel;
        foreach (BattleRowAreaModel row in newRecord.battleRowAreaList) {
            var newRow = row.RemoveAllCard(out var removedCardList);
            foreach (var card in removedCardList) {
                if (card.cardInfo.cardType == CardType.Normal || card.cardInfo.cardType == CardType.Hero) {
                    // 仅角色牌进入弃牌区
                    newDiscardAreaModel = newDiscardAreaModel.AddCard(card);
                }
            }
            newRecord = newRecord with {
                battleRowAreaList = newRecord.battleRowAreaList.Replace(row, newRow)
            };
        }
        newRecord = newRecord with {
            discardAreaModel = newDiscardAreaModel
        };
        return newRecord;
    }

    // 封装对于特定卡牌的操作
    private SinglePlayerAreaModel BattleApplyAction(Func<CardModel, bool> needAction, Func<CardModel, CardModel> action, out List<CardModel> actionCardList)
    {
        var newRecord = this;
        actionCardList = new List<CardModel>();
        foreach (BattleRowAreaModel row in newRecord.battleRowAreaList) {
            var newRow = row;
            foreach (CardModel card in row.cardListModel.cardList) {
                if (needAction(card)) {
                    var newCard = action(card);
                    actionCardList.Add(newCard);
                    newRow = BattleRowAreaModel.Lens_CardListModel_CardList.Set(newRow.cardListModel.cardList.Replace(card, newCard), newRow);
                }
            }
            newRecord = newRecord with {
                battleRowAreaList = newRecord.battleRowAreaList.Replace(row, newRow)
            };
        }
        return newRecord;
    }

    // 封装对于弃牌区特定卡牌的操作
    private SinglePlayerAreaModel DiscardApplyAction(Func<CardModel, bool> needAction, Func<CardModel, CardModel> action, out List<CardModel> actionCardList)
    {
        var newRecord = this;
        actionCardList = new List<CardModel>();
        foreach (CardModel card in newRecord.discardAreaModel.cardListModel.cardList) {
            if (needAction(card)) {
                var newCard = action(card);
                actionCardList.Add(newCard);
                newRecord = Lens_DiscardAreaModel_CardListModel_Cardlist.Set(newRecord.discardAreaModel.cardListModel.cardList.Replace(card, newCard), newRecord);
            }
        }
        return newRecord;
    }

    // 应用Tunning技能
    private SinglePlayerAreaModel ApplyTunning()
    {
        var newRecord = this;
        foreach (BattleRowAreaModel row in newRecord.battleRowAreaList) {
            var newRow = row;
            foreach (CardModel card in newRow.cardListModel.cardList) {
                newRow = newRow.ReplaceCard(card, card.RemoveNormalDebuff());
            }
            newRecord = newRecord with {
                battleRowAreaList = newRecord.battleRowAreaList.Replace(row, newRow)
            };
        }
        return newRecord;
    }

    // 更新bond技能
    private SinglePlayerAreaModel UpdateBond(string bondType)
    {
        var newRecord = this;
        // 统计场上同类型的bond牌数量
        int count = 0;
        foreach (BattleRowAreaModel row in newRecord.battleRowAreaList) {
            foreach (CardModel card in row.cardListModel.cardList) {
                if (card.cardInfo.ability == CardAbility.Bond && card.cardInfo.bondType == bondType) {
                    count++;
                }
            }
        }
        // 更新bond数据
        foreach (BattleRowAreaModel row in newRecord.battleRowAreaList) {
            var newRow = row;
            foreach (CardModel card in row.cardListModel.cardList) {
                if (card.cardInfo.ability == CardAbility.Bond && card.cardInfo.bondType == bondType) {
                    var realCard = newRow.FindCard(card.cardInfo.id);
                    newRow = newRow.ReplaceCard(realCard, realCard.SetBuff(CardBuffType.Bond, count - 1));
                }
            }
            newRecord = newRecord with {
                battleRowAreaList = newRecord.battleRowAreaList.Replace(row, newRow)
            };
        }
        return newRecord;
    }

    // 应用抱团技能
    private SinglePlayerAreaModel ApplyMuster(string musterType)
    {
        var newRecord = this;
        newRecord = newRecord with {
            handCardAreaModel = newRecord.handCardAreaModel.RemoveAndGetMuster(musterType, out var musterCardList)
        };
        // 添加卡牌。为避免递归调用造成错误，应先把要打出的卡牌选出来再一起打出
        foreach (CardModel card in musterCardList) {
            newRecord = newRecord.AddBattleAreaCard(card);
        }
        return newRecord;
    }

    private SinglePlayerAreaModel ApplyK5Leader(CardModel k5LeaderCard)
    {
        var newRecord = this;
        foreach (BattleRowAreaModel row in newRecord.battleRowAreaList) {
            var newRow = row;
            foreach (CardModel card in row.cardListModel.cardList) {
                if (card.cardInfo.id != k5LeaderCard.cardInfo.id && card.cardInfo.cardType == CardType.Normal && card.cardInfo.grade == 1) {
                    var realCard = newRow.FindCard(card.cardInfo.id);
                    newRow = newRow.ReplaceCard(realCard, realCard.AddBuff(CardBuffType.K5Leader, 1));
                }
            }
            newRecord = newRecord with {
                battleRowAreaList = newRecord.battleRowAreaList.Replace(row, newRow)
            };
        }
        return newRecord;
    }

    private SinglePlayerAreaModel ApplyPressure()
    {
        // 注意：目前默认只有英雄牌有这个技能
        var newRecord = this;
        foreach (BattleRowAreaModel row in newRecord.battleRowAreaList) {
            var newRow = row;
            foreach (CardModel card in row.cardListModel.cardList) {
                if (card.cardInfo.cardType == CardType.Normal) {
                    var realCard = newRow.FindCard(card.cardInfo.id);
                    CardBuffType type = new Random().Next(0, 2) == 0 ? CardBuffType.PressurePlus : CardBuffType.PressureMinus;
                    newRow = newRow.ReplaceCard(realCard, realCard.AddBuff(type, 1));
                }
            }
            newRecord = newRecord with {
                battleRowAreaList = newRecord.battleRowAreaList.Replace(row, newRow)
            };
        }
        return newRecord;
    }
}
