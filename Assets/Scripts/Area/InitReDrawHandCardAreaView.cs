using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// 初始化重抽手牌区域的ui展示
public class InitReDrawHandCardAreaView: MonoBehaviour
{
    private static readonly string defaultTip = "请在{0:D2}秒内选择0-2张手牌重新抽取";
    private static readonly string waitTip = "请等待对方完成选择";
    public static readonly int MAX_TIME = 30000; // 30秒内完成选择

    public GameObject cardRow;
    public GameObject tipText;
    public GameObject confirmButton;
    private SinglePlayerAreaModel model_;
    public SinglePlayerAreaModel model {
        get {
            return model_;
        }
        set {
            model_ = value;
            cardRow.GetComponent<RowAreaView>().rowAreaModel = model_.initHandRowAreaModel;
            tipText.GetComponent<TextMeshProUGUI>().text = string.Format(defaultTip, 30);
            startTs = KTime.CurrentMill();
            gameObject.SetActive(true);
        }
    }

    public long startTs { get; private set; }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        UpdateCountDown();
    }

    public void ConfirmButton()
    {
        confirmButton.SetActive(false);
        tipText.GetComponent<TextMeshProUGUI>().text = waitTip;
        PlaySceneManager.Instance.HandleMessage(PlaySceneManager.PlaySceneMsg.ReDrawInitHandCard);
    }

    public void UpdateUI()
    {
        if (!gameObject.activeSelf) {
            return;
        }
        cardRow.GetComponent<RowAreaView>().UpdateUI();
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    private void UpdateCountDown()
    {
        if (!confirmButton.activeSelf) {
            return;
        }
        long remainSecond = (MAX_TIME - (KTime.CurrentMill() - startTs)) / 1000;
        tipText.GetComponent<TextMeshProUGUI>().text = string.Format(defaultTip, remainSecond);
    }
}
