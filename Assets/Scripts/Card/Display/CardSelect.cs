using UnityEngine;
using UnityEngine.EventSystems;

// 定义选择卡片后的相关操作
public class CardSelect : MonoBehaviour, IPointerClickHandler
{
    //public GameObject halfCard;

    private GameObject playArea;

    private GameObject handArea;

    private bool enableSelect = true; // 打出后，不再允许选中

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
        if (!enableSelect) {
            return;
        }
        Debug.Log("on click " + gameObject.GetComponent<CardDisplay>().cardInfo.englishName);

        // TODO: 目前先实现最简单的打出一张普通牌的功能
        PlayNormalCard();
    }

    private void PlayNormalCard()
    {
        // TODO 待优化
        handArea.GetComponent<HandArea>().RemoveCard(gameObject);
        GameObject singlePlayerArea;
        string areaName = "SelfPlayArea";
        singlePlayerArea = GameObject.Find(areaName);
        singlePlayerArea.GetComponent<SinglePlayerArea>().AddNormalCard(gameObject);
        enableSelect = false;
        gameObject.GetComponent<CardInfoDisplay>().SetIsCardUp(false);
    }
}
