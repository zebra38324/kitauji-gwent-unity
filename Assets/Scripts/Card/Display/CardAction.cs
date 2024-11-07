using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

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
    // 悬停时卡片上移距离
    private static int hoverUpDistance = 10;

    private bool isHovering = false; // 鼠标是否在ui内

    private bool isCardInfoShowing = false; // 当前card info正处于展示状态

    private bool enableSelect = false; // 当前卡牌是否允许被选择

    public CardLocation cardLocation { get; set; }

    // Start is called before the first frame update
    void Start()
    {
        PlaySceneManager.Instance.CardEnableSelect += new PlaySceneManager.CardEnableSelectDelegate(EnableSelect);
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

    }

    // 打出牌到对战区
    public void PlayCard()
    {
        
    }

    public void EnableSelect(bool enable)
    {
        enableSelect = enable;
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
