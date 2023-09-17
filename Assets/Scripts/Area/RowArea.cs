using UnityEngine;
using TMPro;

// 打出的牌所在区域，一个RowArea为一排，包括这一排的分数、指挥牌、普通牌
public class RowArea : MonoBehaviour
{
    public GameObject scoreNum;
    public GameObject normalArea;

    public delegate void ScoreChangeNotifyHandler(int diff);
    public event ScoreChangeNotifyHandler ScoreChangeNotify;

    public void AddNormalCard(GameObject newCard)
    {
        newCard.transform.SetParent(normalArea.transform);
        normalArea.GetComponent<CardArea>().AddCard(newCard);

        // 计算分数
        int currentScore = int.Parse(scoreNum.GetComponent<TextMeshProUGUI>().text);
        int diff = newCard.GetComponent<CardDisplay>().cardInfo.originPower;
        scoreNum.GetComponent<TextMeshProUGUI>().text = (currentScore + diff).ToString();
        ScoreChangeNotify(diff);
    }
}
