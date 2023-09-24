using TMPro;
using UnityEngine;

// 包含三行打出的牌，及分数区域
public class SinglePlayerArea : MonoBehaviour
{
    public GameObject woodRow;
    public GameObject brassRow;
    public GameObject percussionRow;
    public GameObject scoreNum;

    // Start is called before the first frame update
    void Start()
    {
        woodRow.GetComponent<RowArea>().ScoreChangeNotify += new RowArea.ScoreChangeNotifyHandler(ScoreUpdate);
        brassRow.GetComponent<RowArea>().ScoreChangeNotify += new RowArea.ScoreChangeNotifyHandler(ScoreUpdate);
        percussionRow.GetComponent<RowArea>().ScoreChangeNotify += new RowArea.ScoreChangeNotifyHandler(ScoreUpdate);
    }

    void ScoreUpdate(int diff)
    {
        int currentScore = int.Parse(scoreNum.GetComponent<TextMeshProUGUI>().text);
        scoreNum.GetComponent<TextMeshProUGUI>().text = (currentScore + diff).ToString();
    }

    public void AddNormalCard(GameObject newCard)
    {
        GameObject targetArea;
        switch (newCard.GetComponent<CardDisplay>().cardInfo.badgeType) {
            case CardBadgeType.Wood: {
                targetArea = woodRow;
                break;
            }
            case CardBadgeType.Brass: {
                targetArea = brassRow;
                break;
            }
            case CardBadgeType.Percussion: {
                targetArea = percussionRow;
                break;
            }
            default: {
                Debug.LogError("badgeType error = " + newCard.GetComponent<CardDisplay>().cardInfo.badgeType);
                return;
            }
        }

        targetArea.GetComponent<RowArea>().AddNormalCard(newCard);
    }
}
