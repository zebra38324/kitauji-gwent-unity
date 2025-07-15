using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;

// 等级1 AI逻辑
public class AIModelL1 : AIModelInterface
{
    private static string TAG = "AIModelL1";

    // 初始化并设置牌组
    public AIModelL1(PlaySceneModel playSceneModel_, List<int> deckList = null, AIBase.AIMode aiMode_ = AIBase.AIMode.Normal) : base(playSceneModel_, aiMode_)
    {
        aiModelCommon = new AIModelCommon(AIBase.AILevel.L1);
        // 设置牌组
        SetDeckInfoIdList(deckList);
    }

    // 初始化手牌，并完成重抽手牌操作
    public override void DoInitHandCard()
    {
        playSceneModel.DrawInitHandCard();
        aiModelInit.ChooseReDrawInitHandCard();
        playSceneModel.ReDrawInitHandCard();
    }

    // WAIT_SELF_ACTION时的操作
    public override async UniTask DoPlayAction()
    {
        int scoreDiff = aiModelCommon.GetScoreDiff(playSceneModel.wholeAreaModel);
        if (scoreDiff > 0 && playSceneModel.wholeAreaModel.gameState.enemyPass) {
            KLog.I(TAG, "DoPlayAction: scoreDiff: " + scoreDiff + ", enemyPass: true, will pass");
            playSceneModel.Pass();
            return;
        }
        var returnList = aiModelCommon.GetAllAction(playSceneModel.wholeAreaModel);
        var actionReturn = SelectActionReturn(returnList);
        if (returnList.Count == 0 || NeedPass(playSceneModel, returnList[0]) || actionReturn == null) {
            playSceneModel.Pass();
        } else {
            foreach (var action in actionReturn.actionList) {
                ApplyAction(playSceneModel, action);
            }
        }
        await UniTask.Yield();
    }

    // 选择操作
    private AIModelCommon.ActionReturn SelectActionReturn(List<AIModelCommon.ActionReturn> allAction)
    {
        var filterList = new List<AIModelCommon.ActionReturn>();
        foreach (var action in allAction) {
            if (action.Total() < 0) {
                break;
            }
            filterList.Add(action);
        }
        if (filterList.Count == 0) {
            if (allAction.Count > 3) {
                KLog.I(TAG, "SelectAction: use first action, allAction.Count = " + allAction.Count);
                return allAction[0]; // 还有后续操作，暂时负收益试一试
            } else {
                KLog.I(TAG, "SelectAction: no valid action found, allAction.Count = " + allAction.Count);
                return null;
            }
        }
        int actionIndex = AIBase.GetIndexLinearProbabilities(filterList.Count);
        KLog.I(TAG, "SelectAction: actionIndex = " + actionIndex + ", filterList.Count = " + filterList.Count + ", total count = " + allAction.Count);
        return allAction[actionIndex];
    }

    private bool NeedPass(PlaySceneModel model, AIModelCommon.ActionReturn maxReturn)
    {
        int scoreDiff = aiModelCommon.GetScoreDiff(model.wholeAreaModel);
        bool ret = false;
        if (model.wholeAreaModel.playTracker.enemyPlayerInfo.setScore == 0 &&
            ((scoreDiff < -10 && maxReturn.scoreDiff < 3) ||
             scoreDiff > 20 && maxReturn.scoreDiff > 5)) {
            ret = true;
        }
        KLog.I(TAG, "NeedPass: scoreDiff: " + scoreDiff +
            ", maxReturn.scoreDiff: " + maxReturn.scoreDiff +
            ", maxReturn.handCardReturn: " + maxReturn.handCardReturn +
            ", ret = " + ret);
        return ret;
    }
}
