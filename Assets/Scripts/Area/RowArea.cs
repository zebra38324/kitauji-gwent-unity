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

    public void ClearCard(DiscardCardManager manager)
    {
        normalArea.GetComponent<RowNormalCardArea>().ClearCard(manager);
        UpdateScore();
    }

    public int ReadyEmbraceAttack(int num)
    {
        return normalArea.GetComponent<RowNormalCardArea>().ReadyEmbraceAttack(num);
    }

    public void FinishWithstandAttack()
    {
        normalArea.GetComponent<RowNormalCardArea>().FinishWithstandAttack();
        UpdateScore();
    }

    public void RemoveSingleCard(GameObject card)
    {
        normalArea.GetComponent<CardArea>().RemoveCard(card);
    }
}
