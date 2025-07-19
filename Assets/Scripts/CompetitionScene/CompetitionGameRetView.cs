using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class CompetitionGameRetView : MonoBehaviour
{
    private string TAG = "CompetitionGameRetView";

    public TextMeshProUGUI tip;

    public GameObject closeButton;

    public CompetitionScheduleTextView scheduleTextView;

    private CompetitionContextModel context;

    private bool currentRoundFinish = false;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Show(CompetitionContextModel context, bool mockAll)
    {
        KLog.I(TAG, "Show");
        currentRoundFinish = false;
        tip.gameObject.SetActive(false);
        closeButton.gameObject.SetActive(false);
        this.context = context;
        gameObject.SetActive(true);
        int currentRound = context.teamDict[context.playerName].currentRound;
        StartCoroutine(UpdateTip(currentRound));
        StartCoroutine(UpdateScheduleText(currentRound));
        StartCoroutine(MockGame(currentRound, mockAll));
    }

    public void ClickCloseButton()
    {
        KLog.I(TAG, "ClickCloseButton");
        KConfig.Instance.SaveCompetitionContext(context.GetRecord());
        gameObject.SetActive(false);
    }

    private IEnumerator UpdateTip(int currentRound)
    {
        string defaultTip = "本轮比赛进行中";
        string waitingSuffix = "。";
        long lastUpdateTs = KTime.CurrentMill();
        int lastCount = 0;
        tip.text = defaultTip;
        tip.gameObject.SetActive(true);
        while (!currentRoundFinish) {
            if (KTime.CurrentMill() - lastUpdateTs < 500) {
                yield return null;
                continue;
            }
            lastUpdateTs = KTime.CurrentMill();
            lastCount = (lastCount + 1) % 4;
            string newTip = defaultTip;
            for (int i = 0; i < lastCount; i++) {
                newTip += waitingSuffix;
            }
            tip.text = newTip;
        }
        tip.gameObject.SetActive(false);
        closeButton.gameObject.SetActive(true);
    }

    private IEnumerator UpdateScheduleText(int currentRound)
    {
        while (gameObject.activeSelf) {
            scheduleTextView.Show(context, currentRound);
            yield return null;
        }
    }

    private IEnumerator MockGame(int currentRound, bool mockAll)
    {
        yield return null; // 加快按钮响应速度
        foreach (var team in context.teamDict.Values) {
            if (team.currentRound > currentRound) {
                continue; // 该队伍已经完成了当前轮次的比赛
            }
            var game = team.gameList[currentRound];
            CompetitionGameInfoModel selfRet = null;
            if (!mockAll && (game.selfName == context.playerName || game.enemyName == context.playerName)) {
                // 不mock玩家的对局
                selfRet = new CompetitionGameInfoModel(game.selfName, game.enemyName);
                var gameConfig = GameConfig.Instance;
                if (game.selfName == context.playerName) {
                    selfRet.FinishGame(gameConfig.selfScore, gameConfig.enemyScore, gameConfig.isSelfWin);
                } else {
                    selfRet.FinishGame(gameConfig.enemyScore, gameConfig.selfScore, !gameConfig.isSelfWin);
                }
            } else {
                var mockBattle = new MockBattle(new MockBattle.Side { name = game.selfName }, new MockBattle.Side { name = game.enemyName });
                mockBattle.Start();
                while (!mockBattle.IsFinish()) {
                    yield return null;
                }
                selfRet = mockBattle.GetResult(game.selfName);
            }
            team.FinishGame(selfRet.selfScore, selfRet.enemyScore, selfRet.selfWin);
            context.teamDict[game.enemyName].FinishGame(selfRet.enemyScore, selfRet.selfScore, !selfRet.selfWin);
        }
        currentRoundFinish = true;
    }
}
