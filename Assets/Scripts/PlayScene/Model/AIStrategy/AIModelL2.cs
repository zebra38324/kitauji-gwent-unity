using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;

// 等级2 AI逻辑
public class AIModelL2 : AIModelInterface
{
    private static string TAG = "AIModelL2";

    // 初始化并设置牌组
    public AIModelL2(PlaySceneModel playSceneModel_, List<int> deckList = null, AIBase.AIMode aiMode_ = AIBase.AIMode.Normal) : base(playSceneModel_, aiMode_)
    {
        aiModelCommon = new AIModelCommon(AIBase.AILevel.L2);
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
        var allAction = aiModelCommon.GetAllAction(playSceneModel.wholeAreaModel);
        AIModelCommon.ActionReturn passAction = new AIModelCommon.ActionReturn {
            scoreDiff = 0,
            handCardReturn = 0,
            actionList = new List<ActionEvent> { new ActionEvent(ActionEvent.Type.BattleMsg, BattleModel.ActionType.Pass) },
            benifit = 0f
        };
        allAction.Add(passAction);
        var mockModelList = MockEnemyCard();
        long timeout = aiMode == AIBase.AIMode.Normal ? 8000 : 500;
        foreach (var action in allAction) {
            float sum = 0;
            foreach (var model in mockModelList) {
                // 为每个mock model计算操作的胜率
                bool originBlockLog = KLog.blockLog;
                KLog.blockLog = true;
                var mockModel = MockAction(model, action.actionList);
                long singleTimeout = timeout / allAction.Count / mockModelList.Count;
                long startTs = KTime.CurrentMill();
                sum += await CalcModelBenifit(playSceneModel.wholeAreaModel, mockModel, singleTimeout);
                KLog.blockLog = originBlockLog;
                KLog.I(TAG, $"time cost {KTime.CurrentMill() - startTs} ms, singleTimeout = {singleTimeout} ms");
            }
            action.benifit = sum / mockModelList.Count;
            KLog.I(TAG, $"CalcModelBenifit: {action.benifit}, action: {(BattleModel.ActionType)(action.actionList[0].args[0])}");
        }
        allAction.Sort((x, y) => y.benifit.CompareTo(x.benifit));
        foreach (var action in allAction[0].actionList) {
            ApplyAction(playSceneModel, action);
        }
    }

    // 模拟对方手牌及备选牌
    private List<WholeAreaModel> MockEnemyCard()
    {
        CardGroup enemyGroup = playSceneModel.wholeAreaModel.playTracker.enemyPlayerInfo.cardGroup;
        List<CardInfo> allInfoList = CardGenerator.GetGroupInfoList(enemyGroup);
        allInfoList.AddRange(CardGenerator.GetGroupInfoList(CardGroup.Neutral));
        // 移除不可能是手牌的部分
        // TODO: 部分牌可能允许重复
        var enemyModel = playSceneModel.wholeAreaModel.enemySinglePlayerAreaModel;
        foreach (var card in enemyModel.discardAreaModel.cardListModel.cardList) {
            allInfoList.RemoveAll(x => x.infoId == card.cardInfo.infoId);
        }
        foreach (var row in enemyModel.battleRowAreaList) {
            foreach (var card in row.cardListModel.cardList) {
                allInfoList.RemoveAll(x => x.infoId == card.cardInfo.infoId);
            }
            foreach (var card in row.hornCardListModel.cardList) {
                allInfoList.RemoveAll(x => x.infoId == card.cardInfo.infoId);
            }
        }

        // 挑选价值高的牌
        var focusInfoList = new List<CardInfo>();
        foreach (var cardInfo in allInfoList) {
            if (cardInfo.ability != CardAbility.None || cardInfo.originPower > 5) {
                focusInfoList.Add(cardInfo);
            }
        }

        // 随机猜测对方的可能手牌
        int handCardNum = enemyModel.handCardAreaModel.handCardListModel.cardList.Count;
        int backupCardNum = enemyModel.handCardAreaModel.backupCardList.Count;
        var ran = new Random();
        var mockModelList = new List<WholeAreaModel>();
        for (int i = 0; i < 3; i++) { // TODO: 可调整猜测数量
            float utilRate = 0.2f;
            var roleList = focusInfoList.Where(x => x.cardType == CardType.Normal || x.cardType == CardType.Hero).ToList();
            var utilList = focusInfoList.Where(x => x.cardType == CardType.Util).ToList();
            int handUtilCount = (int)(handCardNum * utilRate);
            int handRoleCount = handCardNum - handUtilCount;
            var mockHandCardList = roleList
                .OrderBy(x => ran.Next())
                .Take(Math.Min(handRoleCount, roleList.Count))
                .ToList();
            mockHandCardList.AddRange(utilList
                .OrderBy(x => ran.Next())
                .Take(Math.Min(handUtilCount, utilList.Count))
                .ToList());
            foreach (var cardInfo in mockHandCardList) {
                roleList.RemoveAll(x => x.infoId == cardInfo.infoId);
                utilList.RemoveAll(x => x.infoId == cardInfo.infoId);
            }
            int backUtilCount = (int)(backupCardNum * utilRate);
            int backRoleCount = backupCardNum - handUtilCount;
            var mockBackupCardList = roleList
                .OrderBy(x => ran.Next())
                .Take(Math.Min(backRoleCount, roleList.Count))
                .ToList();
            mockBackupCardList.AddRange(utilList
                .OrderBy(x => ran.Next())
                .Take(Math.Min(backUtilCount, utilList.Count))
                .ToList());
            // 此处的100与200仅为了id不重复
            var mockModel = playSceneModel.wholeAreaModel.ReplaceHandAndBackupCard(mockHandCardList.Select(x => x.infoId).ToList(),
                Enumerable.Range(100, mockHandCardList.Count).ToList(),
                mockBackupCardList.Select(x => x.infoId).ToList(),
                Enumerable.Range(200, mockBackupCardList.Count).ToList());
            mockModelList.Add(mockModel);
        }

        return mockModelList;
    }

    private WholeAreaModel MockAction(WholeAreaModel model, List<ActionEvent> actionList)
    {
        var newModel = model;
        foreach (var actionEvent in actionList) {
            switch (actionEvent.args[0]) {
                case BattleModel.ActionType.ChooseCard: {
                    int id = (int)actionEvent.args[1];
                    CardModel card = newModel.FindCard(id);
                    if (card == null) {
                        KLog.E(TAG, "MockAction: ChooseCard: invalid id: " + id);
                        break;
                    }
                    newModel = newModel.ChooseCard(card, true);
                    break;
                }
                case BattleModel.ActionType.Pass: {
                    newModel = newModel.Pass(true);
                    break;
                }
                case BattleModel.ActionType.ChooseHornUtilArea: {
                    newModel = newModel.ChooseHornUtilArea((CardBadgeType)actionEvent.args[1], true);
                    break;
                }
                default: {
                    KLog.E(TAG, "MockAction: invalid type = " + actionEvent.args[0]);
                    break;
                }
            }
        }
        return newModel;
    }

    private struct BenifitRecord
    {
        public WholeAreaModel curModel;
        public float totalRate;
        public long timeout; // 超时时间，单位毫秒
    }

    // 计算几步后的收益，迭代计算，totalRate当前mockModel下的总占比，初始为1
    // realModel为当前实际的model，用于计算收益
    // mockModel为当前模拟了一步的model，在此基础上继续模拟
    private async UniTask<float> CalcModelBenifit(WholeAreaModel realModel, WholeAreaModel mockModel, long timeout)
    {
        int msPerFrame = 20;
        long lastFrameTs = KTime.CurrentMill();
        float benifit = 0f;
        var recordQueue = new Queue<BenifitRecord>();
        recordQueue.Enqueue(new BenifitRecord { curModel = mockModel, totalRate = 1f, timeout = timeout });
        while (recordQueue.Count > 0) {
            var record = recordQueue.Dequeue();
            var curModel = record.curModel;
            if (curModel.playTracker.isGameFinish || record.timeout < 2) {
                // 过深后进行估计
                benifit += CalcBenifitDiff(realModel, curModel) * record.totalRate;
                continue;
            }
            bool isSelfAction = curModel.EnableChooseCard(true);
            var nextModelList = new List<WholeAreaModel>();
            GetNextPossibleAction(curModel, ref nextModelList);
            // 剪枝，去除收益过低的分支
            nextModelList.RemoveAll(x => {
                int xDiff = CalcBenifitDiff(curModel, x);
                if (isSelfAction) {
                    return xDiff < -50;
                } else {
                    return xDiff > 50;
                }
            });
            nextModelList.Sort((x, y) => {
                int xDiff = CalcBenifitDiff(curModel, x);
                int yDiff = CalcBenifitDiff(curModel, y);
                if (isSelfAction) {
                    // self时升序排序
                    return xDiff.CompareTo(yDiff);
                } else {
                    // enemy时，由于CalcBenifitDiff的视角问题，反向一下
                    return yDiff.CompareTo(xDiff);
                }
            });
            int total = nextModelList.Count * (nextModelList.Count + 1) / 2;
            int cur = 1;
            foreach (var model in nextModelList) {
                // 根据一步的收益排序来决策分布，而不是平均分布所有可能路径
                float rate = (float)cur / total;
                cur += 1;
                recordQueue.Enqueue(new BenifitRecord { curModel = model, totalRate = record.totalRate * rate, timeout = (long)(record.timeout * rate) });
            }
            long currentTs = KTime.CurrentMill();
            if (currentTs - lastFrameTs > msPerFrame && aiMode != AIBase.AIMode.Test) {
                lastFrameTs = currentTs;
                await UniTask.Yield();
            }
        }
        return benifit;
    }

    // model的下一个行动方，实行所有可能的行动，并将新状态存入nextModelList
    // 行动指完成后action state为none
    private void GetNextPossibleAction(WholeAreaModel curModel, ref List<WholeAreaModel> nextModelList)
    {
        bool isSelfAction = curModel.EnableChooseCard(true);
        var selfModel = isSelfAction ? curModel.selfSinglePlayerAreaModel : curModel.enemySinglePlayerAreaModel;
        var enemyModel = isSelfAction ? curModel.enemySinglePlayerAreaModel : curModel.selfSinglePlayerAreaModel;
        var enableChooseList = new List<CardModel>();
        switch (curModel.gameState.actionState) {
            case GameState.ActionState.None: {
                enableChooseList = selfModel.handCardAreaModel.handCardListModel.cardList.ToList();
                enableChooseList.AddRange(selfModel.handCardAreaModel.leaderCardListModel.cardList);
                break;
            }
            case GameState.ActionState.ATTACKING: {
                foreach (var row in enemyModel.battleRowAreaList) {
                    foreach (var card in row.cardListModel.cardList) {
                        if (card.cardSelectType == CardSelectType.WithstandAttack) {
                            enableChooseList.Add(card);
                        }
                    }
                }
                break;
            }
            case GameState.ActionState.MEDICING: {
                foreach (var card in selfModel.discardAreaModel.cardListModel.cardList) {
                    if (card.cardSelectType == CardSelectType.PlayCard) {
                        enableChooseList.Add(card);
                    }
                }
                break;
            }
            case GameState.ActionState.DECOYING:
            case GameState.ActionState.MONAKAING: {
                foreach (var row in selfModel.battleRowAreaList) {
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

        if (curModel.gameState.actionState == GameState.ActionState.HORN_UTILING) {
            foreach (var row in selfModel.battleRowAreaList) {
                var newModel = curModel.ChooseHornUtilArea(row.rowType, isSelfAction);
                // HORN_UTILING不可能再继续了
                nextModelList.Add(newModel);
            }
        } else {
            if (curModel.gameState.actionState == GameState.ActionState.None) {
                nextModelList.Add(curModel.Pass(isSelfAction));
            }
            foreach (var card in enableChooseList) {
                var newModel = curModel.ChooseCard(card, isSelfAction);
                if (newModel.gameState.actionState != GameState.ActionState.None) {
                    GetNextPossibleAction(newModel, ref nextModelList);
                } else {
                    nextModelList.Add(newModel);
                }
            }
        }
    }

    private int CalcBenifitDiff(WholeAreaModel oldModel, WholeAreaModel newModel)
    {
        int benifit = 0;
        int setWeight = 30;
        int scoreWeight = 1;
        int selfHandCardWeight = 20; // self主要靠本方手牌和对方pass实现
        int enemyHandCardWeight = 4; // enemy主要靠对方手牌和本方pass实现
        if (newModel.playTracker.selfPlayerInfo.setScore < newModel.playTracker.enemyPlayerInfo.setScore ||
            (newModel.playTracker.selfPlayerInfo.setScore == newModel.playTracker.enemyPlayerInfo.setScore && newModel.playTracker.curSet > 0)) {
            // 这一小局必须赢，需要避免提前pass
            setWeight = 50;
            scoreWeight = 2;
            enemyHandCardWeight = 1;
        } else if (newModel.playTracker.selfPlayerInfo.setScore > newModel.playTracker.enemyPlayerInfo.setScore) {
            // 已经赢了一小局，可以更宽松地pass
            enemyHandCardWeight = 10;
        }
        // 局分差异
        int setDiff = (newModel.playTracker.selfPlayerInfo.setScore - newModel.playTracker.enemyPlayerInfo.setScore) - 
            (oldModel.playTracker.selfPlayerInfo.setScore - oldModel.playTracker.enemyPlayerInfo.setScore);
        benifit += setDiff * setWeight;
        // 点数差异
        int scoreDiff = (newModel.selfSinglePlayerAreaModel.GetCurrentPower() - newModel.enemySinglePlayerAreaModel.GetCurrentPower()) -
            (oldModel.selfSinglePlayerAreaModel.GetCurrentPower() - oldModel.enemySinglePlayerAreaModel.GetCurrentPower());
        scoreDiff = Math.Clamp(scoreDiff, -50, 50);
        benifit += scoreDiff * scoreWeight;
        // 手牌数差异
        int selfHandCardDiff = (newModel.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList.Count + newModel.selfSinglePlayerAreaModel.handCardAreaModel.leaderCardListModel.cardList.Count) -
            (oldModel.selfSinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList.Count + oldModel.selfSinglePlayerAreaModel.handCardAreaModel.leaderCardListModel.cardList.Count);
        selfHandCardWeight = selfHandCardDiff > 0 ? selfHandCardWeight : selfHandCardWeight / 4; // 如果手牌数减少，权重减
        int enemyHandCardDiff = (newModel.enemySinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList.Count + newModel.enemySinglePlayerAreaModel.handCardAreaModel.leaderCardListModel.cardList.Count) -
            (oldModel.enemySinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList.Count + oldModel.enemySinglePlayerAreaModel.handCardAreaModel.leaderCardListModel.cardList.Count);
        enemyHandCardWeight = enemyHandCardDiff > 0 ? enemyHandCardWeight : enemyHandCardWeight / 4; // 如果手牌数减少，权重减
        benifit += selfHandCardDiff * selfHandCardWeight - enemyHandCardDiff * enemyHandCardWeight; // 注意这里enemy需要转负
        // 如果赢了/输了，直接最高/最低
        // 总的范围 -100 ~ 100
        int max = 100;
        int min = -100;
        if (newModel.playTracker.isGameFinish) {
            benifit = newModel.playTracker.isSelfWinner ? max : min;
        }
        benifit = Math.Clamp(benifit, min, max);
        return benifit;
    }
}
