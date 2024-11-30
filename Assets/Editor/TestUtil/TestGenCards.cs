using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class TestGenCards
{
    private static CardGenerator cardGenerator = new CardGenerator();

    public static List<CardModel> GetCardList(List<int> infoIdList)
    {
        List<CardModel> result = new List<CardModel>();
        foreach (int infoId in infoIdList) {
            result.Add(GetCard(infoId));
        }
        return result;
    }

    public static CardModel GetCard(int infoId)
    {
        return cardGenerator.GetCard(infoId);
    }

    public static CardModel GetCard(int infoId, int id)
    {
        return cardGenerator.GetCard(infoId, id);
    }
}
