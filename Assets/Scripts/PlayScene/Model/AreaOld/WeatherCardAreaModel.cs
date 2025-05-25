using System.Collections.Generic;
using UnityEngine;
/**
 * 天气牌区域逻辑
 */
public class WeatherCardAreaModel
{
    private static string TAG = "WeatherCardAreaModel";

    public SingleCardRowAreaModel woodArea { get; private set; }

    public SingleCardRowAreaModel brassArea { get; private set; }

    public SingleCardRowAreaModel percussionArea { get; private set; }

    public WeatherCardAreaModel()
    {
        woodArea = new SingleCardRowAreaModel();
        brassArea = new SingleCardRowAreaModel();
        percussionArea = new SingleCardRowAreaModel();
    }

    public void AddCard(CardModelOld card)
    {
        if (card.cardInfo.ability == CardAbility.SunFes) {
            AddCardToRowArea(woodArea, card);
        } else if (card.cardInfo.ability == CardAbility.Daisangakushou) {
            AddCardToRowArea(brassArea, card);
        } else if (card.cardInfo.ability == CardAbility.Drumstick) {
            AddCardToRowArea(percussionArea, card);
        } else {
            KLog.E(TAG, "AddCard: invalid ability: " + card.cardInfo.ability);
        }
    }

    public void RemoveAllCard()
    {
        woodArea.RemoveAllCard();
        brassArea.RemoveAllCard();
        percussionArea.RemoveAllCard();
    }

    private void AddCardToRowArea(SingleCardRowAreaModel rowArea, CardModelOld card)
    {
        card.cardLocation = CardLocation.WeatherCardArea;
        rowArea.AddCard(card);
    }
}
