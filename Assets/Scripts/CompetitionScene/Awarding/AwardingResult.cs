using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;

public class AwardingResult : MonoBehaviour
{
    public TextMeshProUGUI title;

    public GameObject table;

    public GameObject rowPrefab;

    public TextMeshProUGUI tip;

    private CompetitionContextModel context;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Show(CompetitionContextModel context_)
    {
        context = context_;
        title.text = $"【{CompetitionBase.LEVEL_TEXT[(int)context.currnetLevel]}】竞赛结果";

        var firstRow = Instantiate(rowPrefab, table.gameObject.transform);
        if (context.currnetLevel == CompetitionBase.Level.National) {
            firstRow.GetComponent<AwardingResultTableRow>().HidePromote();
        }
        firstRow.GetComponent<AwardingResultTableRow>().UpdateText("序号",
            "学校",
            "奖项",
            "代表");

        int index = 0;
        var teamList = new List<CompetitionTeamInfoModel>(context.teamDict.Values).OrderBy(x => System.Guid.NewGuid()).ToList();
        foreach (var team in teamList) {
            string prefix = "";
            string suffix = "";
            if (team.name == context.playerName) {
                prefix = "<color=green>";
                suffix = "</color>";
            }
            var row = Instantiate(rowPrefab, table.gameObject.transform);
            row.GetComponent<AwardingResultTableRow>().UpdateText($"{prefix}{index + 1}{suffix}",
                $"{prefix}{team.name}{suffix}",
                $"{prefix}{CompetitionBase.PRIZE_TEXT[(int)team.prize].Substring(0, 1)}{suffix}",
                team.prize == CompetitionBase.Prize.GoldPromote ? "〇" : "");
            index += 1;
        }

        var playerTeam = context.teamDict[context.playerName];
        string levelText = CompetitionBase.LEVEL_TEXT[(int)context.currnetLevel];
        string prizeText = CompetitionBase.PRIZE_TEXT[(int)playerTeam.prize];
        if (context.currnetLevel == CompetitionBase.Level.National) {
            tip.text = $"恭喜获得【{levelText}】{prizeText}！";
        } else if (playerTeam.prize == CompetitionBase.Prize.GoldPromote) {
            string nextLevelText = CompetitionBase.LEVEL_TEXT[(int)(context.currnetLevel + 1)];
            prizeText = CompetitionBase.PRIZE_TEXT[(int)CompetitionBase.Prize.Gold];
            tip.text = $"恭喜获得【{levelText}】{prizeText}！将晋级{nextLevelText}！";
        } else {
            tip.text = $"获得【{levelText}】{prizeText}，很遗憾未能晋级";
        }
    }
}
