using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Unity.VisualScripting;

// AI逻辑模块初始化策略，决定重抽手牌
class AIModelCommon
{
    private static string TAG = "AIModelCommon";
    public struct ActionReturn
    {
        public int scoreDiff;
        public int handCardReturn;
        public static bool operator >(ActionReturn r1, ActionReturn r2)
        {
            return r1.scoreDiff + r1.handCardReturn > r2.scoreDiff + r2.handCardReturn;
        }
        public static bool operator <(ActionReturn r1, ActionReturn r2)
        {
            return r1.scoreDiff + r1.handCardReturn < r2.scoreDiff + r2.handCardReturn;
        }
        public static ActionReturn operator +(ActionReturn r1, ActionReturn r2)
        {
            return new ActionReturn {
                scoreDiff = r1.scoreDiff + r2.scoreDiff,
                handCardReturn = r1.handCardReturn + r2.handCardReturn,
            };
        }
    }

    // 计算收益最高的操作，并通过List<ActionEvent>返回，仅包含battle msg
    // 收益包含分数差与手牌差
    public static ActionReturn GetMaxReturnAction(WholeAreaModel originModel, out List<ActionEvent> actionList)
    {
        KLog.I(TAG, "GetMaxReturnAction: start");
        originModel = originModel with {
            actionEventList = ImmutableList<ActionEvent>.Empty,
        };
        ActionReturn maxReturn = MockActionRecursive(originModel, out actionList);
        KLog.I(TAG, "GetMaxReturnAction: finish, actionList.Count: " + actionList.Count);
        return maxReturn;
    }

    // self - emeny Recursive
    public static int GetScoreDiff(WholeAreaModel model)
    {
        return model.selfSinglePlayerAreaModel.GetCurrentPower() - model.enemySinglePlayerAreaModel.GetCurrentPower();
    }

    public static void ApplyAction(PlaySceneModel model, ActionEvent actionEvent)
    {
        switch (actionEvent.args[0]) {
            case BattleModel.ActionType.ChooseCard: {
                int id = (int)actionEvent.args[1];
                CardModel card = model.wholeAreaModel.FindCard(id);
                if (card == null) {
                    KLog.E(TAG, "ApplyAction: ChooseCard: invalid id: " + id);
                    return;
                }
                model.ChooseCard(card);
                break;
            }
            case BattleModel.ActionType.ChooseHornUtilArea: {
                model.ChooseHornUtilArea((CardBadgeType)actionEvent.args[1]);
                break;
            }
            default: {
                KLog.E(TAG, "ApplyAction: invalid type = " + actionEvent.args[0]);
                break;
            }
        }
    }

    public static bool NeedPass(PlaySceneModel model, ActionReturn maxReturn)
    {
        int scoreDiff = AIModelCommon.GetScoreDiff(model.wholeAreaModel);
        bool ret = false;
        if (model.wholeAreaModel.playTracker.enemyPlayerInfo.setScore <= 1 &&
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

    private static ActionReturn MockActionRecursive(WholeAreaModel originModel, out List<ActionEvent> actionList)
    {
        originModel = originModel with {
            actionEventList = ImmutableList<ActionEvent>.Empty,
        };
        actionList = new List<ActionEvent>();
        ActionReturn maxReturn = new ActionReturn {
            scoreDiff = 0,
            handCardReturn = -20,
        };

        var enableChooseList = new List<CardModel>();
        switch (originModel.gameState.actionState) {
            case GameState.ActionState.None: {
                enableChooseList = originModel.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList.ToList();
                enableChooseList.AddRange(originModel.selfSinglePlayerAreaModel.handCardAreaModel.leaderCardListModel.cardList);
                break;
            }
            case GameState.ActionState.ATTACKING: {
                foreach (var row in originModel.enemySinglePlayerAreaModel.battleRowAreaList) {
                    foreach (var card in row.cardListModel.cardList) {
                        if (card.cardSelectType == CardSelectType.WithstandAttack) {
                            enableChooseList.Add(card);
                        }
                    }
                }
                break;
            }
            case GameState.ActionState.MEDICING: {
                foreach (var card in originModel.selfSinglePlayerAreaModel.discardAreaModel.cardListModel.cardList) {
                    if (card.cardSelectType == CardSelectType.PlayCard) {
                        enableChooseList.Add(card);
                    }
                }
                break;
            }
            case GameState.ActionState.DECOYING:
            case GameState.ActionState.MONAKAING: {
                foreach (var row in originModel.selfSinglePlayerAreaModel.battleRowAreaList) {
                    foreach (var card in row.cardListModel.cardList) {
                        if (card.cardSelectType == CardSelectType.DecoyWithdraw ||
                            card.cardSelectType == CardSelectType.Monaka) {
                            enableChooseList.Add(card);
                        }
                    }
                }
                break;
            }
        }
        
        if (originModel.gameState.actionState == GameState.ActionState.HORN_UTILING) {
            foreach (var row in originModel.selfSinglePlayerAreaModel.battleRowAreaList) {
                var newModel = originModel.ChooseHornUtilArea(row.rowType, true);
                var curReturn = GetReturn(newModel, originModel);
                if (curReturn > maxReturn) {
                    maxReturn = curReturn;
                    actionList = GetActionEventList(newModel);
                }
            }
        } else {
            foreach (var card in enableChooseList) {
                var newModel = originModel.ChooseCard(card, true);
                var curReturn = GetReturn(newModel, originModel);
                var newActionList = new List<ActionEvent>();
                if (originModel.gameState.actionState != GameState.ActionState.None) {
                    curReturn += MockActionRecursive(newModel, out newActionList);
                }
                if (curReturn > maxReturn) {
                    maxReturn = curReturn;
                    actionList = GetActionEventList(newModel);
                    actionList.AddRange(newActionList);
                }
            }
        }
        return maxReturn;
    }

    private static ActionReturn GetReturn(WholeAreaModel newModel, WholeAreaModel oldModel)
    {
        ActionReturn actionReturn = new ActionReturn {
            scoreDiff = GetScoreDiff(newModel) - GetScoreDiff(oldModel),
            handCardReturn = 0,
        };
        Random random = new Random();
        int handCardReturn = newModel.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList.Count -
            oldModel.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList.Count;
        handCardReturn += newModel.selfSinglePlayerAreaModel.handCardAreaModel.leaderCardListModel.cardList.Count -
            oldModel.selfSinglePlayerAreaModel.handCardAreaModel.leaderCardListModel.cardList.Count;
        handCardReturn *= random.Next(5, 15); // TODO: 手牌的收益计算
        actionReturn.handCardReturn = handCardReturn;
        return actionReturn;
    }

    private static List<ActionEvent> GetActionEventList(WholeAreaModel model)
    {
        var actionList = new List<ActionEvent>();
        actionList.AddRange(model.actionEventList);
        actionList.RemoveAll(actionEvent => actionEvent.type != ActionEvent.Type.BattleMsg);
        actionList.RemoveAll(actionEvent => {
            return (BattleModel.ActionType)actionEvent.args[0] != BattleModel.ActionType.ChooseCard &&
                   (BattleModel.ActionType)actionEvent.args[0] != BattleModel.ActionType.ChooseHornUtilArea;
        });
        return actionList;
    }
}
