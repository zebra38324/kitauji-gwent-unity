using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CompetitionScheduleTextView : MonoBehaviour
{
    //private string TAG = "CompetitionScheduleTextView";

    public GameObject cellPrefab;

    private List<GameObject> cellList = new List<GameObject>();

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Show(CompetitionContextModel context, int round)
    {
        //KLog.I(TAG, $"Show: round = {round}");
        DestroyCellList();
        var gameList = context.GetGameInfoList(round);
        foreach (var game in gameList) {
            var cell = Instantiate(cellPrefab, gameObject.transform);
            string selfName = $"{game.selfName}";
            string enemyName = $"{game.enemyName}";
            if (game.hasFinished) {
                if (game.selfWin) {
                    selfName = $"{selfName}（胜）";
                } else {
                    enemyName = $"{enemyName}（胜）";
                }
                cell.GetComponent<CompetitionScheduleCell>().scoreLeft.text = $"{game.selfScore}";
                cell.GetComponent<CompetitionScheduleCell>().scoreRight.text = $"{game.enemyScore}";
            }
            if (game.selfName == context.playerName) {
                selfName = $"<color=green>{selfName}</color>";
            }
            if (game.enemyName == context.playerName) {
                enemyName = $"<color=green>{enemyName}</color>";
            }
            cell.GetComponent<CompetitionScheduleCell>().nameLeft.text = $"{selfName}";
            cell.GetComponent<CompetitionScheduleCell>().nameRight.text = $"{enemyName}";
            cellList.Add(cell);
        }
    }

    public void DestroyCellList()
    {
        foreach (var cell in cellList) {
            Destroy(cell);
        }
        cellList.Clear();
    }
}
