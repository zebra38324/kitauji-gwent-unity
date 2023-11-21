using UnityEngine;
using UnityEngine.EventSystems;

// 定义选择卡片后的相关操作
public class CardSelect : MonoBehaviour, IPointerClickHandler
{
    private GameObject playArea;

    private GameObject handArea;

    private CardSelectType selectType_;
    public CardSelectType selectType
    {
        get
        {
            return selectType_;
        }
        set
        {
            selectType_ = value;
            if (selectType_ == CardSelectType.WithstandAttack) {
                gameObject.GetComponent<CardDisplay>().SetFrameVisible(true);
            } else {
                gameObject.GetComponent<CardDisplay>().SetFrameVisible(false);
            }
        } 
    }
    public int attackNum { get; set; } // 即将遭受的攻击数值

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
        switch (selectType)
        {
            case CardSelectType.HandCard: {
                // TODO: 目前先实现最简单的打出一张普通牌的功能
                PlaySceneManager.Instance.HandleMessage(PlaySceneManager.PlaySceneMsg.PlayCardFromHandArea, gameObject, true);
                PlayNormalCard();
                break;
            }
            case CardSelectType.MedicDiscardCard: {
                PlaySceneManager.Instance.HandleMessage(PlaySceneManager.PlaySceneMsg.MedicSelectDiscardCard, gameObject, true);
                PlayNormalCard(); // 这里顺序不能调整
                break;
            }
            case CardSelectType.WithstandAttack: {
                gameObject.GetComponent<CardDisplay>().SetBuffAddMinus(-attackNum);
                if (gameObject.GetComponent<CardDisplay>().GetCurrentPower() < 0 ||
                    (gameObject.GetComponent<CardDisplay>().GetCurrentPower() == 0 && gameObject.GetComponent<CardDisplay>().GetCardInfo().originPower > 0)) {
                    // 点数小于0，移除卡牌。等于0时需判断原点数是否为0
                    PlaySceneManager.Instance.HandleMessage(PlaySceneManager.PlaySceneMsg.RemoveSingleCard, gameObject, false);
                }
                gameObject.GetComponent<CardDisplay>().SetFrameVisible(false);
                PlaySceneManager.Instance.HandleMessage(PlaySceneManager.PlaySceneMsg.FinishWithstandAttack);
                break;
            }
        }
    }

    public void PlayPassively()
    {
        PlaySceneManager.Instance.HandleMessage(PlaySceneManager.PlaySceneMsg.PlayCardFromHandArea, gameObject, true);
        PlayNormalCard();
    }

    private void PlayNormalCard()
    {
        gameObject.GetComponent<CardInfoDisplay>().SetIsCardUp(false);
        gameObject.GetComponent<CardInfoDisplay>().SetEnableUp(false);
        selectType = CardSelectType.None;
    }
}
