using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using System.Linq;
using UnityEngine.TestTools;
using System.Collections;
using Cysharp.Threading.Tasks;

public class AIMockTest
{
    private static string TAG = "AIMockTestLog";

    private int ai1WinCount = 0;

    private IEnumerator AIMock()
    {
        KLog.blockLog = true;
        long startTs = KTime.CurrentMill();
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

        AIModelInterface aiModel1 = new AIModelL2(playSceneModel1, new List<int>(AIDefaultDeck.deckConfigDic[CardGroup.KumikoFirstYear][1]), AIBase.AIMode.Test);
        AIModelInterface aiModel2 = new AIModelL1(playSceneModel2, new List<int>(AIDefaultDeck.deckConfigDic[CardGroup.KumikoFirstYear][1]), AIBase.AIMode.Test);

        while (!playSceneModel1.wholeAreaModel.playTracker.isGameFinish) {
            if (KTime.CurrentMill() - startTs > 60000) {
                KLog.blockLog = false;
                KLog.E(TAG, "Timeout!!!");
                yield break;
            }
            if (playSceneModel1.wholeAreaModel.gameState.curState == GameState.State.WAIT_BACKUP_INFO ||
                playSceneModel2.wholeAreaModel.gameState.curState == GameState.State.WAIT_BACKUP_INFO) {
                yield return null;
                continue;
            }
            if (playSceneModel1.wholeAreaModel.gameState.curState == GameState.State.WAIT_INIT_HAND_CARD &&
                !playSceneModel1.wholeAreaModel.selfSinglePlayerAreaModel.handCardAreaModel.hasReDrawInitHandCard) {
                aiModel1.DoInitHandCard();
            }
            if (playSceneModel2.wholeAreaModel.gameState.curState == GameState.State.WAIT_INIT_HAND_CARD &&
                !playSceneModel2.wholeAreaModel.selfSinglePlayerAreaModel.handCardAreaModel.hasReDrawInitHandCard) {
                aiModel2.DoInitHandCard();
            }
            if (playSceneModel1.wholeAreaModel.gameState.curState == GameState.State.WAIT_INIT_HAND_CARD ||
                playSceneModel1.wholeAreaModel.gameState.curState == GameState.State.WAIT_START ||
                playSceneModel2.wholeAreaModel.gameState.curState == GameState.State.WAIT_INIT_HAND_CARD ||
                playSceneModel2.wholeAreaModel.gameState.curState == GameState.State.WAIT_START) {
                yield return null;
                continue;
            }
            if (playSceneModel1.wholeAreaModel.gameState.curState == GameState.State.WAIT_SELF_ACTION) {
                var task = aiModel1.DoPlayAction();
                while (task.Status == UniTaskStatus.Pending) {
                    yield return null;
                }
            } else if (playSceneModel2.wholeAreaModel.gameState.curState == GameState.State.WAIT_SELF_ACTION) {
                var task = aiModel2.DoPlayAction();
                while (task.Status == UniTaskStatus.Pending) {
                    yield return null;
                }
            } else {
                yield return null;
            }
        }
        var tracker1 = playSceneModel1.wholeAreaModel.playTracker;
        string winner = tracker1.isSelfWinner ? tracker1.selfPlayerInfo.name : tracker1.enemyPlayerInfo.name;
        if (tracker1.isSelfWinner) {
            ai1WinCount++;
        }
        KLog.blockLog = false;
        KLog.I(TAG, $"finish, time cost {KTime.CurrentMill() - startTs} ms, winner: {winner}, set {tracker1.selfPlayerInfo.setScore} : {tracker1.enemyPlayerInfo.setScore}");
    }

    [UnityTest]
    public IEnumerator MockTest()
    {
        KLog.redirectToFile = true;
        int total = 1;
        for (int i = 0; i < total; i++) {
            //yield return AIMock();
        }
        KLog.I(TAG, $"total: {total}, AI1 win count: {ai1WinCount}, rate {(float)ai1WinCount / total}");
        KLog.redirectToFile = false;
        yield break;
    }
}
