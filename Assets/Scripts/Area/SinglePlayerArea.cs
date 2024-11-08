using System.Collections.Generic;
using TMPro;
using UnityEngine;

// 包含三行打出的牌，及分数区域
public class SinglePlayerArea : MonoBehaviour
{
    private static string TAG = "SinglePlayerArea";
    public GameObject woodRow;
    public GameObject brassRow;
    public GameObject percussionRow;
    public GameObject scoreNum;
    public GameObject enemyArea;
    public GameObject cardPrefab;
    public GameObject discardArea;

    // Start is called before the first frame update
    void Start()
    {
        woodRow.GetComponent<RowArea>().ScoreChangeNotify += new RowArea.ScoreChangeNotifyHandler(ScoreUpdate);
        brassRow.GetComponent<RowArea>().ScoreChangeNotify += new RowArea.ScoreChangeNotifyHandler(ScoreUpdate);
        percussionRow.GetComponent<RowArea>().ScoreChangeNotify += new RowArea.ScoreChangeNotifyHandler(ScoreUpdate);
    }

    void ScoreUpdate(int diff)
    {
        int currentScore = int.Parse(scoreNum.GetComponent<TextMeshProUGUI>().text);
        scoreNum.GetComponent<TextMeshProUGUI>().text = (currentScore + diff).ToString();
    }

    // return: 这张牌打出后，是否需要等待玩家的进一步操作
    public bool AddNormalCard(GameObject newCard)
    {
        GameObject targetArea = GetTargetArea(newCard);
        if (targetArea == null) {
            return false;
        }
        targetArea.GetComponent<RowArea>().AddNormalCard(newCard);

        // 设置buff
        if (newCard.GetComponent<CardDisplay>().GetCardInfo().ability == CardAbility.Tunning) {
            targetArea.GetComponent<RowArea>().RemoveNormalDebuff();
        } else if (newCard.GetComponent<CardDisplay>().GetCardInfo().ability == CardAbility.Bond) {
            UpdateBondBuff(newCard.GetComponent<CardDisplay>().GetCardInfo().bondType);
        } else if (newCard.GetComponent<CardDisplay>().GetCardInfo().ability == CardAbility.ScorchWood) {
            enemyArea.GetComponent<SinglePlayerArea>().ScorchWood();
        } else if (newCard.GetComponent<CardDisplay>().GetCardInfo().ability == CardAbility.Medic) {
            return ApplyMedic();
        }
        return true;
    }

    private GameObject GetTargetArea(GameObject newCard)
    {
        GameObject targetArea;
        switch (newCard.GetComponent<CardDisplay>().GetCardInfo().badgeType) {
            case CardBadgeType.Wood: {
                targetArea = woodRow;
                break;
            }
            case CardBadgeType.Brass: {
                targetArea = brassRow;
                break;
            }
            case CardBadgeType.Percussion: {
                targetArea = percussionRow;
                break;
            }
            default: {
                KLog.E(TAG, "badgeType error = " + newCard.GetComponent<CardDisplay>().GetCardInfo().badgeType);
                return null;
            }
        }
        return targetArea;
    }

    public void ScorchWood()
    {
        woodRow.GetComponent<RowArea>().ScorchWood();
    }

    private void UpdateBondBuff(string bondType)
    {
        int times = woodRow.GetComponent<RowArea>().GetBondCardNum(bondType);
        times += brassRow.GetComponent<RowArea>().GetBondCardNum(bondType);
        times += percussionRow.GetComponent<RowArea>().GetBondCardNum(bondType);

        woodRow.GetComponent<RowArea>().UpdateBondBuff(bondType, times);
        brassRow.GetComponent<RowArea>().UpdateBondBuff(bondType, times);
        percussionRow.GetComponent<RowArea>().UpdateBondBuff(bondType, times);
    }

    public void ClearCard(DiscardCardManager manager)
    {
        woodRow.GetComponent<RowArea>().ClearCard(manager);
        brassRow.GetComponent<RowArea>().ClearCard(manager);
        percussionRow.GetComponent<RowArea>().ClearCard(manager);
    }

    private bool ApplyMedic()
    {
        List<GameObject> targetList = SelfDiscardCardManager.Instance.GetCardList();
        List<GameObject> invalid = new List<GameObject>();
        foreach (GameObject card in targetList) {
            if (card.GetComponent<CardDisplay>().GetCardInfo().cardType == CardType.Hero) {
                invalid.Add(card);
            } else {
                card.GetComponent<CardAction>().cardLocation = CardLocation.DiscardArea;
            }
        }
        foreach (GameObject card in invalid) {
            targetList.Remove(card);
        }
        if (targetList.Count == 0) {
            // 无可选卡牌，不显示弃牌区
            return false;
        }
        discardArea.GetComponent<DiscardArea>().ShowArea(targetList, true);
        return true;
    }

    public int ReadyEmbraceAttack(int num)
    {
        int count = 0;
        count += woodRow.GetComponent<RowArea>().ReadyEmbraceAttack(num);
        count += brassRow.GetComponent<RowArea>().ReadyEmbraceAttack(num);
        count += percussionRow.GetComponent<RowArea>().ReadyEmbraceAttack(num);
        return count;
    }

    public void FinishWithstandAttack()
    {
        woodRow.GetComponent<RowArea>().FinishWithstandAttack();
        brassRow.GetComponent<RowArea>().FinishWithstandAttack();
        percussionRow.GetComponent<RowArea>().FinishWithstandAttack();
    }

    public void RemoveSingleCard(GameObject card)
    {
        woodRow.GetComponent<RowArea>().RemoveSingleCard(card);
        brassRow.GetComponent<RowArea>().RemoveSingleCard(card);
        percussionRow.GetComponent<RowArea>().RemoveSingleCard(card);
        // 更新buff
        if (card.GetComponent<CardDisplay>().GetCardInfo().ability == CardAbility.Bond) {
            UpdateBondBuff(card.GetComponent<CardDisplay>().GetCardInfo().bondType);
        }
    }
}
