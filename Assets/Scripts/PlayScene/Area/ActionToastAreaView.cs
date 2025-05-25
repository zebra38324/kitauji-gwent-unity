using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;

public class ActionToastAreaView : MonoBehaviour
{
    public GameObject actionToastBackground;

    public GameObject actionToastText;

    public string showText = null;

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

    public void UpdateToastText()
    {
        if (showText == null) {
            if (KTime.CurrentMill() - startShowTs > 2000) {
                // 2s后消失
                actionToastBackground.SetActive(false);
            }
            return;
        }
        startShowTs = KTime.CurrentMill();
        actionToastText.GetComponent<TextMeshProUGUI>().text = showText;
        showText = null;
        actionToastBackground.SetActive(true);
    }
}
