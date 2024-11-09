using UnityEngine;
using TMPro;

// 打出的牌所在区域，一个RowArea为一排，包括这一排的分数、指挥牌、普通牌
public class RowArea : MonoBehaviour
{
    public GameObject scoreNum;
    public GameObject normalArea;

    private int currentScore;

    public void AddNormalCard(GameObject newCard)
    {
        normalArea.GetComponent<RowNormalCardArea>().AddCard(newCard);
    }

    public int UpdateScore()
    {
        currentScore = normalArea.GetComponent<RowNormalCardArea>().GetCurrentScore();
        scoreNum.GetComponent<TextMeshProUGUI>().text = currentScore.ToString();
        return currentScore;
    }

    public void ScorchWood()
    {
        normalArea.GetComponent<RowNormalCardArea>().ScorchWood();
    }

    public void ClearCard(DiscardCardManager manager)
    {
        normalArea.GetComponent<RowNormalCardArea>().ClearCard(manager);
    }

    public void RemoveSingleCard(GameObject card)
    {
        normalArea.GetComponent<CardArea>().RemoveCard(card);
    }
}
