using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// 卡牌区域，主要用于场上普通牌的排布
public class RowNormalCardArea : CardArea
{
    // 消除除天气外的debuff
    public void ClearNormalDebuff()
    {
        foreach (GameObject card in cardList)
        {
            card.GetComponent<CardDisplay>().ClearNormalDebuff();
        }
    }

    // 统计一个类型bond的卡牌数量
    public int GetBondCardNum(string bondType)
    {
        int count = 0;
        foreach (GameObject card in cardList)
        {
            if (card.GetComponent<CardDisplay>().cardInfo.bondType == bondType) {
                count++;
            }
        }
        return count;
    }

    // 更新bond的buff
    public void UpdateBondBuff(string bondType, int times)
    {
        foreach (GameObject card in cardList)
        {
            if (card.GetComponent<CardDisplay>().cardInfo.bondType == bondType) {
                card.GetComponent<CardDisplay>().SetBuffTimes(times);
            }
        }
    }

    public int GetCurrentScore()
    {
        int sum = 0;
        foreach (GameObject card in cardList)
        {
            sum += int.Parse(card.GetComponent<CardDisplay>().powerNum.GetComponent<TextMeshProUGUI>().text);
        }
        return sum;
    }
}
