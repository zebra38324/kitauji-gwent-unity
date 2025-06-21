using System.Collections.Generic;
using System.Linq;
using System.Collections.Immutable;
using LanguageExt;
using static LanguageExt.Prelude;

/**
 * 对战区行区域逻辑
 */
public record BattleRowAreaModel
{
    private static string TAG = "BattleRowAreaModel";

    public CardListModel cardListModel { get; init; } = new CardListModel();

    public SingleCardListModel hornCardListModel { get; init; } = new SingleCardListModel(CardLocation.BattleArea);

    public static readonly Lens<BattleRowAreaModel, CardListModel> Lens_CardListModel = Lens<BattleRowAreaModel, CardListModel>.New(
        b => b.cardListModel,
        cardListModel => b => b with { cardListModel = cardListModel }
    );

    public static readonly Lens<BattleRowAreaModel, ImmutableList<CardModel>> Lens_CardListModel_CardList = lens(Lens_CardListModel, CardListModel.Lens_CardList);

    public CardBadgeType rowType { get; init; }

    public bool hasWeatherBuff { get; init; } = false;

    public BattleRowAreaModel(CardBadgeType type)
    {
        rowType = type;
    }

    public BattleRowAreaModel AddCard(CardModel card)
    {
        card = card.ChangeCardLocation(CardLocation.BattleArea);
        var newRecord = this;
        if (card.cardInfo.ability == CardAbility.HornUtil ||
            card.cardInfo.ability == CardAbility.HornBrass) {
            var newHornCardListModel = newRecord.hornCardListModel.AddCard(card) as SingleCardListModel;
            newRecord = newRecord with {
                hornCardListModel = newHornCardListModel
            };
        } else {
            if (card.cardInfo.ability == CardAbility.Kasa) {
                newRecord = newRecord.ApplyKasa();
            } else if (card.cardInfo.ability == CardAbility.SalutdAmour) {
                card = newRecord.ApplySalutdAmour(card);
            }
            var newCardListModel = newRecord.cardListModel.AddCard(card);
            newRecord = newRecord with {
                cardListModel = newCardListModel
            };
        }
        return newRecord.UpdateBuff();
    }

    public BattleRowAreaModel RemoveCard(CardModel card, out CardModel removedCard)
    {
        var newRecord = this;
        newRecord = newRecord with {
            cardListModel = cardListModel.RemoveCard(card, out removedCard)
        };
        removedCard = removedCard.RemoveAllBuff();
        return newRecord.UpdateBuff();
    }

    public BattleRowAreaModel RemoveAllCard(out List<CardModel> removedCardList)
    {
        var newRecord = this;
        newRecord = newRecord with {
            cardListModel = cardListModel.RemoveAllCard(out removedCardList),
            hornCardListModel = hornCardListModel.RemoveAllCard(out var removedHornCardList) as SingleCardListModel
        };
        removedCardList = removedCardList.Select(card => card.RemoveAllBuff()).ToList();
        removedCardList.AddRange(removedHornCardList);
        return newRecord.UpdateBuff();
    }

    // 伞击技能，木管行总点数大于10，将点数最高的卡牌设置Scorch
    public BattleRowAreaModel ApplyScorchWood()
    {
        if (rowType != CardBadgeType.Wood || GetCurrentPower() <= 10) {
            return this;
        }
        List<CardModel> targetCardList = new List<CardModel>();
        int maxPower = -1;
        foreach (CardModel card in cardListModel.cardList) {
            if (card.cardInfo.cardType != CardType.Normal) {
                continue;
            }
            int cardPower = card.currentPower;
            if (cardPower > maxPower) {
                targetCardList.Clear();
                targetCardList.Add(card);
                maxPower = cardPower;
            } else if (cardPower == maxPower) {
                targetCardList.Add(card);
            }
        }
        var newRecord = this;
        foreach (CardModel card in targetCardList) {
            var newCard = card.SetScorch();
            newRecord = Lens_CardListModel_CardList.Set(newRecord.cardListModel.cardList.Replace(card, newCard), newRecord);
        }
        return newRecord;
    }

    // 为现有的卡牌调整buff，或者替换
    // 宽松的，寻找对应id
    public BattleRowAreaModel ReplaceCard(CardModel oldCard, CardModel newCard)
    {
        var newRecord = this;
        newCard = newCard.ChangeCardLocation(CardLocation.BattleArea);
        var oldCardList = newRecord.cardListModel.cardList;
        oldCard = oldCardList.Find(x => x.cardInfo.id == oldCard.cardInfo.id);
        if (oldCard != null) {
            newRecord = Lens_CardListModel_CardList.Set(oldCardList.Replace(oldCard, newCard), newRecord);
        }
        return newRecord.UpdateBuff();
    }

    public int GetCurrentPower()
    {
        int sum = 0;
        foreach (CardModel card in cardListModel.cardList) {
            sum += card.currentPower;
        }
        return sum;
    }

    public BattleRowAreaModel SetWeatherBuff(bool flag)
    {
        var newRecord = this;
        if (flag == hasWeatherBuff) {
            return newRecord;
        }
        newRecord = newRecord with {
            hasWeatherBuff = flag
        };
        return newRecord.UpdateBuff();
    }

    public CardModel FindCard(int id)
    {
        foreach (CardModel card in cardListModel.cardList) {
            if (card.cardInfo.id == id) {
                return card;
            }
        }
        foreach (CardModel card in hornCardListModel.cardList) {
            if (card.cardInfo.id == id) {
                return card;
            }
        }
        KLog.E(TAG, "FindCard: invalid id: " + id);
        return null;
    }

    private BattleRowAreaModel ApplyKasa()
    {
        var newRecord = this;
        foreach (CardModel card in newRecord.cardListModel.cardList) {
            if (card.cardInfo.chineseName == "铠冢霙" && card.cardInfo.cardType == CardType.Normal) {
                var newCard = card.RemoveNormalDebuff()
                    .AddBuff(CardBuffType.Kasa, 1);
                return Lens_CardListModel_CardList.Set(newRecord.cardListModel.cardList.Replace(card, newCard), newRecord);
            }
        }
        return newRecord;
    }

    private BattleRowAreaModel UpdateBuff()
    {
        var newRecord = this;
        newRecord = newRecord.UpdateMoraleBuff();
        newRecord = newRecord.UpdateHornBuff();
        newRecord = newRecord.UpdateWeatherBuff();
        newRecord = newRecord.UpdateDefend();
        return newRecord;
    }

    private BattleRowAreaModel UpdateMoraleBuff()
    {
        var newRecord = this;
        int moraleCount = 0;
        foreach (CardModel card in newRecord.cardListModel.cardList) {
            if (card.cardInfo.ability == CardAbility.Morale) {
                moraleCount++;
            }
        }
        var newCardList = newRecord.cardListModel.cardList.Select(card => {
            var newCard = card;
            if (card.cardInfo.ability != CardAbility.Morale) {
                newCard = card.SetBuff(CardBuffType.Morale, moraleCount);
            } else if (moraleCount > 0) {
                newCard = card.SetBuff(CardBuffType.Morale, moraleCount - 1);
            }
            return newCard;
        }).ToImmutableList();
        return Lens_CardListModel_CardList.Set(newCardList, newRecord);
    }

    private BattleRowAreaModel UpdateHornBuff()
    {
        var newRecord = this;
        int hornCount = hornCardListModel.cardList.Count;
        foreach (CardModel card in newRecord.cardListModel.cardList) {
            if (card.cardInfo.ability == CardAbility.Horn) {
                hornCount++;
            }
        }
        var newCardList = newRecord.cardListModel.cardList.Select(card => {
            var newCard = card;
            if (card.cardInfo.ability != CardAbility.Horn) {
                newCard = card.SetBuff(CardBuffType.Horn, hornCount);
            } else if (hornCount > 0) {
                newCard = card.SetBuff(CardBuffType.Horn, hornCount - 1);
            }
            return newCard;
        }).ToImmutableList();
        return Lens_CardListModel_CardList.Set(newCardList, newRecord);
    }

    private BattleRowAreaModel UpdateWeatherBuff()
    {
        var newRecord = this;
        var newCardList = newRecord.cardListModel.cardList.Select(card => {
            var newCard = card;
            if (newRecord.hasWeatherBuff) {
                newCard = card.SetBuff(CardBuffType.Weather, 1);
            } else {
                newCard = card.RemoveBuff(CardBuffType.Weather);
            }
            return newCard;
        }).ToImmutableList();
        return Lens_CardListModel_CardList.Set(newCardList, newRecord);
    }

    private BattleRowAreaModel UpdateDefend()
    {
        var newRecord = this;
        bool hasDefend = newRecord.cardListModel.cardList.Find(x => x.cardInfo.ability == CardAbility.Defend) != null;
        var newCardList = newRecord.cardListModel.cardList.Select(card => card.SetUnderDefend(hasDefend)).ToImmutableList();
        return Lens_CardListModel_CardList.Set(newCardList, newRecord);
    }

    private CardModel ApplySalutdAmour(CardModel card)
    {
        foreach (CardModel releatedCard in cardListModel.cardList) {
            if (releatedCard.cardInfo.chineseName == "川岛绿辉") {
                return card.AddBuff(CardBuffType.SalutdAmour, 1);
            }
        }
        return card;
    }
}
