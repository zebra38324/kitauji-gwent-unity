using System.Collections.Generic;
using UnityEngine;

/**
 * 天气牌区域逻辑
 */
public record WeatherAreaModel
{
    private static string TAG = "WeatherAreaModel";

    public SingleCardListModel wood { get; init; } = new SingleCardListModel(CardLocation.WeatherCardArea);

    public SingleCardListModel brass { get; init; } = new SingleCardListModel(CardLocation.WeatherCardArea);

    public SingleCardListModel percussion { get; init; } = new SingleCardListModel(CardLocation.WeatherCardArea);

    public WeatherAreaModel()
    {

    }

    public WeatherAreaModel AddCard(CardModel card)
    {
        card = card.ChangeCardLocation(CardLocation.WeatherCardArea);
        var newRecord = this;
        if (card.cardInfo.ability == CardAbility.SunFes) {
            newRecord = newRecord with {
                wood = newRecord.wood.AddCard(card) as SingleCardListModel
            };
        } else if (card.cardInfo.ability == CardAbility.Daisangakushou) {
            newRecord = newRecord with {
                brass = newRecord.brass.AddCard(card) as SingleCardListModel
            };
        } else if (card.cardInfo.ability == CardAbility.Drumstick) {
            newRecord = newRecord with {
                percussion = newRecord.percussion.AddCard(card) as SingleCardListModel
            };
        } else {
            KLog.E(TAG, "AddCard: invalid ability: " + card.cardInfo.ability);
        }
        return newRecord;
    }

    public WeatherAreaModel RemoveCard(CardModel card, out CardModel removedCard)
    {
        var newRecord = this;
        if (wood.cardList.Contains(card)) {
            newRecord = newRecord with {
                wood = newRecord.wood.RemoveCard(card, out removedCard) as SingleCardListModel
            };
        } else if (brass.cardList.Contains(card)) {
            newRecord = newRecord with {
                brass = newRecord.brass.RemoveCard(card, out removedCard) as SingleCardListModel
            };
        } else if (percussion.cardList.Contains(card)) {
            newRecord = newRecord with {
                percussion = newRecord.percussion.RemoveCard(card, out removedCard) as SingleCardListModel
            };
        } else {
            KLog.E(TAG, "RemoveCard: invalid card: " + card);
            removedCard = card;
        }
        return newRecord;
    }

    public WeatherAreaModel RemoveAllCard(out List<CardModel> removedCardList)
    {
        var newRecord = this with {
            wood = wood.RemoveAllCard(out var removedWoodCardList) as SingleCardListModel,
            brass = brass.RemoveAllCard(out var removedBrassCardList) as SingleCardListModel,
            percussion = percussion.RemoveAllCard(out var removedPercussionCardList) as SingleCardListModel
        };
        removedCardList = new List<CardModel>();
        removedCardList.AddRange(removedWoodCardList);
        removedCardList.AddRange(removedBrassCardList);
        removedCardList.AddRange(removedPercussionCardList);
        return newRecord;
    }
}
