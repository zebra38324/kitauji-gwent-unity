using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CompetitionStandingsView : CompetitionPanelApi
{
    //private string TAG = "CompetitionStandingsView";

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

    public override void Show(CompetitionContextModel context)
    {
        //KLog.I(TAG, "Show");
        if (gameObject.activeSelf) {
            Hide();
        }
        var rankTeamList = context.GetRankTeamList();
        gameObject.SetActive(true);
        cellList = new List<GameObject>();
        for (int i = 0; i < rankTeamList.Count; i++) {
            var team = rankTeamList[i];
            string prefix = "";
            string suffix = "";
            if (team.name == context.playerName) {
                prefix = "<color=green>";
                suffix = "</color>";
            }
            var cell = Instantiate(cellPrefab, gameObject.transform);
            cell.GetComponent<TextMeshProUGUI>().text = $"{prefix}{i + 1}{suffix}";
            cellList.Add(cell);
            cell = Instantiate(cellPrefab, gameObject.transform);
            cell.GetComponent<TextMeshProUGUI>().text = $"{prefix}{team.name}{suffix}";
            cellList.Add(cell);
            cell = Instantiate(cellPrefab, gameObject.transform);
            cell.GetComponent<TextMeshProUGUI>().text = $"{prefix}{team.GetWinCount()}-{team.GetLoseCount()}{suffix}";
            cellList.Add(cell);
            cell = Instantiate(cellPrefab, gameObject.transform);
            cell.GetComponent<TextMeshProUGUI>().text = $"{prefix}{team.GetMinorScoreDiff().ToString("+0;-0;0")}{suffix}";
            cellList.Add(cell);
            cell = Instantiate(cellPrefab, gameObject.transform);
            cell.GetComponent<TextMeshProUGUI>().text = $"{prefix}{CompetitionBase.PRIZE_TEXT[(int)CompetitionBase.GetPrize(i, context.currnetLevel)]}{suffix}";
            cellList.Add(cell);
        }
    }

    public override void Hide()
    {
        //KLog.I(TAG, "Hide");
        if (!gameObject.activeSelf) {
            return;
        }
        gameObject.SetActive(false);
        foreach (var cell in cellList) {
            Destroy(cell);
        }
        cellList = new List<GameObject>();
    }
}
