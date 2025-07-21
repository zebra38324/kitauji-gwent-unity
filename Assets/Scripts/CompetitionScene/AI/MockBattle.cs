using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;

// 模拟两个牌组的对战
public class MockBattle
{
    public class Side
    {
        public bool isPlayer = false;
        public string name = "";
        public List<int> deck = null;
        public CardGroup group = CardGroup.KumikoFirstYear;
        public AIBase.AILevel level = AIBase.AILevel.L1;
    }

    public Side side1 { get; private set; }

    public Side side2 { get; private set; }

    private AIModelInterface aiModel1;

    private AIModelInterface aiModel2;

    private bool isAbort = false;

    public MockBattle(Side side1_, Side side2_)
    {
        side1 = FillSide(side1_);
        side2 = FillSide(side2_);

        PlaySceneModel playSceneModel1 = new PlaySceneModel(true,
            side1.name,
            side2.name,
            side1.group,
            events => { }
        );
        PlaySceneModel playSceneModel2 = new PlaySceneModel(false,
            side2.name,
            side1.name,
            side2.group,
            events => { }
        );
        playSceneModel1.battleModel.SendToEnemyFunc += playSceneModel2.battleModel.AddEnemyActionMsg;
        playSceneModel2.battleModel.SendToEnemyFunc += playSceneModel1.battleModel.AddEnemyActionMsg;

        aiModel1 = GenAIModel(playSceneModel1, side1);
        aiModel2 = GenAIModel(playSceneModel2, side2);
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

    public static AIBase.AILevel GetAILevel(string name)
    {
        var aiTeamNameList = CompetitionContextModel.AI_TEAM_NAME_LIST;
        AIBase.AILevel level;
        if (aiTeamNameList[(int)CompetitionBase.Level.KyotoPrefecture].Contains(name)) {
            if (name == "龙圣学园高中") {
                level = AIBase.AILevel.L3;
            } else if (name == "立华高中" || name == "洛秋高中") {
                level = AIBase.AILevel.L2;
            } else {
                level = AIBase.AILevel.L1;
            }
        } else if (aiTeamNameList[(int)CompetitionBase.Level.Kansai].Contains(name)) {
            if (name == "大阪东照高中" || name == "明静工科高中" || name == "秀塔大学附属高中") {
                level = AIBase.AILevel.L3;
            } else {
                level = AIBase.AILevel.L2;
            }
        } else {
            level = AIBase.AILevel.L3;
        }
        return level;
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
            await aiModel.DoPlayAction();
        }
    }

    // 填充side
    private Side FillSide(Side origin)
    {
        var result = origin;
        if (origin.isPlayer) {
            result.level = AIBase.AILevel.L1;
            result.group = KConfig.Instance.deckCardGroup;
            result.deck = KConfig.Instance.GetDeckInfoIdList(KConfig.Instance.deckCardGroup);
            return result;
        }
        result.level = GetAILevel(result.name);
        result.group = (CardGroup)(new Random().Next(0, (int)CardGroup.Neutral));
        result.deck = new List<int>(AIDefaultDeck.deckConfigDic[result.group][(int)result.level]);
        return result;
    }

    private AIModelInterface GenAIModel(PlaySceneModel playSceneModel, Side side)
    {
        if (side.level == AIBase.AILevel.L1) {
            return new AIModelL1(playSceneModel, side.deck, AIBase.AIMode.Quick);
        } else if (side.level == AIBase.AILevel.L2 || side.level == AIBase.AILevel.L3) {
            return new AIModelL2(playSceneModel, side.deck, AIBase.AIMode.Quick);
        }
        return null;
    }
}
