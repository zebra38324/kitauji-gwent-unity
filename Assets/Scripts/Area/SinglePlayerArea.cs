using System.Collections.Generic;
using TMPro;
using UnityEngine;

// 包含三行打出的牌，及分数区域
public class SinglePlayerArea : MonoBehaviour
{
    public GameObject woodRow;
    public GameObject brassRow;
    public GameObject percussionRow;
    public GameObject scoreNum;
    public GameObject enemyArea;
    public GameObject cardPrefab;

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

    public void AddNormalCard(GameObject newCard)
    {
        GameObject targetArea = GetTargetArea(newCard);
        if (targetArea == null) {
            return;
        }
        targetArea.GetComponent<RowArea>().AddNormalCard(newCard);

        // 设置buff
        if (newCard.GetComponent<CardDisplay>().GetCardInfo().ability == CardAbility.Tunning) {
            targetArea.GetComponent<RowArea>().ClearNormalDebuff();
        } else if (newCard.GetComponent<CardDisplay>().GetCardInfo().ability == CardAbility.Bond) {
            UpdateBondBuff(newCard.GetComponent<CardDisplay>().GetCardInfo().bondType);
        } else if (newCard.GetComponent<CardDisplay>().GetCardInfo().ability == CardAbility.ScorchWood) {
            enemyArea.GetComponent<SinglePlayerArea>().ScorchWood();
        } else if (newCard.GetComponent<CardDisplay>().GetCardInfo().ability == CardAbility.Muster) {
            ApplyMuster(newCard.GetComponent<CardDisplay>().GetCardInfo().musterType);
        }
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
                Debug.LogError("badgeType error = " + newCard.GetComponent<CardDisplay>().GetCardInfo().badgeType);
                return null;
            }
        }
        return targetArea;
    }

    private void ApplyMuster(string musterType)
    {
        // 备选卡牌中拉取
        List<CardInfo> cardInfos = BackupCardManager.Instance.GetCardInfosWithMusterType(musterType);
        foreach (CardInfo info in cardInfos) {
            GameObject musterCard = GameObject.Instantiate(cardPrefab, null);
            musterCard.GetComponent<CardDisplay>().SetCardInfo(info);
            GameObject target = GetTargetArea(musterCard);
            if (target == null) {
                return;
            }
            target.GetComponent<RowArea>().AddNormalCard(musterCard);
        }

        // 手牌区拉取
        GameObject handArea;
        string areaName = "HandArea";
        handArea = GameObject.Find(areaName);
        handArea.GetComponent<HandArea>().PlayMusterCard(musterType);
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
}
