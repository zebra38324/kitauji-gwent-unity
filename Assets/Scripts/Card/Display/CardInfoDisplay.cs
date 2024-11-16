using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CardInfoDisplay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private bool isCardUp = false; // 卡片是否是抬升状态
    private bool enableUp = false; // 是否允许抬升

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetIsCardUp(bool flag)
    {
        isCardUp = flag;
    }

    public void SetEnableUp(bool flag)
    {
        enableUp = flag;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isCardUp) {
            return;
        }
        isCardUp = true;
        if (enableUp) {
            transform.Translate(0, 10, 0); // 鼠标悬浮时，卡片上移
        }
        PlaySceneManager.Instance.HandleMessage(PlaySceneManager.PlaySceneMsg.ShowCardInfo, gameObject.GetComponent<CardDisplay>().GetCardInfo());
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (isCardUp && enableUp) {
            transform.Translate(0, -10, 0); // 鼠标悬移出时，卡片恢复
        }
        isCardUp = false;
        PlaySceneManager.Instance.HandleMessage(PlaySceneManager.PlaySceneMsg.HideCardInfo);
    }
}
