using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 接收卡牌层面的用户输入，并进行model与view的调用处理
public class CardController : MonoBehaviour
{
    private CardModel cardModel;

    private CardView cardView;

    private static object cardIdLock = new object();

    private static int globalCardId = 1;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Init(CardInfo cardInfo)
    {
        // TODO: cardInfo深拷贝
        lock (cardIdLock) {
            cardInfo.id = globalCardId++;
        }
        cardModel = new CardModel(cardInfo);
        cardView = gameObject.GetComponent<CardView>();
        cardView.SetCardInfo(cardInfo);
    }

    public int GetId()
    {
        return cardModel.GetId();
    }

    // 点击事件
    public void OnClick()
    {

    }

    // 开始悬停
    public void OnHoverStart()
    {

    }

    // 结束悬停
    public void OnHoverEnd()
    {

    }
}
