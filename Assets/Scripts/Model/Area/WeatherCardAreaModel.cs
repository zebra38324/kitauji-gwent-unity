using System.Collections.Generic;
using UnityEngine;
/**
 * 天气牌区域逻辑
 */
public class WeatherCardAreaModel
{
    public RowAreaModel woodArea { get; private set; }

    public RowAreaModel brassArea { get; private set; }

    public RowAreaModel percussionArea { get; private set; }

    public WeatherCardAreaModel()
    {
        woodArea = new RowAreaModel();
        brassArea = new RowAreaModel();
        percussionArea = new RowAreaModel();
    }

    public void AddCard(CardModel card)
    {

    }
}
