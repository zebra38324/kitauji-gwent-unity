using LanguageExt;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
/**
 * 游戏对局场景的逻辑管理
 * 流程：
 * 1. SetBackupCardInfoIdList，设置本方备选卡牌，生成CardModel并发送信息至BattleModel
 * 2. EnemyMsgCallback，等待收到对方备选卡牌信息，并生成对应CardModel
 * 3. DrawInitHandCard，抽取本方手牌，等待用户选择重抽的牌
 * 4. ReDrawInitHandCard，确定初始手牌，并发送信息至BattleModel
 * 5. EnemyMsgCallback，收到对方手牌信息（双方都确定后开始游戏）
 * 6. ChooseCard，双方出牌
 * 7. Pass，跳过本局出牌。双方都pass后，结束本局。满足结束条件时，结束整场游戏
 */

public class PlaySceneModel
{
    private string TAG = "PlaySceneModel";

    public WholeAreaModel wholeAreaModel { get; private set; }

    public BattleModel battleModel { get; private set; }

    // ActionEvent中，battle msg在这层处理，其余的ui、音效相关传递到上层处理
    private Action<List<ActionEvent>> UpdateModelNotify;

    public PlaySceneModel(bool isHost,
        string selfName,
        string enemyName,
        CardGroup selfGroup,
        Action<List<ActionEvent>> notify)
    {
        TAG += isHost ? "-Host" : "-Player";
        wholeAreaModel = new WholeAreaModel(isHost, selfName, enemyName, selfGroup);
        battleModel = new BattleModel(isHost);
        battleModel.EnemyMsgCallback += EnemyMsgCallback;
        UpdateModelNotify = notify;
    }

    public void Release()
    {
        KLog.I(TAG, "Release");
        wholeAreaModel = null;
        battleModel.Release();
        battleModel = null;
    }

    // ========================== 以下接口仅self调用 ==============================

    // 设置本方备选卡牌
    public void SetBackupCardInfoIdList(List<int> selfInfoIdList)
    {
        wholeAreaModel = wholeAreaModel.SelfInit(selfInfoIdList);
        LoadActionEvent();
    }

    public void DrawInitHandCard()
    {
        wholeAreaModel = wholeAreaModel.SelfDrawInitHandCard();
        LoadActionEvent();
    }

    public void ReDrawInitHandCard()
    {
        wholeAreaModel = wholeAreaModel.SelfReDrawInitHandCard();
        LoadActionEvent();
    }

    // 跳过本局出牌
    public void Pass()
    {
        wholeAreaModel = wholeAreaModel.Pass(true);
        LoadActionEvent();
    }

    // 是否允许选择卡牌
    public bool EnableChooseCard()
    {
        return wholeAreaModel.EnableChooseCard(true);
    }

    public void ChooseCard(CardModel card)
    {
        wholeAreaModel = wholeAreaModel.ChooseCard(card, true);
        LoadActionEvent();
    }

    public void InterruptAction()
    {

        wholeAreaModel = wholeAreaModel.InterruptAction(true);
        LoadActionEvent();
    }

    public void ChooseHornUtilArea(CardBadgeType cardBadgeType)
    {
        wholeAreaModel = wholeAreaModel.ChooseHornUtilArea(cardBadgeType, true);
        LoadActionEvent();
    }

    // ========================== enemy使用 ==============================
    public void EnemyMsgCallback(BattleModel.ActionType actionType, params object[] list)
    {
        switch (actionType) {
            case BattleModel.ActionType.Init: {
                CardGroup cardGroup = (CardGroup)list[0];
                List<int> infoIdList = (List<int>)list[1];
                List<int> idList = (List<int>)list[2];
                bool hostFirst = false;
                int seed = 0;
                if (list.Length > 3) {
                    hostFirst = (bool)list[3];
                    seed = (int)list[4];
                }
                wholeAreaModel = wholeAreaModel.EnemyInit(infoIdList, idList, cardGroup, hostFirst, seed);
                LoadActionEvent();
                break;
            }
            case BattleModel.ActionType.DrawHandCard: {
                List<int> idList = (List<int>)list[0];
                if (wholeAreaModel.gameState.curState == GameState.State.WAIT_INIT_HAND_CARD) {
                    wholeAreaModel = wholeAreaModel.EnemyDrawInitHandCard(idList);
                } else {
                    wholeAreaModel = wholeAreaModel.EnemyDrawHandCard(idList);
                }
                LoadActionEvent();
                break;
            }
            case BattleModel.ActionType.ChooseCard: {
                List<int> idList = (List<int>)list[0];
                int id = idList[0];
                CardModel card = wholeAreaModel.FindCard(id);
                if (card == null) {
                    KLog.E(TAG, "EnemyMsgCallback: ChooseCard: invalid id: " + id);
                    return;
                }
                wholeAreaModel = wholeAreaModel.ChooseCard(card, false);
                LoadActionEvent();
                break;
            }
            case BattleModel.ActionType.Pass: {
                wholeAreaModel = wholeAreaModel.Pass(false);
                LoadActionEvent();
                break;
            }
            case BattleModel.ActionType.InterruptAction: {
                wholeAreaModel = wholeAreaModel.InterruptAction(false);
                LoadActionEvent();
                break;
            }
            case BattleModel.ActionType.ChooseHornUtilArea: {
                wholeAreaModel = wholeAreaModel.ChooseHornUtilArea((CardBadgeType)list[0], false);
                LoadActionEvent();
                break;
            }
        }
    }

    // ========================== 私有方法 ==============================
    // 调用wholeAreaModel接口后调用，执行其actionEventList，并将actionEventList置空
    // wholeAreaModel变化时调用，需要通知上层
    private void LoadActionEvent()
    {
        List<ActionEvent> notifyList = new List<ActionEvent>();
        foreach (ActionEvent actionEvent in wholeAreaModel.actionEventList) {
            switch (actionEvent.type) {
                case ActionEvent.Type.BattleMsg: {
                    BattleModel.ActionType type = (BattleModel.ActionType)actionEvent.args[0];
                    battleModel.AddSelfActionMsg(type, actionEvent.args.Skip(1).ToArray());
                    break;
                }
                default: {
                    notifyList.Add(actionEvent);
                    break;
                }
            }
        }
        wholeAreaModel = wholeAreaModel with {
            actionEventList = ImmutableList<ActionEvent>.Empty
        };
        UpdateModelNotify(notifyList);
    }
}
