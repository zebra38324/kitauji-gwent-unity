using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CompetitionScheduleView : CompetitionPanelApi
{
    private string TAG = "CompetitionScheduleView";

    public TMP_Dropdown roundSelect;

    public CompetitionScheduleTextView scheduleTextView;

    private CompetitionContextModel context;

    private void Awake()
    {
        InitRoundSelect();
    }

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
        KLog.I(TAG, "Show");
        this.context = context;
        if (gameObject.activeSelf) {
            Hide();
        }
        gameObject.SetActive(true);
        roundSelect.value = context.teamDict[context.playerName].currentRound;
        scheduleTextView.Show(context, roundSelect.value);
    }

    public override void Hide()
    {
        KLog.I(TAG, "Hide");
        if (!gameObject.activeSelf) {
            return;
        }
        gameObject.SetActive(false);
        scheduleTextView.DestroyCellList();
    }

    public void OnRoundSelectChange()
    {
        KLog.I(TAG, $"OnRoundSelectChange: round = {roundSelect.value}");
        scheduleTextView.Show(context, roundSelect.value);
    }

    private void InitRoundSelect()
    {
        KLog.I(TAG, "InitRoundSelect");
        roundSelect.ClearOptions();
        var options = new List<string>();
        for (int i = 0; i < CompetitionContextModel.TEAM_NUM - 1; i++) {
            options.Add($"第{i + 1}轮");
        }
        roundSelect.AddOptions(options);
        roundSelect.value = 0;
        roundSelect.RefreshShownValue();
    }
}
