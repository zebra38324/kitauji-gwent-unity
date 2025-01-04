using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ActionToastAreaView : MonoBehaviour
{
    public GameObject actionToastBackground;

    public GameObject actionToastText;

    public ActionTextModel actionTextModel { get; set; }

    private long startShowTs = 0;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        UpdateToastText();
    }

    private void UpdateToastText()
    {
        if (actionTextModel.toastText == null) {
            if (KTime.CurrentMill() - startShowTs > 2000) {
                // 2s后消失
                actionToastBackground.SetActive(false);
            }
            return;
        }
        startShowTs = KTime.CurrentMill();
        actionToastText.GetComponent<TextMeshProUGUI>().text = actionTextModel.toastText;
        actionTextModel.toastText = null;
        actionToastBackground.SetActive(true);
    }
}
