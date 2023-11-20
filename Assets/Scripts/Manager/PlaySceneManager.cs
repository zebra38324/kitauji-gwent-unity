using System;
using System.Threading;
using System.Threading.Tasks;
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
    }

    private static readonly PlaySceneManager instance = new PlaySceneManager();

    private GameObject discardArea;

    private GameObject handArea;

    private GameObject selfPlayArea;
    private GameObject enemyPlayArea;

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

                bool isSelf = (bool)list[1]; // TODO: 间谍牌
                AddCardToPlayArea(card, isSelf);
                break;
            }
            case PlaySceneMsg.PlayCardFromHandArea: {
                if (handArea == null) {
                    handArea = GameObject.Find("HandArea");
                }
                GameObject card = (GameObject)list[0];
                handArea.GetComponent<HandArea>().RemoveCard(card);

                bool isSelf = (bool)list[1]; // TODO: 间谍牌
                if (card.GetComponent<CardDisplay>().GetCardInfo().ability == CardAbility.Attack && ApplyAttackCard(card)) {
                    curCard = card;
                    break;
                }
                AddCardToPlayArea(card, isSelf);
                break;
            }
            case PlaySceneMsg.FinishWithstandAttack: {
                Debug.Log("FinishWithstandAttack");
                enemyPlayArea.GetComponent<SinglePlayerArea>().FinishWithstandAttack();
                AddCardToPlayArea(curCard, true); // 不考虑同时是间谍牌和攻击牌的情况
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
        }
    }

    private void AddCardToPlayArea(GameObject card, bool isSelf)
    {
        if (isSelf) {
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
}
