using UnityEngine;
using UnityEngine.EventSystems;

// 定义选择卡片后的相关操作
public class CardSelect : MonoBehaviour, IPointerClickHandler
{
    private GameObject playArea;

    private GameObject handArea;

    public bool enableDiscardSelect { get; set; } // 弃牌状态是否允许选择

    // Start is called before the first frame update
    void Start()
    {
        playArea = GameObject.Find("PlayArea");
        handArea = GameObject.Find("HandArea");
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("on click " + gameObject.GetComponent<CardDisplay>().GetCardInfo().englishName);
        switch (gameObject.GetComponent<CardDisplay>().GetStatus())
        {
            case CardStatus.Hand: {
                // TODO: 目前先实现最简单的打出一张普通牌的功能
                PlayNormalCard();
                break;
            }
            case CardStatus.Discard: {
                if (enableDiscardSelect) {
                    PlaySceneManager.Instance.HandleMessage(PlaySceneManager.PlaySceneMsg.MedicSelectDiscardCard, gameObject);
                    PlayNormalCard(); // 这里顺序不能调整
                }
                break;
            }
        }
    }

    public void PlayPassively()
    {
        PlayNormalCard();
    }

    private void PlayNormalCard()
    {
        // TODO 待优化
        handArea.GetComponent<HandArea>().RemoveCard(gameObject); // TODO handarea
        GameObject singlePlayerArea;
        string areaName = "SelfPlayArea";
        singlePlayerArea = GameObject.Find(areaName);
        singlePlayerArea.GetComponent<SinglePlayerArea>().AddNormalCard(gameObject);
        gameObject.GetComponent<CardInfoDisplay>().SetIsCardUp(false);
        gameObject.GetComponent<CardInfoDisplay>().SetEnableUp(false);
    }
}
