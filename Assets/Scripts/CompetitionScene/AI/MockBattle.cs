using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

// 模拟两个牌组的对战
public class MockBattle
{
    public struct Side
    {
        public string name;
        // TODO: public List<int> deck;
        // TODO: ai type
    }

    public Side side1 { get; private set; }

    public Side side2 { get; private set; }

    private AIModelInterface aiModel1;

    private AIModelInterface aiModel2;

    private bool isAbort = false;

    public MockBattle(Side side1, Side side2)
    {
        this.side1 = side1;
        this.side2 = side2;

        // TODO: ai type
        PlaySceneModel playSceneModel1 = new PlaySceneModel(true,
            "AI1",
            "AI2",
            CardGroup.KumikoFirstYear,
            events => { }
        );
        PlaySceneModel playSceneModel2 = new PlaySceneModel(false,
            "AI2",
            "AI1",
            CardGroup.KumikoFirstYear,
            events => { }
        );
        playSceneModel1.battleModel.SendToEnemyFunc += playSceneModel2.battleModel.AddEnemyActionMsg;
        playSceneModel2.battleModel.SendToEnemyFunc += playSceneModel1.battleModel.AddEnemyActionMsg;

        aiModel1 = new AIModelL1(playSceneModel1);
        aiModel2 = new AIModelL1(playSceneModel2);
    }

    public void Release()
    {
        isAbort = true;
    }

    public void Start()
    {
        AICoroutine(aiModel1);
        AICoroutine(aiModel2);
    }

    public bool IsFinish()
    {
        return aiModel1.playSceneModel.wholeAreaModel.playTracker.isGameFinish;
    }

    // 获取比赛结果，注意，等结束后再调用
    public CompetitionGameInfoModel GetResult(string name)
    {
        var model = name == side1.name ? aiModel1.playSceneModel : aiModel2.playSceneModel;
        var gameInfoModel = new CompetitionGameInfoModel(model.wholeAreaModel.playTracker.selfPlayerInfo.name,
            model.wholeAreaModel.playTracker.enemyPlayerInfo.name);
        gameInfoModel.FinishGame(model.wholeAreaModel.playTracker.selfPlayerInfo.setScore,
            model.wholeAreaModel.playTracker.enemyPlayerInfo.setScore,
            model.wholeAreaModel.playTracker.isSelfWinner);
        return gameInfoModel;
    }

    private async void AICoroutine(AIModelInterface aiModel)
    {
        PlaySceneModel playSceneModel = aiModel.playSceneModel;
        while (!isAbort && playSceneModel.wholeAreaModel.gameState.curState != GameState.State.WAIT_INIT_HAND_CARD) {
            await UniTask.Delay(1);
        }
        aiModel.DoInitHandCard();
        while (!isAbort &&
               playSceneModel.wholeAreaModel.gameState.curState != GameState.State.WAIT_SELF_ACTION &&
               playSceneModel.wholeAreaModel.gameState.curState != GameState.State.WAIT_ENEMY_ACTION) {
            await UniTask.Delay(1);
        }
        while (!isAbort) {
            if (playSceneModel.wholeAreaModel.gameState.curState != GameState.State.WAIT_SELF_ACTION) {
                await UniTask.Delay(1);
                continue;
            }
            aiModel.DoPlayAction();
        }
    }
}
