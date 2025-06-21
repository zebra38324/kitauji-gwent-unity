using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CompetitionCurrentView : CompetitionPanelApi
{
    private string TAG = "CompetitionCurrentView";

    public GameObject levelOngoing;

    public GameObject levelFinish;

    public CompetitionScheduleTextView scheduleTextView;

    public TextMeshProUGUI roundTip;

    public CompetitionStandingsView standingsView;

    public TextMeshProUGUI finishLevelButtonText;

    private CompetitionContextModel context;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        UpdateUI();
    }

    public override void Show(CompetitionContextModel context)
    {
        KLog.I(TAG, "Show");
        this.context = context;
        if (gameObject.activeSelf) {
            Hide();
        }
        gameObject.SetActive(true);
        UpdateUI();
    }

    public override void Hide()
    {
        KLog.I(TAG, "Hide");
        if (!gameObject.activeSelf) {
            return;
        }
        gameObject.SetActive(false);
    }

    private void UpdateUI()
    {
        if (context == null) {
            return;
        }
        if (context.IsCurrentLevelFinish()) {
            levelOngoing.SetActive(false);
            levelFinish.SetActive(true);
            standingsView.Show(context);
            finishLevelButtonText.text = $"进行【{CompetitionBase.LEVEL_TEXT[(int)context.currnetLevel]}】颁奖仪式";
        } else {
            levelOngoing.SetActive(true);
            levelFinish.SetActive(false);
            int round = context.teamDict[context.playerName].currentRound;
            if (round < CompetitionContextModel.TEAM_NUM - 1) {
                scheduleTextView.Show(context, round);
                roundTip.text = $"当前比赛进度\n【{CompetitionBase.LEVEL_TEXT[(int)context.currnetLevel]}】 第 {round + 1}/{CompetitionContextModel.TEAM_NUM - 1} 轮";
            }
        }
    }
}
