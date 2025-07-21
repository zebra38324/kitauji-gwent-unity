using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class AwardingResult : MonoBehaviour
{
    public TextMeshProUGUI title;

    public GameObject table;

    public GameObject rowPrefab;

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
        foreach (var team in context.teamDict.Values) {
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
    }
}
