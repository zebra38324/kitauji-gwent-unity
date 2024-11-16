using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using static PlaySceneManager;

/**
 * 实现卡牌可能的动作
 * 1. 手牌区主动打出
 * 2. 手牌区被动打出
 * 3. 弃牌区打出
 * 4. 备选卡牌区打出
 * 5. 成为可攻击对象（出现高亮边框）
 * 6. 被攻击
 * 7. 展示详细信息
 */
public class CardAction : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerClickHandler
{
    private static string TAG = "CardAction";

    // 悬停时卡片上移距离
    private static int hoverUpDistance = 10;

    private bool isHovering = false; // 鼠标是否在ui内

    private bool isCardInfoShowing = false; // 当前card info正处于展示状态

    private CardLocation cardLocation_ = CardLocation.None;

    public CardLocation cardLocation {
        get {
            return cardLocation_;
        }
        set {
            cardLocation_ = value;
            if (cardLocation_ == CardLocation.HandArea) {
                selectType = CardSelectType.HandCard;
            } else {
                selectType = CardSelectType.None;
            }
        }
    }

    private CardSelectType selectType_ = CardSelectType.None;
    public CardSelectType selectType {
        get {
            return selectType_;
        }
        set {
            selectType_ = value;
            if (selectType_ == CardSelectType.WithstandAttack) {
                gameObject.GetComponent<CardDisplay>().SetFrameVisible(true);
            } else {
                gameObject.GetComponent<CardDisplay>().SetFrameVisible(false);
            }
        }
    }

    private int beAttackedNum = 0; // 可能被攻击的数值

    // Start is called before the first frame update
    void Start()
    {
        PlaySceneManager.Instance.CardBoardcast += new PlaySceneManager.CardBoardcastDelegate(ReceiveCardBoardcast);
    }

    // Update is called once per frame
    void Update()
    {
        JudgeShowCardInfo();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovering = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;
        // hide如果也放在update里，可能造成下一卡片已经展示info，然后info区域被本卡牌关掉
        JudgeHideCardInfo();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        KLog.I(TAG, "on click " + gameObject.GetComponent<CardDisplay>().GetCardInfo().chineseName);
        switch (selectType) {
            case CardSelectType.HandCard: {
                PlayCard();
                break;
            }
            case CardSelectType.MedicDiscardCard: {
                PlayCard();
                break;
            }
            case CardSelectType.WithstandAttack: {
                if (beAttackedNum == 2) {
                    gameObject.GetComponent<CardDisplay>().AddBuff(CardBuffType.Attack2, 1);
                } else {
                    gameObject.GetComponent<CardDisplay>().AddBuff(CardBuffType.Attack4, 1); // TODO: 这里需要优化
                }
                if (gameObject.GetComponent<CardDisplay>().IsDead()) {
                    // 移除卡牌
                    PlaySceneManager.Instance.HandleMessage(PlaySceneManager.PlaySceneMsg.RemoveSingleCard, gameObject, false);
                }
                PlaySceneManager.Instance.HandleMessage(PlaySceneManager.PlaySceneMsg.FinishWithstandAttack, gameObject);
                break;
            }
            default: {
                break;
            }
        }
    }

    // 打出牌到对战区
    public void PlayCard()
    {
        PlaySceneManager.Instance.HandleMessage(PlaySceneManager.PlaySceneMsg.PlayCard, gameObject);
    }

    private int ReceiveCardBoardcast(CardBoardcastType cardBoardcastType, params object[] list)
    {
        switch (cardBoardcastType) {
            case CardBoardcastType.CountBond: {
                string bondType = (string)list[0];
                if (cardLocation == CardLocation.SelfBattleArea && gameObject.GetComponent<CardDisplay>().GetCardInfo().bondType == bondType) {
                    return 1;
                }
                break;
            }
            case CardBoardcastType.UpdateBond: {
                string bondType = (string)list[0];
                int count = (int)list[1];
                if (cardLocation == CardLocation.SelfBattleArea && gameObject.GetComponent<CardDisplay>().GetCardInfo().bondType == bondType) {
                    gameObject.GetComponent<CardDisplay>().SetBuff(CardBuffType.Bond, count - 1); // buff数量排除自身
                }
                break;
            }
            case CardBoardcastType.Tunning: {
                if (cardLocation == CardLocation.SelfBattleArea) {
                    gameObject.GetComponent<CardDisplay>().RemoveNormalDebuff();
                }
                break;
            }
            case CardBoardcastType.WillWithstandAttack: {
                if (cardLocation == CardLocation.EnemyBattleArea && gameObject.GetComponent<CardDisplay>().GetCardInfo().cardType == CardType.Normal) {
                    selectType = CardSelectType.WithstandAttack;
                    beAttackedNum = (int)list[0];
                    return 1;
                }
                break;
            }
            case CardBoardcastType.FinishAttack: {
                if (selectType == CardSelectType.WithstandAttack) {
                    selectType = CardSelectType.None;
                    beAttackedNum = 0;
                }
                break;
            }
            default: {
                break;
            }
        }
        return 0;
    }

    // 判断悬停时是否需要卡牌上移
    private bool HoverNeedUp()
    {
        return cardLocation == CardLocation.HandArea;
    }
    
    // 判断是否需要显示info
    private void JudgeShowCardInfo()
    {
        if (!isHovering || isCardInfoShowing) {
            return;
        }
        // 将鼠标屏幕坐标转换为RectTransform的局部坐标
        Vector2 localMousePos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(gameObject.GetComponent<RectTransform>(), Input.mousePosition, null, out localMousePos);
        Rect cardRect = gameObject.GetComponent<RectTransform>().rect;
        Rect excludeRect = new Rect(cardRect.position.x,
                cardRect.position.y,
                cardRect.size.x,
                cardRect.size.y / 10);
        if (excludeRect.Contains(localMousePos)) {
            return; // 避免卡牌上下鬼畜，靠下侧width*(height/10)区域不进行响应
        }
        if (HoverNeedUp()) {
            transform.Translate(0, hoverUpDistance, 0); // 卡片上移
        }
        isCardInfoShowing = true;
        PlaySceneManager.Instance.HandleMessage(PlaySceneManager.PlaySceneMsg.ShowCardInfo, gameObject.GetComponent<CardDisplay>().GetCardInfo());
    }

    // 判断是否需要隐藏info
    private void JudgeHideCardInfo()
    {
        if (!isCardInfoShowing) {
            return;
        }
        if (HoverNeedUp()) {
            transform.Translate(0, -hoverUpDistance, 0); // 卡片下移恢复
        }
        isCardInfoShowing = false;
        PlaySceneManager.Instance.HandleMessage(PlaySceneManager.PlaySceneMsg.HideCardInfo);
    }
}
