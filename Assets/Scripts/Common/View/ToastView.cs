using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ToastView : MonoBehaviour
{
    public GameObject toastViewBackground;

    public GameObject toastViewText;

    private long startShowTs = 0;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (KTime.CurrentMill() - startShowTs > 2000) {
            // 2s后消失
            toastViewBackground.SetActive(false);
        }
        return;
    }

    public void ShowToast(string toast)
    {
        startShowTs = KTime.CurrentMill();
        toastViewText.GetComponent<TextMeshProUGUI>().text = toast;
        toastViewBackground.SetActive(true);
    }
}
