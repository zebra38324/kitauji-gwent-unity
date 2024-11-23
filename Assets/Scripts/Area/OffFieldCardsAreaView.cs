using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// 弃牌区按钮与备选卡牌区的ui展示
public class OffFieldCardsAreaView : MonoBehaviour
{
    public GameObject backupCardNum;
    public SinglePlayerAreaModel model { get; set; }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void UpdateDiscardAreaView()
    {
        PlaySceneManager.Instance.HandleMessage(PlaySceneManager.PlaySceneMsg.ShowDiscardArea, model.discardAreaModel, false);
    }

    public void UpdateUI()
    {
        backupCardNum.GetComponent<TextMeshProUGUI>().text = model.backupCardList.Count.ToString();
    }
}
