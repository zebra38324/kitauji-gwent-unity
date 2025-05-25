using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class TestGenCards
{
    private static CardGeneratorOld cardGenerator = new CardGeneratorOld();

    public static List<CardModelOld> GetCardList(List<int> infoIdList)
    {
        List<CardModelOld> result = new List<CardModelOld>();
        foreach (int infoId in infoIdList) {
            result.Add(GetCard(infoId));
        }
        return result;
    }

    public static CardModelOld GetCard(int infoId)
    {
        return cardGenerator.GetCard(infoId);
    }

    public static CardModelOld GetCard(int infoId, int id)
    {
        return cardGenerator.GetCard(infoId, id);
    }
}
