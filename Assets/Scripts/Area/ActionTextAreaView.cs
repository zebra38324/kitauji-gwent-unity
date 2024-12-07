using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ActionTextAreaView : MonoBehaviour
{
    public GameObject actionText;

    private Coroutine scrollCoroutine;

    // Start is called before the first frame update
    void Start()
    {
        actionText.GetComponent<TextMeshProUGUI>().text = "test";
        scrollCoroutine = StartCoroutine(ScrollText());
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // 滚动条在最下时，要保持最下
    private IEnumerator ScrollText()
    {
        while (true) {
            yield return new WaitForSeconds(3); // 每隔3秒添加新文本
            actionText.GetComponent<TextMeshProUGUI>().text = actionText.GetComponent<TextMeshProUGUI>().text + "\n test\n test\n test";
        }
    }
}
