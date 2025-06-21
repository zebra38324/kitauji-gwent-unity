using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

// 等级1 AI逻辑
class AIModelL1 : AIModelInterface
{
    private static string TAG = "AIModelL1";

    // 初始化并设置牌组
    public AIModelL1(PlaySceneModel playSceneModel_, List<int> deckList = null) : base(playSceneModel_, deckList)
    {
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
    public override void DoPlayAction()
    {
        int scoreDiff = AIModelCommon.GetScoreDiff(playSceneModel.wholeAreaModel);
        if (scoreDiff > 0 && playSceneModel.wholeAreaModel.gameState.enemyPass) {
            KLog.I(TAG, "DoPlayAction: scoreDiff: " + scoreDiff + ", enemyPass: true, will pass");
            playSceneModel.Pass();
            return;
        }
        var returnList = AIModelCommon.GetAllAction(playSceneModel.wholeAreaModel);
        var actionReturn = SelectActionReturn(returnList);
        if (returnList.Count == 0 || AIModelCommon.NeedPass(playSceneModel, returnList[0]) || actionReturn == null) {
            playSceneModel.Pass();
        } else {
            foreach (var action in actionReturn.actionList) {
                AIModelCommon.ApplyAction(playSceneModel, action);
            }
        }
    }

    private void SetDeckInfoIdList(List<int> deckList)
    {
        if (deckList == null) {
            CardGroup cardGroup = playSceneModel.wholeAreaModel.playTracker.selfPlayerInfo.cardGroup;
            var groupDeckList = AIDefaultDeck.deckConfigDic[cardGroup];
            var selectDeckIndex = new Random().Next(groupDeckList.Length);
            deckList = new List<int>(groupDeckList[selectDeckIndex]);
        }
        playSceneModel.SetBackupCardInfoIdList(deckList);
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
        int actionIndex = GetActionIndexLinearProbabilities(filterList.Count);
        KLog.I(TAG, "SelectAction: actionIndex = " + actionIndex + ", filterList.Count = " + filterList.Count + ", total count = " + allAction.Count);
        return allAction[actionIndex];
    }

    // 线性分布 获取操作index
    private int GetActionIndexLinearProbabilities(int num)
    {
        double total = num * (num + 1) / 2.0;
        List<double> probabilities = new List<double>();
        for (int i = num; i >= 1; i--) {
            probabilities.Add(i / total);
        }
        double randomValue = new Random().NextDouble(); // 生成一个0到1之间的随机数
        double cumulativeProbability = 0.0;
        for (int i = 0; i < num; i++) {
            cumulativeProbability += probabilities[i];
            if (randomValue <= cumulativeProbability) {
                return i;
            }
        }
        return 0;
    }
}
