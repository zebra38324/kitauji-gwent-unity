using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CompetitionAwardingView : MonoBehaviour
{
    private string TAG = "CompetitionAwardingView";

    public CompetitionStandingsView standingsView;

    public TextMeshProUGUI tip;

    public Button continueButton;

    private CompetitionContextModel context;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void Show(CompetitionContextModel context)
    {
        KLog.I(TAG, "Show");
        this.context = context;
        gameObject.SetActive(true);
        standingsView.Show(context);
        context.FinishCurrentLevel();
        var playerTeam = context.teamDict[context.playerName];
        string levelText = CompetitionBase.LEVEL_TEXT[(int)context.currnetLevel];
        string prizeText = CompetitionBase.PRIZE_TEXT[(int)playerTeam.prize];
        bool showContinueButton = false;
        if (context.currnetLevel == CompetitionBase.Level.National) {
            tip.text = $"恭喜获得【{levelText}】{prizeText}！";
        } else if (playerTeam.prize == CompetitionBase.Prize.GoldPromote) {
            string nextLevelText = CompetitionBase.LEVEL_TEXT[(int)(context.currnetLevel + 1)];
            prizeText = CompetitionBase.PRIZE_TEXT[(int)CompetitionBase.Prize.Gold];
            tip.text = $"恭喜获得【{levelText}】{prizeText}！将晋级{nextLevelText}！";
            showContinueButton = true;
        } else {
            tip.text = $"获得【{levelText}】{prizeText}，很遗憾未能晋级";
        }
        continueButton.gameObject.SetActive(showContinueButton);
    }

    public void Hide()
    {
        KLog.I(TAG, "Hide");
        gameObject.SetActive(false);
        KConfig.Instance.SaveCompetitionContext(context.GetRecord());
    }
}
