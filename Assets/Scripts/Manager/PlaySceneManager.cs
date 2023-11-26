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
        ShowCardInfo, // 显示卡牌信息
        HideCardInfo, // 隐藏卡牌信息
    }

    private static readonly PlaySceneManager instance = new PlaySceneManager();

    private GameObject discardArea;

    private GameObject handArea;

    private GameObject selfPlayArea;
    private GameObject enemyPlayArea;
    private GameObject cardInfoArea;

    private GameObject cardPrefab;

    private GameObject curCard; // 当前需要异步处理的卡牌

    static PlaySceneManager() {}

    public static PlaySceneManager Instance
    {
        get
        {
            return instance;
        }
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
                AddCardToPlayArea(card);
                break;
            }
            case PlaySceneMsg.PlayCardFromHandArea: {
                GameObject card = (GameObject)list[0];
                handArea.GetComponent<HandArea>().RemoveCard(card);
                AddCardToPlayArea(card);
                break;
            }
            case PlaySceneMsg.FinishWithstandAttack: {
                Debug.Log("FinishWithstandAttack");
                enemyPlayArea.GetComponent<SinglePlayerArea>().FinishWithstandAttack();
                selfPlayArea.GetComponent<SinglePlayerArea>().AddNormalCard(curCard); // 不考虑同时是间谍牌和攻击牌的情况
                curCard = null;
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

    private void AddCardToPlayArea(GameObject card)
    {
        if (card.GetComponent<CardDisplay>().GetCardInfo().ability == CardAbility.Attack && ApplyAttackCard(card)) {
            curCard = card;
            return;
        }
        if (card.GetComponent<CardDisplay>().GetCardInfo().ability == CardAbility.Spy) {
            DrawCards(2);
            enemyPlayArea.GetComponent<SinglePlayerArea>().AddNormalCard(card);
        } else {
            selfPlayArea.GetComponent<SinglePlayerArea>().AddNormalCard(card);
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
}
