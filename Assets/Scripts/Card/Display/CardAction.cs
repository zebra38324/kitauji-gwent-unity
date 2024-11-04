using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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
public class CardAction : MonoBehaviour
{
    // 悬停时卡片上移距离
    private static int hoverUpDistance = 10;

    private bool isHovering = false;



    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        HandleHover();
    }

    // 处理悬停操作
    private void HandleHover()
    {
        // 将鼠标屏幕坐标转换为RectTransform的局部坐标
        Vector2 localMousePos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(gameObject.GetComponent<RectTransform>(), Input.mousePosition, null, out localMousePos);
        if (isHovering) {
            JudgeHoverEnd(localMousePos);
        } else {
            JudgeHoverStart(localMousePos);
        }
    }

    // 判断鼠标是否进入设定区域 TODO: 卡牌覆盖
    private void JudgeHoverStart(Vector2 localMousePos)
    {
        if (!gameObject.GetComponent<RectTransform>().rect.Contains(localMousePos)) {
            return; // 没进入卡牌ui区域
        }
        if (HoverNeedUp()) {
            transform.Translate(0, hoverUpDistance, 0); // 卡片上移
        }
        isHovering = true;
        PlaySceneManager.Instance.HandleMessage(PlaySceneManager.PlaySceneMsg.ShowCardInfo, gameObject.GetComponent<CardDisplay>().GetCardInfo());
    }

    // 判断鼠标是否离开设定区域
    private void JudgeHoverEnd(Vector2 localMousePos)
    {
        if (gameObject.GetComponent<RectTransform>().rect.Contains(localMousePos)) {
            return; // 没离开卡牌ui区域
        }
        if (HoverNeedUp()) {
            Rect cardRect = gameObject.GetComponent<RectTransform>().rect;
            Rect extendRect = new Rect(cardRect.position.x,
                cardRect.position.y - cardRect.size.y / 5,
                cardRect.size.x,
                cardRect.size.y / 5);
            if (extendRect.Contains(localMousePos)) {
                // 卡牌上移后，需要判断卡下方width*(height/5)区域
                // 避免鼠标处于这个区域中时，导致卡牌疯狂上下鬼畜
                return;
            }
            transform.Translate(0, -hoverUpDistance, 0); // 卡片下移恢复
        }
        isHovering = false;
        PlaySceneManager.Instance.HandleMessage(PlaySceneManager.PlaySceneMsg.HideCardInfo);
    }

    // 判断悬停时是否需要卡牌上移 TODO: 完善
    private bool HoverNeedUp()
    {
        return true;
    }
}
