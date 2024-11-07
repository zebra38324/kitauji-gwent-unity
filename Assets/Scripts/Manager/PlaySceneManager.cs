using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

// 统一管理全局通知行为
public class PlaySceneManager
{
    public enum PlaySceneMsg
    {
        MedicSelectDiscardCard = 0, // 复活技能，已点击选取弃用卡牌
        PlayCardFromHandArea, // 从手牌区打出牌
        FinishWithstandAttack, // 完成攻击目标选择
        RemoveSingleCard, // 移除一个特定卡牌
        PlayCard, // 打出一张牌
        ShowCardInfo, // 显示卡牌信息
        HideCardInfo, // 隐藏卡牌信息
    }

    public delegate void CardEnableSelectDelegate(bool enable);

    public CardEnableSelectDelegate CardEnableSelect;

    private static readonly PlaySceneManager instance = new PlaySceneManager();

    private GameObject discardArea;

    private GameObject handArea;

    private GameObject selfPlayArea;
    private GameObject enemyPlayArea;
    private GameObject cardInfoArea;

    private GameObject cardPrefab;

    private GameObject curCard; // 当前需要异步处理的卡牌

    private BattleManager battleManager;

    private BattleAction action;

    // 状态机
    private enum State
    {
        NOT_START = 0, // 还未开始
        WAIT_SELF_ACTION, // 本方回合，正在等待玩家操作
        SELF_DOING, // 本方正在操作中
        SELF_DONE, // 本方操作结束
        WAIT_ENEMY_ACTION, // 等待对方操作
        STOP, // 本局结束
    }

    private State curState = State.WAIT_SELF_ACTION; // 测试时暂时WAIT_SELF_ACTION

    static PlaySceneManager() {}

    public static PlaySceneManager Instance
    {
        get
        {
            return instance;
        }
    }

    // 单例模式，每次启动一局游戏都需要调用Reset进行初始化
    public void Reset()
    {
        battleManager = new BattleManager();
        battleManager.Start();
        battleManager.EnemyActionNotify += new BattleManager.EnemyActionNotifyHandler(ApplyEnemyAction);
    }

    public bool IsSelfTurn()
    {
        //return battleManager.GetCurStatus() == BattleStatus.SelfTurn;
        return true;
    }

    public void HandleMessage(PlaySceneMsg msg, params object[] list)
    {
        if (selfPlayArea == null) {
            selfPlayArea = GameObject.Find("SelfPlayArea");
        }
        if (enemyPlayArea == null) {
            enemyPlayArea = GameObject.Find("EnemyPlayArea");
        }
        if (handArea == null) {
            handArea = GameObject.Find("HandArea");
        }
        if (cardInfoArea == null) {
            cardInfoArea = GameObject.Find("CardInfoArea");
        }
        switch (msg)
        {
            case PlaySceneMsg.MedicSelectDiscardCard: {
                if (discardArea == null) {
                    discardArea = GameObject.Find("DiscardArea");
                }
                GameObject card = (GameObject)list[0];
                discardArea.GetComponent<DiscardArea>().RemoveCard(card);
                discardArea.GetComponent<DiscardArea>().CloseArea();
                SelfDiscardCardManager.Instance.RemoveCard(card);
                bool needWait = AddCardToPlayArea(card);
                if (!needWait) {
                    action.extend = card.GetComponent<CardDisplay>().GetCardInfo();
                    FinishSelfTurn();
                }
                break;
            }
            case PlaySceneMsg.PlayCardFromHandArea: {
                GameObject card = (GameObject)list[0];
                bool isClick = (bool)list[1]; // 是否为玩家点击手牌触发的
                handArea.GetComponent<HandArea>().RemoveCard(card);
                bool needWait = AddCardToPlayArea(card);
                if (isClick) {
                    action.selected = card.GetComponent<CardDisplay>().GetCardInfo();
                    if (!needWait) {
                        FinishSelfTurn();
                    }
                }
                break;
            }
            case PlaySceneMsg.FinishWithstandAttack: {
                GameObject target = (GameObject)list[0];
                enemyPlayArea.GetComponent<SinglePlayerArea>().FinishWithstandAttack();
                selfPlayArea.GetComponent<SinglePlayerArea>().AddNormalCard(curCard); // 不考虑同时是间谍牌和攻击牌的情况
                curCard = null;
                action.extend = target.GetComponent<CardDisplay>().GetCardInfo();
                FinishSelfTurn(); // TODO: 多个一样的牌咋办？
                break;
            }
            case PlaySceneMsg.RemoveSingleCard: {
                GameObject card = (GameObject)list[0];
                bool isSelf = (bool)list[1];
                if (isSelf) {
                    selfPlayArea.GetComponent<SinglePlayerArea>().RemoveSingleCard(card);
                } else {
                    enemyPlayArea.GetComponent<SinglePlayerArea>().RemoveSingleCard(card);
                }
                break;
            }
            case PlaySceneMsg.PlayCard: {
                GameObject card = (GameObject)list[0];
                CardLocation cardLocation = (CardLocation)list[1];
                bool success = (bool)list[2];
                success = true;
                //bool isClick = (bool)list[2]; // 是否为玩家点击手牌触发的
                //PlayCard(card, cardLocation, isClick);
                break;
            }
            case PlaySceneMsg.ShowCardInfo: {
                CardInfo info = (CardInfo)list[0];
                cardInfoArea.GetComponent<CardInfoArea>().ShowInfo(info);
                break;
            }
            case PlaySceneMsg.HideCardInfo: {
                cardInfoArea.GetComponent<CardInfoArea>().HideInfo();
                break;
            }
        }
    }

    // return: 这张牌打出后，是否需要等待玩家的进一步操作
    private bool AddCardToPlayArea(GameObject card)
    {
        if (card.GetComponent<CardDisplay>().GetCardInfo().ability == CardAbility.Attack && ApplyAttackCard(card)) {
            curCard = card;
            return true;
        }
        if (card.GetComponent<CardDisplay>().GetCardInfo().ability == CardAbility.Spy) {
            DrawCards(2);
            enemyPlayArea.GetComponent<SinglePlayerArea>().AddNormalCard(card);
        } else {
            selfPlayArea.GetComponent<SinglePlayerArea>().AddNormalCard(card);
        }
        return false;
    }

    private void PlayCardlAction(GameObject card, CardLocation cardLocation)
    {
        switch (curState) {
            case State.WAIT_SELF_ACTION: {
                // 首先关闭所有卡牌的可选状态
                curState = State.SELF_DOING;
                DisableSelect();
                PlayCard(card, cardLocation);
                break;
            }
            case State.SELF_DOING: {
                PlayCard(card, cardLocation);
                break;
            }
            default: {
                // TODO: LOG: error
                break;
            }
        }
        if (curState == State.SELF_DONE) {
            EnableSelect();
        }
    }

    private void PlayCard(GameObject card, CardLocation cardLocation)
    {
        // 打出这张牌，并根据技能判断是否跟进后续操作
        switch (card.GetComponent<CardDisplay>().GetCardInfo().ability) {
            case CardAbility.Spy: {
                DrawCards(2);
                enemyPlayArea.GetComponent<SinglePlayerArea>().AddNormalCard(card);
                break;
            }
            case CardAbility.Medic: {
                break;
            }
            case CardAbility.Attack: {
                break;
            }
            default: {
                break; // 其他技能交到下层各自实现即可
            }
        }
    }

    // 关闭所有卡牌的可选状态
    private void DisableSelect()
    {
        if (CardEnableSelect != null) {
            CardEnableSelect(false);
        }
    }

    // 恢复卡牌的可选状态
    private void EnableSelect()
    {
        if (CardEnableSelect != null) {
            CardEnableSelect(true);
        }
    }

    // 实施攻击牌技能，如果没有可攻击的牌，返回false
    // 若有目标，则等待玩家选择完目标后再打出这个攻击牌
    private bool ApplyAttackCard(GameObject card)
    {
        int attackNum = card.GetComponent<CardDisplay>().GetCardInfo().attackNum;
        int count = enemyPlayArea.GetComponent<SinglePlayerArea>().ReadyEmbraceAttack(attackNum);
        return count > 0;
    }

    // 从备选卡牌中拉取几张牌到手牌区
    private void DrawCards(int num)
    {
        if (cardPrefab == null) {
            cardPrefab = Resources.Load<GameObject>("Prefabs/HalfCard");
        }
        // 备选卡牌中拉取
        List<CardInfo> cardInfos = BackupCardManager.Instance.GetCardInfos(num);
        foreach (CardInfo info in cardInfos) {
            GameObject card = GameObject.Instantiate(cardPrefab, null);
            card.GetComponent<CardDisplay>().SetCardInfo(info);
            handArea.GetComponent<HandArea>().AddCard(card);
        }
    }

    private void FinishSelfTurn()
    {
        //battleManager.FinishSelfTurn(action);
        action = new BattleAction();
    }

    private void ApplyEnemyAction(BattleAction action)
    {

    }
}
