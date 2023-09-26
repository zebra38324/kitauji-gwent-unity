using UnityEngine;
using TMPro;

// 打出的牌所在区域，一个RowArea为一排，包括这一排的分数、指挥牌、普通牌
public class RowArea : MonoBehaviour
{
    public GameObject scoreNum;
    public GameObject normalArea;

    public delegate void ScoreChangeNotifyHandler(int diff);
    public event ScoreChangeNotifyHandler ScoreChangeNotify;

    private int currentScore;

    public void AddNormalCard(GameObject newCard)
    {
        newCard.transform.SetParent(normalArea.transform);
        normalArea.GetComponent<CardArea>().AddCard(newCard);
        UpdateScore();
    }

    public void ClearNormalDebuff() 
    {
        normalArea.GetComponent<CardArea>().ClearNormalDebuff();
        UpdateScore();
    }

    public int GetBondCardNum(string bondType)
    {
        return normalArea.GetComponent<CardArea>().GetBondCardNum(bondType);
    }

    public void UpdateBondBuff(string bondType, int times)
    {
        normalArea.GetComponent<CardArea>().UpdateBondBuff(bondType, times);
        UpdateScore();
    }

    private void UpdateScore()
    {
        int newScore = normalArea.GetComponent<CardArea>().GetCurrentScore();
        int diff = newScore - currentScore;
        ScoreChangeNotify(diff);
        currentScore = newScore;
        scoreNum.GetComponent<TextMeshProUGUI>().text = currentScore.ToString();
    }
}
