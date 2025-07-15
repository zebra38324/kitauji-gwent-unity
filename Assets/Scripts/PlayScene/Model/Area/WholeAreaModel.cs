using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using LanguageExt;
using static LanguageExt.Prelude;

/**
 * PlayScene的整体区域逻辑，不涉及状态机逻辑
 * 封装所有接口，更上层不跨层调用下面的接口
 * 重要：同时也是整体的场上状态记录
 */
public record WholeAreaModel
{
    private string TAG = "WholeAreaModel";

    public PlayTracker playTracker { get; init; }

    public GameState gameState { get; init; }

    public SinglePlayerAreaModel selfSinglePlayerAreaModel { get; init; }

    public SinglePlayerAreaModel enemySinglePlayerAreaModel { get; init; }

    public WeatherAreaModel weatherAreaModel { get; init; } = new WeatherAreaModel();

    public static readonly Lens<WholeAreaModel, PlayTracker> Lens_PlayTracker = Lens<WholeAreaModel, PlayTracker>.New(
        w => w.playTracker,
        playTracker => w => w with { playTracker = playTracker }
    );

    public static readonly Lens<WholeAreaModel, CardGroup> Lens_PlayTracker_EnemyPlayerInfo_CardGroup = lens(Lens_PlayTracker, PlayTracker.Lens_EnemyPlayerInfo_CardGroup);

    public static readonly Lens<WholeAreaModel, SinglePlayerAreaModel> Lens_SelfSinglePlayerAreaModel = Lens<WholeAreaModel, SinglePlayerAreaModel>.New(
        w => w.selfSinglePlayerAreaModel,
        selfSinglePlayerAreaModel => w => w with { selfSinglePlayerAreaModel = selfSinglePlayerAreaModel }
    );

    public static readonly Lens<WholeAreaModel, SinglePlayerAreaModel> Lens_EnemySinglePlayerAreaModel = Lens<WholeAreaModel, SinglePlayerAreaModel>.New(
        w => w.enemySinglePlayerAreaModel,
        enemySinglePlayerAreaModel => w => w with { enemySinglePlayerAreaModel = enemySinglePlayerAreaModel }
    );

    public static readonly Lens<WholeAreaModel, HandCardAreaModel> Lens_SelfSinglePlayerAreaModel_HandCardAreaModel = lens(Lens_SelfSinglePlayerAreaModel, SinglePlayerAreaModel.Lens_HandCardAreaModel);

    public static readonly Lens<WholeAreaModel, InitHandCardListModel> Lens_SelfSinglePlayerAreaModel_HandCardAreaModel_InitHandCardListModel = lens(Lens_SelfSinglePlayerAreaModel_HandCardAreaModel, HandCardAreaModel.Lens_InitHandCardListModel);

    public static readonly Lens<WholeAreaModel, HandCardAreaModel> Lens_EnemySinglePlayerAreaModel_HandCardAreaModel = lens(Lens_EnemySinglePlayerAreaModel, SinglePlayerAreaModel.Lens_HandCardAreaModel);

    public ImmutableList<ActionEvent> actionEventList { get; init; } = ImmutableList<ActionEvent>.Empty; // 记录本次操作，主要用于消息传递与文本提示，由上层负责读取与清空

    public WholeAreaModel(bool isHost,
        string selfName,
        string enemyName,
        CardGroup selfGroup)
    {
        if (TAG == "WholeAreaModel") {
            TAG += isHost ? "-Host" : "-Player";
        }
        playTracker = new PlayTracker(isHost, selfName, enemyName, selfGroup);
        gameState = new GameState(isHost);
        CardGenerator gen = new CardGenerator(isHost);
        selfSinglePlayerAreaModel = new SinglePlayerAreaModel(gen, true);
        enemySinglePlayerAreaModel = new SinglePlayerAreaModel(gen, false);
    }

    // 初始时调用，设置所有备选卡牌信息，以及其他的一些初始化信息
    public WholeAreaModel SelfInit(List<int> infoIdList)
    {
        var newRecord = this;
        if (newRecord.gameState.curState != GameState.State.WAIT_BACKUP_INFO) {
            KLog.E(TAG, "SelfInit: state invalid: " + newRecord.gameState.curState);
            return newRecord;
        }
        newRecord = Lens_SelfSinglePlayerAreaModel_HandCardAreaModel.Set(newRecord.selfSinglePlayerAreaModel.handCardAreaModel.SetBackupCardInfoIdList(infoIdList), newRecord);

        // add action event
        List<int> idList = new List<int>();
        infoIdList = new List<int>();
        foreach (var card in newRecord.selfSinglePlayerAreaModel.handCardAreaModel.backupCardList) {
            infoIdList.Add(card.cardInfo.infoId);
            idList.Add(card.cardInfo.id);
        }
        if (newRecord.selfSinglePlayerAreaModel.handCardAreaModel.leaderCardListModel.cardList.Count > 0) {
            infoIdList.Add(newRecord.selfSinglePlayerAreaModel.handCardAreaModel.leaderCardListModel.cardList[0].cardInfo.infoId);
            idList.Add(newRecord.selfSinglePlayerAreaModel.handCardAreaModel.leaderCardListModel.cardList[0].cardInfo.id);
        }
        ActionEvent actionEvent = null;
        if (newRecord.playTracker.isHost) {
            bool hostFirst = PlayTracker.IsHostSelfFirst();
            int seed = new Random().Next();
            newRecord = newRecord with {
                playTracker = newRecord.playTracker.ConfigFirstSet(hostFirst),
                selfSinglePlayerAreaModel = newRecord.selfSinglePlayerAreaModel.InitKRandom(seed + 1),
                enemySinglePlayerAreaModel = newRecord.enemySinglePlayerAreaModel.InitKRandom(seed + 2),
            };
            actionEvent = new ActionEvent(ActionEvent.Type.BattleMsg,
                BattleModel.ActionType.Init,
                newRecord.playTracker.selfPlayerInfo.cardGroup,
                infoIdList,
                idList,
                hostFirst,
                seed);
        } else {
            actionEvent = new ActionEvent(ActionEvent.Type.BattleMsg,
                BattleModel.ActionType.Init,
                newRecord.playTracker.selfPlayerInfo.cardGroup,
                infoIdList,
                idList);
        }
        newRecord = newRecord with {
            actionEventList = newRecord.actionEventList.Add(actionEvent)
        };
        if (newRecord.selfSinglePlayerAreaModel.handCardAreaModel.backupCardList.Count > 0 &&
            newRecord.enemySinglePlayerAreaModel.handCardAreaModel.backupCardList.Count > 0) {
            newRecord = newRecord with {
                gameState = newRecord.gameState.TransState(GameState.State.WAIT_INIT_HAND_CARD)
            };
        }
        return newRecord;
    }

    // 初始时调用，设置所有备选卡牌信息，以及其他的一些初始化信息
    // enemy设置时idList由对端决定，因此不为null
    public WholeAreaModel EnemyInit(List<int> infoIdList, List<int> idList, CardGroup enemyCardGroup, bool hostFirst, int seed = 0)
    {
        var newRecord = this;
        if (newRecord.gameState.curState != GameState.State.WAIT_BACKUP_INFO) {
            KLog.E(TAG, "EnemyInit: state invalid: " + newRecord.gameState.curState);
            return newRecord;
        }
        newRecord = Lens_EnemySinglePlayerAreaModel_HandCardAreaModel.Set(newRecord.enemySinglePlayerAreaModel.handCardAreaModel.SetBackupCardInfoIdList(infoIdList, idList), newRecord);
        newRecord = Lens_PlayTracker_EnemyPlayerInfo_CardGroup.Set(enemyCardGroup, newRecord);
        if (!newRecord.playTracker.isHost) {
            // 非host，参照enemy传来的开局先手配置
            newRecord = Lens_PlayTracker.Set(newRecord.playTracker.ConfigFirstSet(!hostFirst), newRecord);
            newRecord = newRecord with {
                selfSinglePlayerAreaModel = newRecord.selfSinglePlayerAreaModel.InitKRandom(seed + 2),
                enemySinglePlayerAreaModel = newRecord.enemySinglePlayerAreaModel.InitKRandom(seed + 1),
            };
        }
        if (newRecord.selfSinglePlayerAreaModel.handCardAreaModel.backupCardList.Count > 0 &&
            newRecord.enemySinglePlayerAreaModel.handCardAreaModel.backupCardList.Count > 0) {
            newRecord = newRecord with {
                gameState = newRecord.gameState.TransState(GameState.State.WAIT_INIT_HAND_CARD)
            };
        }
        return newRecord;
    }

    // 仅self调用
    public WholeAreaModel SelfDrawInitHandCard()
    {
        var newRecord = this;
        if (newRecord.gameState.curState != GameState.State.WAIT_INIT_HAND_CARD) {
            KLog.E(TAG, "SelfDrawInitHandCard: state invalid: " + newRecord.gameState.curState);
            return newRecord;
        }
        return Lens_SelfSinglePlayerAreaModel_HandCardAreaModel.Set(newRecord.selfSinglePlayerAreaModel.handCardAreaModel.DrawInitHandCard(), newRecord);
    }

    // 仅self调用
    public WholeAreaModel SelfReDrawInitHandCard()
    {
        var newRecord = this;
        if (newRecord.gameState.curState != GameState.State.WAIT_INIT_HAND_CARD) {
            KLog.E(TAG, "SelfReDrawInitHandCard: state invalid: " + newRecord.gameState.curState);
            return newRecord;
        }
        newRecord = Lens_SelfSinglePlayerAreaModel_HandCardAreaModel.Set(newRecord.selfSinglePlayerAreaModel.handCardAreaModel.ReDrawInitHandCard(), newRecord);
        // add action event
        List<int> idList = new List<int>();
        foreach (var card in newRecord.selfSinglePlayerAreaModel.handCardAreaModel.initHandCardListModel.cardList) {
            idList.Add(card.cardInfo.id);
        }
        newRecord = newRecord with {
            actionEventList = newRecord.actionEventList.Add(new ActionEvent(ActionEvent.Type.BattleMsg, BattleModel.ActionType.DrawHandCard, idList))
                .Add(new ActionEvent(ActionEvent.Type.ActionText, string.Format("{0} 完成了初始手牌抽取\n", newRecord.playTracker.GetNameText(true))))
        };
        return newRecord.TryStartGame();
    }

    // 仅enemy调用
    public WholeAreaModel EnemyDrawInitHandCard(List<int> idList)
    {
        var newRecord = this;
        if (newRecord.gameState.curState != GameState.State.WAIT_INIT_HAND_CARD) {
            KLog.E(TAG, "EnemyDrawInitHandCard: state invalid: " + newRecord.gameState.curState);
            return newRecord;
        }
        newRecord = Lens_EnemySinglePlayerAreaModel_HandCardAreaModel.Set(newRecord.enemySinglePlayerAreaModel.handCardAreaModel.DrawHandCardsWithoutRandom(idList), newRecord);
        newRecord = newRecord with {
            actionEventList = newRecord.actionEventList.Add(new ActionEvent(ActionEvent.Type.ActionText, string.Format("{0} 完成了初始手牌抽取\n", newRecord.playTracker.GetNameText(false))))
        };
        return newRecord.TryStartGame();
    }

    // 是否允许选择卡牌
    public bool EnableChooseCard(bool isSelf = true)
    {
        if (isSelf) {
            return gameState.curState == GameState.State.WAIT_SELF_ACTION ||
                (gameState.curState == GameState.State.WAIT_INIT_HAND_CARD && !selfSinglePlayerAreaModel.handCardAreaModel.hasReDrawInitHandCard);
        } else {
            return gameState.curState == GameState.State.WAIT_ENEMY_ACTION;
        }
    }

    public WholeAreaModel ChooseCard(CardModel card, bool isSelf)
    {
        var newRecord = this;
        if (!EnableChooseCard(isSelf)) {
            KLog.E(TAG, "ChooseCard: state invalid: " + gameState.curState + ", isSelf = " + isSelf);
            return newRecord;
        }
        // 从选择卡牌方的视角来看
        var selfArea = isSelf ? newRecord.selfSinglePlayerAreaModel : newRecord.enemySinglePlayerAreaModel;
        var enemyArea = isSelf ? newRecord.enemySinglePlayerAreaModel : newRecord.selfSinglePlayerAreaModel;
        CardSelectType originSelectType = card.cardSelectType;
        KLog.I(TAG, "ChooseCard: " + card.cardInfo.chineseName + ", isSelf = " + isSelf);
        if (isSelf && newRecord.gameState.curState == GameState.State.WAIT_SELF_ACTION) {
            // 初始手牌抽取时，不发送选牌动作，因此增加了状态判断
            newRecord = newRecord with {
                // 发送本方选择牌动作
                actionEventList = newRecord.actionEventList.Add(new ActionEvent(ActionEvent.Type.BattleMsg, BattleModel.ActionType.ChooseCard, card.cardInfo.id))
            };
        }
        switch (originSelectType) {
            case CardSelectType.None: {
                KLog.I(TAG, "ChooseCard: selectType = None, can not choose");
                return newRecord;
            }
            case CardSelectType.ReDrawHandCard: {
                // 记录要选择的重抽手牌
                newRecord = Lens_SelfSinglePlayerAreaModel_HandCardAreaModel_InitHandCardListModel
                    .Set(selfArea.handCardAreaModel.initHandCardListModel.SelectCard(card), newRecord);
                break;
            }
            case CardSelectType.PlayCard: {
                if (gameState.actionState != GameState.ActionState.None && gameState.actionState != GameState.ActionState.MEDICING) {
                    KLog.I(TAG, "ChooseCard: actionState = " + gameState.actionState + ", can not play card");
                    return newRecord;
                }
                if (gameState.actionState == GameState.ActionState.MEDICING) {
                    if (card.cardLocation != CardLocation.DiscardArea) {
                        // 此时不许打出非弃牌区的牌
                        KLog.I(TAG, "ChooseCard: actionState = MEDICING, card location invalid = " + card.cardLocation);
                        return newRecord;
                    }
                    newRecord = newRecord with {
                        selfSinglePlayerAreaModel = isSelf ? selfArea.ResetCardSelectType() : enemyArea,
                        enemySinglePlayerAreaModel = isSelf ? enemyArea : selfArea.ResetCardSelectType(),
                    };
                }
                newRecord = newRecord with {
                    gameState = newRecord.gameState.TransActionState(GameState.ActionState.None) // medic技能可能递归打牌，先置空再进行后续
                };
                newRecord = newRecord.PlayCard(card, isSelf);
                break;
            }
            case CardSelectType.WithstandAttack: {
                newRecord = newRecord.ApplyWithstandAttack(card, isSelf);
                break;
            }
            case CardSelectType.DecoyWithdraw: {
                newRecord = newRecord.ApplyBeWithdraw(card, isSelf);
                break;
            }
            case CardSelectType.Monaka: {
                newRecord = newRecord.ApplyBeMonaka(card, isSelf);
                break;
            }
        }

        if (newRecord.gameState.actionState == GameState.ActionState.None) {
            // 流转双方出牌回合
            if (isSelf && gameState.curState == GameState.State.WAIT_SELF_ACTION) {
                newRecord = newRecord with {
                    gameState = newRecord.gameState.TransState(GameState.State.WAIT_ENEMY_ACTION)
                };
            } else if (gameState.curState == GameState.State.WAIT_ENEMY_ACTION) {
                newRecord = newRecord with {
                    gameState = newRecord.gameState.TransState(GameState.State.WAIT_SELF_ACTION)
                };
            }
        }

        return newRecord;
    }

    // 仅enemy调用
    public WholeAreaModel EnemyDrawHandCard(List<int> idList)
    {
        var newRecord = this;
        // 抽牌可能在状态流转后才到这里，因此不判断状态
        newRecord = Lens_EnemySinglePlayerAreaModel_HandCardAreaModel.Set(newRecord.enemySinglePlayerAreaModel.handCardAreaModel.DrawHandCardsWithoutRandom(idList), newRecord);
        newRecord = newRecord with {
            actionEventList = newRecord.actionEventList.Add(new ActionEvent(ActionEvent.Type.ActionText, string.Format("{0} 抽取了{1}张牌\n", newRecord.playTracker.GetNameText(false), idList.Count)))
        };
        return newRecord;
    }

    public WholeAreaModel Pass(bool isSelf)
    {
        KLog.I(TAG, "Pass: isSelf = " + isSelf);
        var newRecord = this;
        var newActionEventList = newRecord.actionEventList;
        if (isSelf) {
            newActionEventList = newActionEventList.Add(new ActionEvent(ActionEvent.Type.BattleMsg, BattleModel.ActionType.Pass));
        }
        newActionEventList = newActionEventList.Add(new ActionEvent(ActionEvent.Type.ActionText, string.Format("{0} 放弃跟牌\n", newRecord.playTracker.GetNameText(isSelf))));
        newRecord = newRecord with {
            gameState = newRecord.gameState.Pass(isSelf),
            actionEventList = newActionEventList,
        };
        if (newRecord.gameState.curState == GameState.State.SET_FINFISH) {
            newRecord = newRecord.SetFinish();
        }
        return newRecord;
    }

    // actionState不为None时，一些特殊情况，中止技能流程并流转curState
    public WholeAreaModel InterruptAction(bool isSelf)
    {
        KLog.I(TAG, "InterruptAction: isSelf = " + isSelf);
        var newRecord = this;
        GameState.State newState = isSelf ? GameState.State.WAIT_ENEMY_ACTION : GameState.State.WAIT_SELF_ACTION;
        newRecord = newRecord with {
            selfSinglePlayerAreaModel = newRecord.selfSinglePlayerAreaModel.ResetCardSelectType(),
            enemySinglePlayerAreaModel = newRecord.enemySinglePlayerAreaModel.ResetCardSelectType(),
            gameState = newRecord.gameState.TransActionState(GameState.ActionState.None).TransState(newState),
            actionEventList = newRecord.actionEventList.Add(new ActionEvent(ActionEvent.Type.ActionText, string.Format("{0} 未选择目标\n", newRecord.playTracker.GetNameText(isSelf)))),
        };
        if (isSelf) {
            newRecord = newRecord with {
                actionEventList = newRecord.actionEventList.Add(new ActionEvent(ActionEvent.Type.BattleMsg, BattleModel.ActionType.InterruptAction)),
            };
        }
        return newRecord;
    }

    public WholeAreaModel ChooseHornUtilArea(CardBadgeType cardBadgeType, bool isSelf)
    {
        KLog.I(TAG, "ChooseHornUtilArea: cardBadgeType = " + cardBadgeType + ", isSelf = " + isSelf);
        var newRecord = this;
        if (newRecord.gameState.actionState != GameState.ActionState.HORN_UTILING) {
            KLog.E(TAG, "ChooseHornUtilArea: actionState invalid: " + newRecord.gameState.actionState + ", isSelf = " + isSelf);
            return newRecord;
        }
        var selfArea = isSelf ? newRecord.selfSinglePlayerAreaModel : newRecord.enemySinglePlayerAreaModel;
        var enemyArea = isSelf ? newRecord.enemySinglePlayerAreaModel : newRecord.selfSinglePlayerAreaModel;
        selfArea = selfArea with {
            battleRowAreaList = selfArea.battleRowAreaList.SetItem((int)cardBadgeType, selfArea.battleRowAreaList[(int)cardBadgeType].AddCard(gameState.actionCard)),
        };
        GameState.State newState = isSelf ? GameState.State.WAIT_ENEMY_ACTION : GameState.State.WAIT_SELF_ACTION;
        newRecord = newRecord with {
            selfSinglePlayerAreaModel = isSelf ? selfArea : enemyArea,
            enemySinglePlayerAreaModel = isSelf ? enemyArea : selfArea,
            gameState = newRecord.gameState.TransActionState(GameState.ActionState.None).TransState(newState),
        };
        if (isSelf) {
            newRecord = newRecord with {
                actionEventList = newRecord.actionEventList.Add(new ActionEvent(ActionEvent.Type.BattleMsg, BattleModel.ActionType.ChooseHornUtilArea, (BattleModel.HornUtilAreaType)cardBadgeType)),
            };
        }
        return newRecord;
    }

    public CardModel FindCard(int id)
    {
        System.Func<SinglePlayerAreaModel, int, CardModel> findCard = (singlePlayerAreaModel, id) => {
            var targetLists = new[] {
                singlePlayerAreaModel.handCardAreaModel.backupCardList,
                singlePlayerAreaModel.handCardAreaModel.handCardListModel.cardList,
                singlePlayerAreaModel.handCardAreaModel.initHandCardListModel.cardList,
                singlePlayerAreaModel.handCardAreaModel.leaderCardListModel.cardList,
                singlePlayerAreaModel.discardAreaModel.cardListModel.cardList,
                singlePlayerAreaModel.battleRowAreaList[(int)CardBadgeType.Wood].cardListModel.cardList,
                singlePlayerAreaModel.battleRowAreaList[(int)CardBadgeType.Brass].cardListModel.cardList,
                singlePlayerAreaModel.battleRowAreaList[(int)CardBadgeType.Percussion].cardListModel.cardList
            };
            var card = targetLists
                .SelectMany(list => list)
                .FirstOrDefault(o => o.cardInfo.id == id);
            return card;
        };
        var card = findCard(selfSinglePlayerAreaModel, id);
        if (card != null) {
            return card;
        }
        card = findCard(enemySinglePlayerAreaModel, id);
        if (card != null) {
            return card;
        }
        card = new[] {
            weatherAreaModel.wood.cardList,
            weatherAreaModel.brass.cardList,
            weatherAreaModel.percussion.cardList,
        }.SelectMany(list => list).FirstOrDefault(o => o.cardInfo.id == id);
        return card;
    }

    // TODO: 暂时默认对enemy生效
    public WholeAreaModel ReplaceHandAndBackupCard(List<int> handInfoIdList, List<int> handIdList, List<int> backupInfoIdList, List<int> backupIdList, bool mockSelf = false)
    {
        var newRecord = this;
        if (mockSelf) {
            newRecord = Lens_SelfSinglePlayerAreaModel_HandCardAreaModel.Set(
                newRecord.selfSinglePlayerAreaModel.handCardAreaModel.ReplaceHandAndBackupCard(handInfoIdList, handIdList, backupInfoIdList, backupIdList),
                newRecord);
        } else {
            newRecord = Lens_EnemySinglePlayerAreaModel_HandCardAreaModel.Set(
                newRecord.enemySinglePlayerAreaModel.handCardAreaModel.ReplaceHandAndBackupCard(handInfoIdList, handIdList, backupInfoIdList, backupIdList),
                newRecord);
        }
        return newRecord;
    }

    // 尝试开始游戏
    private WholeAreaModel TryStartGame()
    {
        var newRecord = this;
        if (!newRecord.selfSinglePlayerAreaModel.handCardAreaModel.hasReDrawInitHandCard ||
            newRecord.enemySinglePlayerAreaModel.handCardAreaModel.handCardListModel.cardList.Count == 0) {
            return newRecord;
        }
        newRecord = Lens_SelfSinglePlayerAreaModel_HandCardAreaModel.Set(newRecord.selfSinglePlayerAreaModel.handCardAreaModel.LoadHandCard(), newRecord);
        newRecord = newRecord with {
            gameState = newRecord.gameState.TransState(GameState.State.WAIT_START)
        };
        GameState.State nextState = newRecord.playTracker.setRecordList[0].selfFirst ? GameState.State.WAIT_SELF_ACTION : GameState.State.WAIT_ENEMY_ACTION;
        newRecord = newRecord with {
            gameState = newRecord.gameState.TransState(nextState)
        };
        newRecord = newRecord with {
            actionEventList = newRecord.actionEventList.Add(new ActionEvent(ActionEvent.Type.ActionText, string.Format("新一局开始，{0} 先手\n", newRecord.playTracker.GetNameText(newRecord.playTracker.setRecordList[0].selfFirst))))
        };
        return newRecord;
    }

    /**
     * 打出卡牌，可能来源于
     * 1. 手牌主动打出
     * 2. 手牌被动打出
     * 3. 备选卡牌区打出
     * 4. 弃牌区复活打出
     * 5. 指挥牌
     */
    private WholeAreaModel PlayCard(CardModel card, bool isSelf)
    {
        var newRecord = this;
        KLog.I(TAG, "PlayCard: isSelf: " + isSelf + ", card: " + card.cardInfo.chineseName + ", ability: " + CardText.cardAbilityText[(int)card.cardInfo.ability]);
        // 从选择卡牌方的视角来看
        var selfArea = isSelf ? newRecord.selfSinglePlayerAreaModel : newRecord.enemySinglePlayerAreaModel;
        var enemyArea = isSelf ? newRecord.enemySinglePlayerAreaModel : newRecord.selfSinglePlayerAreaModel;
        var newActionList = newRecord.actionEventList;
        newActionList = newActionList.Add(new ActionEvent(ActionEvent.Type.ActionText, string.Format("{0} 打出卡牌：{1}\n", newRecord.playTracker.GetNameText(isSelf), PlayTracker.GetCardNameText(card))));
        CardModel realCard = card;
        // 从原区域移除
        switch (card.cardLocation) {
            case CardLocation.HandArea: {
                selfArea = SinglePlayerAreaModel.Lens_HandCardAreaModel_HandCardListModel
                    .Set(selfArea.handCardAreaModel.handCardListModel.RemoveCard(card, out realCard) as HandCardListModel, selfArea);
                break;
            }
            case CardLocation.DiscardArea: {
                selfArea = selfArea with {
                    discardAreaModel = selfArea.discardAreaModel.RemoveCard(card, out realCard)
                };
                break;
            }
            case CardLocation.SelfLeaderCardArea:
            case CardLocation.EnemyLeaderCardArea: {
                selfArea = SinglePlayerAreaModel.Lens_HandCardAreaModel_LeaderCardListModel
                    .Set(selfArea.handCardAreaModel.leaderCardListModel.RemoveCard(card, out realCard) as SingleCardListModel, selfArea);
                break;
            }
            default: {
                break;
            }
        }

        // 实施卡牌技能
        // 涉及双方的技能放在这一层，其他的放到下层各自实现即可
        // Medic较为特殊，放到下层的话，无法很好地确定结束技能流程的时机，因此也放在这里
        switch (realCard.cardInfo.ability) {
            case CardAbility.Spy: {
                enemyArea = enemyArea.AddBattleAreaCard(realCard);
                // 仅self时需要实际抽牌
                if (isSelf) {
                    selfArea = selfArea with {
                        handCardAreaModel = selfArea.handCardAreaModel.DrawHandCardsRandom(2, out var idList)
                    };
                    newActionList = newActionList.Add(new ActionEvent(ActionEvent.Type.BattleMsg, BattleModel.ActionType.DrawHandCard, idList))
                        .Add(new ActionEvent(ActionEvent.Type.ActionText, string.Format("{0} 抽取了{1}张牌\n", newRecord.playTracker.GetNameText(isSelf), idList.Count)));
                }
                break;
            }
            case CardAbility.Attack: {
                selfArea = selfArea.AddBattleAreaCard(realCard);
                enemyArea = enemyArea.PrepareAttackTarget(out var targetCardList);
                string toast;
                if (targetCardList.Count == 0) {
                    KLog.I(TAG, "PrepareAttackTarget: no target card");
                    toast = "无可攻击目标";
                } else {
                    toast = "请选择攻击目标";
                    newRecord = newRecord with {
                        gameState = newRecord.gameState.TransActionState(GameState.ActionState.ATTACKING, realCard)
                    };
                }
                if (isSelf) {
                    newActionList = newActionList.Add(new ActionEvent(ActionEvent.Type.Toast, toast));
                }
                break;
            }
            case CardAbility.Tunning: {
                selfArea = selfArea.AddBattleAreaCard(realCard);
                newActionList = newActionList.Add(new ActionEvent(ActionEvent.Type.Sfx, AudioManager.SFXType.Tunning));
                break;
            }
            case CardAbility.ScorchWood: {
                selfArea = selfArea.AddBattleAreaCard(realCard);
                enemyArea = enemyArea.ApplyScorchWood().RemoveDeadCard(out var removedCardList);
                newActionList = RecordScorchAction(newActionList, removedCardList, CardAbility.ScorchWood);
                break;
            }
            case CardAbility.Medic: {
                if (realCard.cardInfo.cardType == CardType.Normal ||
                    realCard.cardInfo.cardType == CardType.Hero) {
                    // 指挥牌可能是medic，不放入对战区
                    selfArea = selfArea.AddBattleAreaCard(realCard);
                }
                selfArea = selfArea.PrepareMedicTarget(out var targetCardList);
                string toast;
                if (targetCardList.Count == 0) {
                    KLog.I(TAG, "PrepareMedicTarget: no target card");
                    toast = "无可复活目标";
                } else {
                    toast = "请选择复活目标";
                    newRecord = newRecord with {
                        gameState = newRecord.gameState.TransActionState(GameState.ActionState.MEDICING)
                    };
                }
                if (isSelf) {
                    newActionList = newActionList.Add(new ActionEvent(ActionEvent.Type.Toast, toast));
                }
                break;
            }
            case CardAbility.Decoy: {
                selfArea = selfArea.PrepareDecoyTarget(out var targetCardList);
                string toast;
                if (targetCardList.Count == 0) {
                    KLog.I(TAG, "PrepareDecoyTarget: no target card");
                    toast = "无可使用大号君的目标";
                } else {
                    toast = "请选择使用大号君的目标";
                    newRecord = newRecord with {
                        gameState = newRecord.gameState.TransActionState(GameState.ActionState.DECOYING, realCard)
                    };
                }
                if (isSelf) {
                    newActionList = newActionList.Add(new ActionEvent(ActionEvent.Type.Toast, toast));
                }
                break;
            }
            case CardAbility.Scorch: {
                int maxNormalPower = Math.Max(selfArea.GetMaxNormalPower(), enemyArea.GetMaxNormalPower());
                selfArea = selfArea.ApplyScorch(maxNormalPower).RemoveDeadCard(out var selfRemovedCardList);
                enemyArea = enemyArea.ApplyScorch(maxNormalPower).RemoveDeadCard(out var enemyRemovedCardList);
                newActionList = RecordScorchAction(newActionList, selfRemovedCardList.Concat(enemyRemovedCardList).ToList(), CardAbility.Scorch);
                if (realCard.cardInfo.cardType == CardType.Normal ||
                    realCard.cardInfo.cardType == CardType.Hero) {
                    // 角色牌带Scorch，先技能结算再打出，不然可能退掉自己
                    selfArea = selfArea.AddBattleAreaCard(realCard);
                }
                break;
            }
            case CardAbility.SunFes:
            case CardAbility.Daisangakushou:
            case CardAbility.Drumstick: {
                newRecord = newRecord with {
                    weatherAreaModel = newRecord.weatherAreaModel.AddCard(card)
                };
                int rowIndex = realCard.cardInfo.ability switch {
                    CardAbility.SunFes => (int)CardBadgeType.Wood,
                    CardAbility.Daisangakushou => (int)CardBadgeType.Brass,
                    CardAbility.Drumstick => (int)CardBadgeType.Percussion,
                    _ => (int)CardBadgeType.None,
                };
                selfArea = selfArea with {
                    battleRowAreaList = selfArea.battleRowAreaList.SetItem(rowIndex, selfArea.battleRowAreaList[rowIndex].SetWeatherBuff(true))
                };
                enemyArea = enemyArea with {
                    battleRowAreaList = enemyArea.battleRowAreaList.SetItem(rowIndex, enemyArea.battleRowAreaList[rowIndex].SetWeatherBuff(true))
                };
                selfArea = selfArea.RemoveDeadCard(out var selfRemovedCardList);
                enemyArea = enemyArea.RemoveDeadCard(out var enemyRemovedCardList);
                newActionList = RecordScorchAction(newActionList, selfRemovedCardList.Concat(enemyRemovedCardList).ToList(), realCard.cardInfo.ability);
                break;
            }
            case CardAbility.ClearWeather: {
                newRecord = newRecord with {
                    weatherAreaModel = newRecord.weatherAreaModel.RemoveAllCard(out var removedCardList)
                };
                selfArea = selfArea with {
                    battleRowAreaList = selfArea.battleRowAreaList.Select(row => row.SetWeatherBuff(false)).ToImmutableList()
                };
                enemyArea = enemyArea with {
                    battleRowAreaList = enemyArea.battleRowAreaList.Select(row => row.SetWeatherBuff(false)).ToImmutableList()
                };
                break;
            }
            case CardAbility.HornUtil: {
                if (isSelf) {
                    newActionList = newActionList.Add(new ActionEvent(ActionEvent.Type.Toast, "请选择指导老师的目标行"));
                }
                newRecord = newRecord with {
                    gameState = newRecord.gameState.TransActionState(GameState.ActionState.HORN_UTILING, realCard)
                };
                break;
            }
            case CardAbility.Lip: {
                selfArea = selfArea.AddBattleAreaCard(realCard);
                enemyArea = enemyArea.ApplyLip(out var attackCardList).RemoveDeadCard(out var removedCardList);
                if (removedCardList.Count > 0) {
                    newActionList = RecordScorchAction(newActionList, removedCardList, CardAbility.Lip);
                } else if (attackCardList.Count > 0) {
                    newActionList = RecordAttackAction(newActionList, attackCardList, CardAbility.Lip);
                }
                break;
            }
            case CardAbility.Guard: {
                selfArea = selfArea.AddBattleAreaCard(realCard);
                bool hasRelatedCard = selfArea.HasCardInBattleArea(realCard.cardInfo.relatedCard) || enemyArea.HasCardInBattleArea(realCard.cardInfo.relatedCard);
                if (!hasRelatedCard) {
                    if (isSelf) {
                        newActionList = newActionList.Add(new ActionEvent(ActionEvent.Type.Toast, "守卫目标不在场上，无法攻击"));
                    }
                    KLog.I(TAG, "Guard: no related card");
                    break;
                }
                enemyArea = enemyArea.PrepareAttackTarget(out var targetCardList);
                string toast;
                if (targetCardList.Count == 0) {
                    KLog.I(TAG, "Guard: no target card");
                    toast = "无可攻击目标";
                } else {
                    toast = "请选择攻击目标";
                    newRecord = newRecord with {
                        gameState = newRecord.gameState.TransActionState(GameState.ActionState.ATTACKING, realCard)
                    };
                }
                if (isSelf) {
                    newActionList = newActionList.Add(new ActionEvent(ActionEvent.Type.Toast, toast));
                }
                break;
            }
            case CardAbility.Monaka: {
                selfArea = selfArea.PrepareMonakaTarget(out var targetCardList);
                string toast;
                if (targetCardList.Count == 0) {
                    KLog.I(TAG, "PrepareMonakaTarget: no target card");
                    toast = "无可使用monaka的目标";
                } else {
                    toast = "请选择使用monaka的目标";
                    newRecord = newRecord with {
                        gameState = newRecord.gameState.TransActionState(GameState.ActionState.MONAKAING)
                    };
                }
                if (isSelf) {
                    newActionList = newActionList.Add(new ActionEvent(ActionEvent.Type.Toast, toast));
                }
                selfArea = selfArea.AddBattleAreaCard(realCard); // 要先判断monaka，再把牌打出去。不然就加强自己了
                break;
            }
            case CardAbility.PowerFirst: {
                enemyArea = enemyArea.ApplyPowerFirst(out var attackCardList).RemoveDeadCard(out var removedCardList);
                if (removedCardList.Count > 0) {
                    newActionList = RecordScorchAction(newActionList, removedCardList, CardAbility.PowerFirst);
                } else if (attackCardList.Count > 0) {
                    newActionList = RecordAttackAction(newActionList, attackCardList, CardAbility.PowerFirst);
                }
                break;
            }
            case CardAbility.TubaAlliance: {
                selfArea = selfArea.AddBattleAreaCard(realCard);
                // 仅self时需要实际抽牌
                CardModel tubaCard = selfArea.handCardAreaModel.backupCardList.Find(x => x.cardInfo.chineseName == "大号君");
                if (isSelf && tubaCard != null) {
                    var idList = new List<int> { tubaCard.cardInfo.id };
                    selfArea = selfArea with {
                        handCardAreaModel = selfArea.handCardAreaModel.DrawHandCardsWithoutRandom(idList)
                    };
                    newActionList = newActionList.Add(new ActionEvent(ActionEvent.Type.BattleMsg, BattleModel.ActionType.DrawHandCard, idList))
                        .Add(new ActionEvent(ActionEvent.Type.ActionText, string.Format("{0} 抽取了{1}张牌\n", newRecord.playTracker.GetNameText(isSelf), idList.Count)));
                }
                break;
            }
            default: {
                selfArea = selfArea.AddBattleAreaCard(realCard);
                break;
            }
        }


        newRecord = newRecord with {
            selfSinglePlayerAreaModel = isSelf ? selfArea : enemyArea,
            enemySinglePlayerAreaModel = isSelf ? enemyArea : selfArea,
            actionEventList = newActionList
        };
        return newRecord;
    }

    private WholeAreaModel ApplyWithstandAttack(CardModel card, bool isSelf)
    {
        var newRecord = this;
        var selfArea = isSelf ? newRecord.selfSinglePlayerAreaModel : newRecord.enemySinglePlayerAreaModel;
        var enemyArea = isSelf ? newRecord.enemySinglePlayerAreaModel : newRecord.selfSinglePlayerAreaModel;
        var newActionList = newRecord.actionEventList;
        if (newRecord.gameState.actionCard == null) {
            KLog.E(TAG, "ApplyWithstandAttack: actionCard is null");
            return newRecord;
        }
        int rowIndex = (int)card.cardInfo.badgeType;
        // 注意，此处攻击力4的guard技能也会走到
        var attackCard = newRecord.gameState.actionCard;
        CardBuffType cardBuffType = attackCard.cardInfo.attackNum == 2 ? CardBuffType.Attack2 : CardBuffType.Attack4;
        var newCard = card.AddBuff(cardBuffType, 1);
        enemyArea = enemyArea with {
            battleRowAreaList = enemyArea.battleRowAreaList.SetItem(rowIndex, enemyArea.battleRowAreaList[rowIndex].ReplaceCard(card, newCard))
        };
        enemyArea = enemyArea.RemoveDeadCard(out var removedCardList).ResetCardSelectType();
        if (removedCardList.Count > 0) {
            newActionList = RecordScorchAction(newActionList, removedCardList, attackCard.cardInfo.ability);
        } else {
            newActionList = RecordAttackAction(newActionList, new List<CardModel> { newCard }, attackCard.cardInfo.ability);
        }
        newRecord = newRecord with {
            selfSinglePlayerAreaModel = isSelf ? selfArea : enemyArea,
            enemySinglePlayerAreaModel = isSelf ? enemyArea : selfArea,
            gameState = newRecord.gameState.TransActionState(GameState.ActionState.None), // 攻击技能，先完成再置空
            actionEventList = newActionList
        };
        return newRecord;
    }

    private WholeAreaModel ApplyBeWithdraw(CardModel card, bool isSelf)
    {
        var newRecord = this;
        var selfArea = isSelf ? newRecord.selfSinglePlayerAreaModel : newRecord.enemySinglePlayerAreaModel;
        var enemyArea = isSelf ? newRecord.enemySinglePlayerAreaModel : newRecord.selfSinglePlayerAreaModel;
        var newActionList = newRecord.actionEventList;
        if (newRecord.gameState.actionCard == null) {
            KLog.E(TAG, "ApplyBeWithdraw: actionCard is null");
            return newRecord;
        }
        selfArea = selfArea.DecoyWithdrawCard(card, newRecord.gameState.actionCard).ResetCardSelectType();
        newRecord = newRecord with {
            selfSinglePlayerAreaModel = isSelf ? selfArea : enemyArea,
            enemySinglePlayerAreaModel = isSelf ? enemyArea : selfArea,
            gameState = newRecord.gameState.TransActionState(GameState.ActionState.None),
            actionEventList = newRecord.actionEventList.Add(new ActionEvent(ActionEvent.Type.ActionText, string.Format("施放{0}技能，换下卡牌：{1}\n", PlayTracker.GetAbilityNameText(CardAbility.Decoy), PlayTracker.GetCardNameText(card))))
        };
        return newRecord;
    }

    private WholeAreaModel ApplyBeMonaka(CardModel card, bool isSelf)
    {
        var newRecord = this;
        var selfArea = isSelf ? newRecord.selfSinglePlayerAreaModel : newRecord.enemySinglePlayerAreaModel;
        var enemyArea = isSelf ? newRecord.enemySinglePlayerAreaModel : newRecord.selfSinglePlayerAreaModel;
        int rowIndex = (int)card.cardInfo.badgeType;
        var newCard = card.AddBuff(CardBuffType.Monaka, 1);
        selfArea = selfArea with {
            battleRowAreaList = selfArea.battleRowAreaList.SetItem(rowIndex, selfArea.battleRowAreaList[rowIndex].ReplaceCard(card, newCard))
        };
        selfArea = selfArea.ResetCardSelectType();
        newRecord = newRecord with {
            selfSinglePlayerAreaModel = isSelf ? selfArea : enemyArea,
            enemySinglePlayerAreaModel = isSelf ? enemyArea : selfArea,
            gameState = newRecord.gameState.TransActionState(GameState.ActionState.None),
            actionEventList = newRecord.actionEventList.Add(new ActionEvent(ActionEvent.Type.ActionText, string.Format("施放{0}技能，增益卡牌：{1}\n", PlayTracker.GetAbilityNameText(CardAbility.Monaka), PlayTracker.GetCardNameText(card))))
        };
        return newRecord;
    }

    private WholeAreaModel SetFinish()
    {
        KLog.I(TAG, "SetFinish");
        var newRecord = this;
        // 清理卡牌、buff
        var selfArea = newRecord.selfSinglePlayerAreaModel.RemoveAllBattleCard(newRecord.playTracker.selfPlayerInfo.cardGroup == CardGroup.KumikoThirdYear);
        selfArea = selfArea with {
            battleRowAreaList = selfArea.battleRowAreaList.Select(row => row.SetWeatherBuff(false)).ToImmutableList()
        };
        var enemyArea = newRecord.enemySinglePlayerAreaModel.RemoveAllBattleCard(newRecord.playTracker.enemyPlayerInfo.cardGroup == CardGroup.KumikoThirdYear);
        enemyArea = enemyArea with {
            battleRowAreaList = enemyArea.battleRowAreaList.Select(row => row.SetWeatherBuff(false)).ToImmutableList()
        };
        newRecord = newRecord with {
            selfSinglePlayerAreaModel = selfArea,
            enemySinglePlayerAreaModel = enemyArea,
            weatherAreaModel = newRecord.weatherAreaModel.RemoveAllCard(out var removedCardList),
            playTracker = newRecord.playTracker.SetFinish(newRecord.selfSinglePlayerAreaModel.GetCurrentPower(), newRecord.enemySinglePlayerAreaModel.GetCurrentPower()),
        };

        // 新一局或者比赛结束的处理
        var newPlayTracker = newRecord.playTracker;
        var newActionEventList = newRecord.actionEventList;
        var newGameState = newRecord.gameState;
        string setFinishActionText;
        int lastSet = newPlayTracker.isGameFinish ? newPlayTracker.curSet : newPlayTracker.curSet - 1;
        int lastSetResult = newPlayTracker.setRecordList[lastSet].result;
        if (lastSetResult > 0) {
            setFinishActionText = string.Format("本局结果：{0} 胜利\n", newPlayTracker.GetNameText(true));
        } else if (lastSetResult < 0) {
            setFinishActionText = string.Format("本局结果：{0} 胜利\n", newPlayTracker.GetNameText(false));
        } else {
            setFinishActionText = "本局结果：双方平局\n";
        }
        newActionEventList = newActionEventList.Add(new ActionEvent(ActionEvent.Type.ActionText, setFinishActionText));
        if (newPlayTracker.isGameFinish) {
            newGameState = newGameState.TransState(GameState.State.STOP);
            if (newPlayTracker.isSelfWinner) {
                newActionEventList = newActionEventList.Add(new ActionEvent(ActionEvent.Type.Sfx, AudioManager.SFXType.Win));
            }
        } else {
            bool nextSelfFirst = newPlayTracker.setRecordList[newPlayTracker.curSet].selfFirst;
            var newState = nextSelfFirst ? GameState.State.WAIT_SELF_ACTION : GameState.State.WAIT_ENEMY_ACTION;
            newGameState = newGameState.TransState(newState);
            newActionEventList = newActionEventList.Add(new ActionEvent(ActionEvent.Type.Sfx, AudioManager.SFXType.SetFinish))
                .Add(new ActionEvent(ActionEvent.Type.ActionText, string.Format("新一局开始，{0} 先手\n", newRecord.playTracker.GetNameText(nextSelfFirst))));
            if (newPlayTracker.selfPlayerInfo.cardGroup == CardGroup.KumikoFirstYear && lastSetResult > 0) {
                // 久一年技能
                newRecord = Lens_SelfSinglePlayerAreaModel_HandCardAreaModel.Set(newRecord.selfSinglePlayerAreaModel.handCardAreaModel.DrawHandCardsRandom(1, out var idList), newRecord);
                newActionEventList = newActionEventList.Add(new ActionEvent(ActionEvent.Type.BattleMsg, BattleModel.ActionType.DrawHandCard, idList))
                    .Add(new ActionEvent(ActionEvent.Type.ActionText, string.Format("{0} 抽取了{1}张牌\n", newRecord.playTracker.GetNameText(true), idList.Count)));
            }
        }

        newRecord = newRecord with {
            playTracker = newPlayTracker,
            gameState = newGameState,
            actionEventList = newActionEventList,
        };
        return newRecord;
    }

    // 记录卡牌退部操作
    private static ImmutableList<ActionEvent> RecordScorchAction(ImmutableList<ActionEvent> actionList, List<CardModel> removedCardList, CardAbility cardAbility)
    {
        if (removedCardList.Count == 0) {
            KLog.I("WholeAreaModel", "RecordScorchAction: removedCardList is empty");
            if (cardAbility == CardAbility.ScorchWood || cardAbility == CardAbility.Scorch) {
                actionList = actionList.Add(new ActionEvent(ActionEvent.Type.ActionText, string.Format("施放{0}技能，无退部目标\n", PlayTracker.GetAbilityNameText(cardAbility))));
            }
            return actionList;
        }
        actionList = actionList.Add(new ActionEvent(ActionEvent.Type.Sfx, AudioManager.SFXType.Scorch));
        string removedCardNameStr = string.Join("、", removedCardList.Select(o => PlayTracker.GetCardNameText(o)));
        actionList = actionList.Add(new ActionEvent(ActionEvent.Type.ActionText, string.Format("施放{0}技能，移除卡牌：{1}\n", PlayTracker.GetAbilityNameText(cardAbility), removedCardNameStr)));
        return actionList;
    }

    // 记录卡牌攻击操作
    private static ImmutableList<ActionEvent> RecordAttackAction(ImmutableList<ActionEvent> actionList, List<CardModel> attackCardList, CardAbility cardAbility)
    {
        if (attackCardList.Count == 0) {
            KLog.I("WholeAreaModel", "RecordAttackAction: attackCardList is empty");
            return actionList;
        }
        actionList = actionList.Add(new ActionEvent(ActionEvent.Type.Sfx, AudioManager.SFXType.Attack));
        string removedCardNameStr = string.Join("、", attackCardList.Select(o => PlayTracker.GetCardNameText(o)));
        actionList = actionList.Add(new ActionEvent(ActionEvent.Type.ActionText, string.Format("施放{0}技能，攻击卡牌：{1}\n", PlayTracker.GetAbilityNameText(cardAbility), removedCardNameStr)));
        return actionList;
    }
}
