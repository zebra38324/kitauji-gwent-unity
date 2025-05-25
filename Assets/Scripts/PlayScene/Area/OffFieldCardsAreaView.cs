using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// 弃牌区按钮与备选卡牌区的ui展示
public class OffFieldCardsAreaView : MonoBehaviour
{
    public GameObject backupCardNum;

    private HandCardAreaModel handCardAreaModel;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void UpdateModel(HandCardAreaModel model)
    {
        if (handCardAreaModel == model) {
            return;
        }
        handCardAreaModel = model;
        backupCardNum.GetComponent<TextMeshProUGUI>().text = handCardAreaModel.backupCardList.Count.ToString();
    }

    public void UpdateDiscardAreaView()
    {
        PlaySceneManager.Instance.HandleMessage(SceneMsg.ShowDiscardArea, handCardAreaModel.isSelf, false);
    }
}
