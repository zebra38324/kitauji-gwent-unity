using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class CompetitionScene : MonoBehaviour
{
    private string TAG = "CompetitionScene";

    public List<CompetitionPanelApi> panelViewList;

    public CompetitionGameRetView gameRetView;

    public CompetitionAwardingView awardingView;

    private enum PanelType
    {
        Current = 0, // 当前日程
        Standings, // 积分榜
        Schedule, // 赛程
        // News, // 新闻实况
    }

    private CompetitionContextModel context;

    private static readonly string[] backgroundImgNameList = new string[]
    {
        "KyotoPrefectureBack",
        "KansaiBack",
        "NationalBack"
    };

    private static bool needShownGameRetViewAtStart = false; // 启动场景时是否直接显示GameRetView，用于对局场景的切换

    // Start is called before the first frame update
    void Start()
    {
        Init();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Exit()
    {
        KLog.I(TAG, "Exit");
        SceneManager.LoadScene("MainMenuScene");
    }

    public void ClickCurrentButton()
    {
        KLog.I(TAG, "ClickCurrentButton");
        HideAllPanel();
        panelViewList[(int)PanelType.Current].Show(context);
    }

    public void ClickStandingsButton()
    {
        KLog.I(TAG, "ClickStandingsButton");
        HideAllPanel();
        panelViewList[(int)PanelType.Standings].Show(context);
    }

    public void ClickScheduleButton()
    {
        KLog.I(TAG, "ClickScheduleButton");
        HideAllPanel();
        panelViewList[(int)PanelType.Schedule].Show(context);
    }

    public void ClickPlayButton()
    {
        KLog.I(TAG, "ClickPlayButton");
        needShownGameRetViewAtStart = true;
        // 保存context
        KConfig.Instance.SaveCompetitionContext(context.GetRecord());
        // 切换到比赛场景
        GameConfig.Instance.Reset();
        var gameConfig = GameConfig.Instance;
        gameConfig.selfName = KConfig.Instance.playerName;
        var team = context.teamDict[context.playerName];
        gameConfig.enemyName = team.gameList[team.currentRound].enemyName;
        gameConfig.selfGroup = KConfig.Instance.deckCardGroup;
        gameConfig.isHost = true;
        gameConfig.isPVP = false;
        gameConfig.pveAIType = PlaySceneAI.AIType.L1K1;
        gameConfig.fromScene = "CompetitionScene";
        KHeartbeat.Instance.SendHeartbeat(KHeartbeat.UserStatus.PVE_GAMING);
        SceneManager.LoadScene("PlayScene");
    }

    public void ClickMockButton()
    {
        KLog.I(TAG, "ClickMockButton");
        gameRetView.Show(context, true);
    }

    public void ClickFinishLevelButton()
    {
        KLog.I(TAG, "ClickFinishLevelButton");
        awardingView.Show(context);
    }

    // 晋级
    public void ClickAwardingContinueButton()
    {
        KLog.I(TAG, "ClickAwardingContinueButton");
        context.StartNextLevel();
        awardingView.Hide();
    }

    // 重启竞赛。fromKyotoPrefecture：从京都赛重新开始，还是从当前等级重新开始
    public void ClickAwardingRestartButton(bool fromKyotoPrefecture)
    {
        KLog.I(TAG, "ClickAwardingRestartButton");
        // 此处必是没晋级
        // TODO
        awardingView.Hide();
    }

    private void Init()
    {
        var contextRecord = KConfig.Instance.GetCompetitionContext();
        if (contextRecord == null) {
            context = new CompetitionContextModel("北宇治高中");
        } else {
            context = new CompetitionContextModel(contextRecord);
        }

        KResources.Instance.Load<Sprite>(gameObject.GetComponent<Image>(), @"Image/texture/CompetitionScene/" + backgroundImgNameList[(int)context.currnetLevel]);

        HideAllPanel();
        panelViewList[(int)PanelType.Current].Show(context);

        if (needShownGameRetViewAtStart && GameConfig.Instance.normalFinish) {
            gameRetView.Show(context, false);
        }
        needShownGameRetViewAtStart = false;
    }

    private void HideAllPanel()
    {
        foreach (var panel in panelViewList) {
            panel.Hide();
        }
    }
}
