using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// 统一管理全局通知行为
public class PlaySceneManager
{
    private static string TAG = "PlaySceneManager";
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

    public enum CardBoardcastType
    {
        CountBond = 0, // 统计本方场上特定bond type的卡牌数量
        UpdateBond, // 更新本方场上特定bond type的buff状态
        Tunning, // 本方应用调音技能
        WillWithstandAttack, // 统计对方场上可被攻击的对象，并使其做好被攻击准备
        FinishAttack, // 完成攻击动作，恢复卡牌状态
    }

    public delegate int CardBoardcastDelegate(CardBoardcastType cardBoardcastType, params object[] list);

    public CardBoardcastDelegate CardBoardcast;

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
        SELF_ATTACKING, // 本方正在使用攻击技能中
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
        if (cardPrefab == null) {
            cardPrefab = Resources.Load<GameObject>("Prefabs/HalfCard");
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
                FinishAttack();
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
                PlayCardAction(card);
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
        if (card.GetComponent<CardDisplay>().GetCardInfo().ability == CardAbility.Spy) {
            DrawCards(2);
            enemyPlayArea.GetComponent<SinglePlayerArea>().AddNormalCard(card);
        } else {
            selfPlayArea.GetComponent<SinglePlayerArea>().AddNormalCard(card);
        }
        return false;
    }

    private void PlayCardAction(GameObject card)
    {
        switch (curState) {
            case State.WAIT_SELF_ACTION: {
                PlayCard(card);
                break;
            }
            case State.SELF_ATTACKING: {
                FinishAttack();
                PlayCard(card);
                break;
            }
            default: {
                // TODO: LOG: error
                KLog.E(TAG, "PlayCardlAction: error state = " + curState);
                break;
            }
        }
        if (curState == State.SELF_DONE) {
            curState = State.WAIT_SELF_ACTION; // TODO: 测试用
        }
    }

    private void PlayCard(GameObject card)
    {
        // 从原区域移除
        CardLocation cardLocation = card.GetComponent<CardAction>().cardLocation;
        switch (cardLocation) {
            case CardLocation.HandArea: {
                handArea.GetComponent<HandArea>().RemoveCard(card);
                break;
            }
            default: {
                break;
            }
        }
        // 打出这张牌，并根据技能判断是否跟进后续操作
        switch (card.GetComponent<CardDisplay>().GetCardInfo().ability) {
            case CardAbility.Spy: {
                DrawCards(2);
                card.GetComponent<CardAction>().cardLocation = CardLocation.EnemyBattleArea;
                enemyPlayArea.GetComponent<SinglePlayerArea>().AddNormalCard(card);
                curState = State.SELF_DONE;
                break;
            }
            case CardAbility.Attack: {
                card.GetComponent<CardAction>().cardLocation = CardLocation.SelfBattleArea;
                selfPlayArea.GetComponent<SinglePlayerArea>().AddNormalCard(card);
                ApplyAttack(card.GetComponent<CardDisplay>().GetCardInfo().attackNum);
                break;
            }
            default: {
                card.GetComponent<CardAction>().cardLocation = CardLocation.SelfBattleArea;
                selfPlayArea.GetComponent<SinglePlayerArea>().AddNormalCard(card);
                switch (card.GetComponent<CardDisplay>().GetCardInfo().ability) {
                    case CardAbility.Tunning: {
                        ApplyTunning();
                        break;
                    }
                    case CardAbility.Bond: {
                        ApplyBond(card.GetComponent<CardDisplay>().GetCardInfo().bondType);
                        break;
                    }
                    case CardAbility.ScorchWood: {
                        ApplyScorchWood();
                        break;
                    }
                    case CardAbility.Muster: {
                        ApplyMuster(card.GetComponent<CardDisplay>().GetCardInfo().musterType);
                        break;
                    }
                    default: {
                        break; // 其他技能交到下层各自实现即可
                    }
                }
                curState = State.SELF_DONE;
                break;
            }
        }
        selfPlayArea.GetComponent<SinglePlayerArea>().UpdateScore(); // TODO: 优化调用时机
        enemyPlayArea.GetComponent<SinglePlayerArea>().UpdateScore();
    }

    // 从备选卡牌中拉取几张牌到手牌区
    private void DrawCards(int num)
    {
        // 备选卡牌中拉取
        List<CardInfo> cardInfos = BackupCardManager.Instance.GetCardInfos(num);
        foreach (CardInfo info in cardInfos) {
            GameObject card = GameObject.Instantiate(cardPrefab, null);
            card.GetComponent<CardDisplay>().SetCardInfo(info);
            handArea.GetComponent<HandArea>().AddCard(card);
        }
    }

    // 实施攻击牌技能
    private void ApplyAttack(int attackNum)
    {
        int count = GetAttackTargetNum(attackNum);
        if (count == 0) {
            curState = State.SELF_DONE;
            return;
        }
        curState = State.SELF_ATTACKING;
    }

    // 获取可攻击的目标数量，若有，就使其准备好被攻击
    private int GetAttackTargetNum(int attackNum)
    {
        if (CardBoardcast == null) {
            return 0;
        }
        int count = 0;
        foreach (CardBoardcastDelegate invocation in CardBoardcast.GetInvocationList()) {
            count += invocation(CardBoardcastType.WillWithstandAttack, attackNum);
        }
        return count;
    }

    // 完成攻击动作
    private void FinishAttack()
    {
        if (CardBoardcast != null) {
            CardBoardcast(CardBoardcastType.FinishAttack);
        }
        curState = State.WAIT_SELF_ACTION; // TODO: 测试用
    }

    private void ApplyTunning()
    {
        if (CardBoardcast != null) {
            CardBoardcast(CardBoardcastType.Tunning);
        }
    }

    // 应用bond技能
    private void ApplyBond(string bondType)
    {
        int count = GetBondCardCount(bondType); // 此时count包含刚刚打出去的那张
        if (CardBoardcast != null) {
            CardBoardcast(CardBoardcastType.UpdateBond, bondType, count);
        }
    }

    // 统计己方场上bondType的卡牌数量
    private int GetBondCardCount(string bondType)
    {
        if (CardBoardcast == null) {
            return 0;
        }
        int count = 0;
        foreach (CardBoardcastDelegate invocation in CardBoardcast.GetInvocationList()) {
            count += invocation(CardBoardcastType.CountBond, bondType);
        }
        return count;
    }

    private void ApplyScorchWood()
    {
        enemyPlayArea.GetComponent<SinglePlayerArea>().ScorchWood();
    }

    // 应用抱团技能 TODO: 合并同类项
    private void ApplyMuster(string musterType)
    {
        // 备选卡牌中拉取
        List<CardInfo> cardInfos = BackupCardManager.Instance.GetCardInfosWithMusterType(musterType);
        foreach (CardInfo info in cardInfos) {
            GameObject musterCard = GameObject.Instantiate(cardPrefab, null);
            musterCard.GetComponent<CardDisplay>().SetCardInfo(info);
            musterCard.GetComponent<CardAction>().cardLocation = CardLocation.SelfBattleArea;
            selfPlayArea.GetComponent<SinglePlayerArea>().AddNormalCard(musterCard);
        }

        // 手牌区拉取
        List<GameObject> cardList = handArea.GetComponent<HandArea>().GetMusterCardList(musterType);
        foreach (GameObject card in cardList) {
            handArea.GetComponent<HandArea>().RemoveCard(card);
            card.GetComponent<CardAction>().cardLocation = CardLocation.SelfBattleArea;
            selfPlayArea.GetComponent<SinglePlayerArea>().AddNormalCard(card);
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
