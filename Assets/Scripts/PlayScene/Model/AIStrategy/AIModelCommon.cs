using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Unity.VisualScripting;

// AI逻辑模块初始化策略，决定重抽手牌
class AIModelCommon
{
    private static string TAG = "AIModelCommon";
    public class ActionReturn
    {
        public int scoreDiff = 0;
        public int handCardReturn = 0;
        public List<ActionEvent> actionList = new List<ActionEvent>();
        public static ActionReturn operator +(ActionReturn r1, ActionReturn r2)
        {
            return new ActionReturn {
                scoreDiff = r1.scoreDiff + r2.scoreDiff,
                handCardReturn = r1.handCardReturn + r2.handCardReturn,
                actionList = r1.actionList.Concat(r2.actionList).ToList(),
            };
        }
        public int Total()
        {
            return scoreDiff + handCardReturn;
        }
    }

    // 获取所有可能的操作，并按照收益由高到低排序
    public static List<ActionReturn> GetAllAction(WholeAreaModel originModel)
    {
        KLog.I(TAG, "GetAllAction: start");
        originModel = originModel with {
            actionEventList = ImmutableList<ActionEvent>.Empty,
        };
        var result = new List<ActionReturn>();
        MockActionRecursive(originModel, new ActionReturn(), ref result);
        result.Sort((x, y) => y.Total() - x.Total());
        KLog.I(TAG, "GetAllAction: finish, result.Count: " + result.Count);
        return result;
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
        int scoreDiff = GetScoreDiff(model.wholeAreaModel);
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

    private static void MockActionRecursive(WholeAreaModel originModel, ActionReturn lastReturn, ref List<ActionReturn> record)
    {
        originModel = originModel with {
            actionEventList = ImmutableList<ActionEvent>.Empty,
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
                var curReturn = lastReturn + GetReturn(newModel, originModel);
                // HORN_UTILING不可能再继续了
                record.Add(curReturn);
            }
        } else {
            foreach (var card in enableChooseList) {
                var newModel = originModel.ChooseCard(card, true);
                var curReturn = lastReturn + GetReturn(newModel, originModel);
                if (newModel.gameState.actionState != GameState.ActionState.None) {
                    MockActionRecursive(newModel, curReturn, ref record);
                } else {
                    record.Add(curReturn);
                }
            }
        }
    }

    private static ActionReturn GetReturn(WholeAreaModel newModel, WholeAreaModel oldModel)
    {
        ActionReturn actionReturn = new ActionReturn {
            scoreDiff = GetScoreDiff(newModel) - GetScoreDiff(oldModel),
            handCardReturn = 0,
            actionList = GetActionEventList(newModel),
        };
        Random random = new Random();
        int handCardReturn = newModel.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList.Count -
            oldModel.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList.Count;
        handCardReturn += newModel.selfSinglePlayerAreaModel.handCardAreaModel.leaderCardListModel.cardList.Count -
            oldModel.selfSinglePlayerAreaModel.handCardAreaModel.leaderCardListModel.cardList.Count;
        int HAND_CAR_DIFF_BASE = -1; // 正常情况，手牌减少1
        handCardReturn -= HAND_CAR_DIFF_BASE; // 减去基础手牌差
        handCardReturn = Math.Abs(handCardReturn); // 手牌数只考虑正向收益
        handCardReturn *= random.Next(10, 20); // TODO: 手牌的收益计算
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
