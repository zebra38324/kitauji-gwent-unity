using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;

// 统一管理全局通知行为
public class PlaySceneManager
{
    private static string TAG = "PlaySceneManager";
    public enum PlaySceneMsg
    {
        FinishWithstandAttack = 0, // 完成攻击目标选择
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

    // 状态机
    private enum State
    {
        NOT_START = 0, // 还未开始
        WAIT_SELF_ACTION, // 本方回合，正在等待玩家操作
        SELF_ATTACKING, // 本方正在使用攻击技能中
        SELF_MEDICING, // 本方正在使用复活技能中
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
        if (discardArea == null) {
            discardArea = GameObject.Find("Canvas/Background/DiscardArea");
        }
        switch (msg)
        {
            case PlaySceneMsg.FinishWithstandAttack: {
                GameObject target = (GameObject)list[0];
                FinishAttack(); // TODO: 多个一样的牌咋办？
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
            case State.SELF_MEDICING: {
                FinishMedic();
                PlayCard(card);
                break;
            }
            default: {
                KLog.E(TAG, "PlayCardlAction: error state = " + curState);
                break;
            }
        }
        if (curState == State.SELF_DONE) {
            TransState(State.WAIT_SELF_ACTION); // TODO: 测试用
        }
    }

    private void TransState(State newState)
    {
        if (curState == newState) {
            return;
        }
        if (curState == State.SELF_MEDICING) {
            FinishMedic();
        }
        curState = newState;
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
            case CardLocation.DiscardArea: {
                discardArea.GetComponent<DiscardArea>().RemoveCard(card);
                SelfDiscardCardManager.Instance.RemoveCard(card);
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
                TransState(State.SELF_DONE);
                break;
            }
            case CardAbility.Attack: {
                card.GetComponent<CardAction>().cardLocation = CardLocation.SelfBattleArea;
                selfPlayArea.GetComponent<SinglePlayerArea>().AddNormalCard(card);
                ApplyAttack(card.GetComponent<CardDisplay>().GetCardInfo().attackNum);
                break;
            }
            case CardAbility.Medic: {
                card.GetComponent<CardAction>().cardLocation = CardLocation.SelfBattleArea;
                selfPlayArea.GetComponent<SinglePlayerArea>().AddNormalCard(card);
                ApplyMedic();
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
                TransState(State.SELF_DONE);
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
            TransState(State.SELF_DONE);
            return;
        }
        TransState(State.SELF_ATTACKING);
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
        TransState(State.WAIT_SELF_ACTION); // TODO: 测试用
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

    // 应用抱团技能
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

    // 应用复活技能
    private void ApplyMedic()
    {
        List<GameObject> targetList = SelfDiscardCardManager.Instance.GetCardList();
        List<GameObject> validList = new List<GameObject>();
        foreach (GameObject card in targetList) {
            if (card.GetComponent<CardDisplay>().GetCardInfo().cardType != CardType.Hero) {
                validList.Add(card);
            }
        }
        if (validList.Count == 0) {
            // 无可选卡牌，不显示弃牌区
            TransState(State.SELF_DONE);
            return;
        }
        foreach (GameObject card in validList) {
            card.GetComponent<CardAction>().selectType = CardSelectType.MedicDiscardCard;
        }
        discardArea.GetComponent<DiscardArea>().ShowArea(validList, true);
        TransState(State.SELF_MEDICING);
    }

    private void FinishMedic()
    {
        discardArea.GetComponent<DiscardArea>().CloseArea();
        List<GameObject> discardList = SelfDiscardCardManager.Instance.GetCardList();
        foreach (GameObject card in discardList) {
            card.GetComponent<CardAction>().selectType = CardSelectType.None;
        }
    }
}
