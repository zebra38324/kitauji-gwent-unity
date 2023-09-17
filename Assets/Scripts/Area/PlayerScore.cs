using TMPro;
using UnityEngine;

public class PlayerScore : MonoBehaviour
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
}
