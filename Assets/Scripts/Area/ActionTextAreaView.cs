using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ActionTextAreaView : MonoBehaviour
{
    public GameObject actionText;

    public GameObject scrollBar;

    public ActionTextModel actionTextModel { get; set; }

    // Start is called before the first frame update
    void Start()
    {
        actionText.GetComponent<TextMeshProUGUI>().text = "";
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void UpdateUI()
    {
        if (actionText == null) {
            return;
        }
        actionText.GetComponent<TextMeshProUGUI>().text = actionTextModel.totalText;
        // 滚动条滑到最下处
        StartCoroutine(SetSrollBarBottom());
    }

    private IEnumerator SetSrollBarBottom()
    {
        // 不等待两帧的话，可能设置不生效
        yield return null;
        yield return null;
        scrollBar.GetComponent<Scrollbar>().value = 0;
    }
}
