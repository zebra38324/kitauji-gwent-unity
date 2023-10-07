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
        normalArea.GetComponent<RowNormalCardArea>().AddCard(newCard);
        UpdateScore();
    }

    public void ClearNormalDebuff() 
    {
        normalArea.GetComponent<RowNormalCardArea>().ClearNormalDebuff();
        UpdateScore();
    }

    public int GetBondCardNum(string bondType)
    {
        return normalArea.GetComponent<RowNormalCardArea>().GetBondCardNum(bondType);
    }

    public void UpdateBondBuff(string bondType, int times)
    {
        normalArea.GetComponent<RowNormalCardArea>().UpdateBondBuff(bondType, times);
        UpdateScore();
    }

    public void ScorchWood()
    {
        normalArea.GetComponent<RowNormalCardArea>().ScorchWood();
        UpdateScore();
    }

    private void UpdateScore()
    {
        int newScore = normalArea.GetComponent<RowNormalCardArea>().GetCurrentScore();
        int diff = newScore - currentScore;
        ScoreChangeNotify(diff);
        currentScore = newScore;
        scoreNum.GetComponent<TextMeshProUGUI>().text = currentScore.ToString();
    }
}
