using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System;

public class AIModelCommon
{
    private static string TAG = "AIModelCommon";
    public class ActionReturn
    {
        public int scoreDiff = 0;
        public int handCardReturn = 0;
        public List<ActionEvent> actionList = new List<ActionEvent>();
        public float benifit = 0; // L2使用
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

    private AIBase.AILevel level;

    public AIModelCommon(AIBase.AILevel level)
    {
        this.level = level;
    }

    // 获取所有可能的操作，并按照收益由高到低排序
    public List<ActionReturn> GetAllAction(WholeAreaModel originModel)
    {
        KLog.I(TAG, "GetAllAction: start");
        originModel = originModel with {
            actionEventList = ImmutableList<ActionEvent>.Empty,
        };
        var result = new List<ActionReturn>();
        bool originBlockLog = KLog.blockLog;
        KLog.blockLog = true;
        MockActionRecursive(originModel, new ActionReturn(), ref result);
        KLog.blockLog = originBlockLog;
        result.Sort((x, y) => y.Total() - x.Total());
        KLog.I(TAG, "GetAllAction: finish, result.Count: " + result.Count);
        return result;
    }

    // self - emeny Recursive
    public int GetScoreDiff(WholeAreaModel model)
    {
        return model.selfSinglePlayerAreaModel.GetCurrentPower() - model.enemySinglePlayerAreaModel.GetCurrentPower();
    }

    private void MockActionRecursive(WholeAreaModel originModel, ActionReturn lastReturn, ref List<ActionReturn> record)
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

    private ActionReturn GetReturn(WholeAreaModel newModel, WholeAreaModel oldModel)
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
