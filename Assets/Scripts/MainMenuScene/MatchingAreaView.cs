using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MatchingAreaView : MonoBehaviour
{
    private static string TAG = "MatchingAreaView";
    private static string DefaultText = "匹配中";
    private static string LoadingText = "。";
    private static int LoadingTextNum = 4;

    public GameObject matchingAreaText;

    public bool isMatching = false;

    private int curLoadingTextNum = 0;
    private long curLoadingTextNumLastTs = 0;

    private RoomManager roomManager = null;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (gameObject.activeSelf && KTime.CurrentMill() - curLoadingTextNumLastTs > 500) {
            curLoadingTextNumLastTs = KTime.CurrentMill();
            curLoadingTextNum = (curLoadingTextNum + 1) % LoadingTextNum;
            matchingAreaText.GetComponent<TextMeshProUGUI>().text = DefaultText;
            for (int i = 0; i < curLoadingTextNum; i++) {
                matchingAreaText.GetComponent<TextMeshProUGUI>().text += LoadingText;
            }
        }
    }

    public void ShowArea()
    {
        isMatching = true;
        gameObject.SetActive(true);
        StartMatch();
    }

    private void StartMatch()
    {
        roomManager = new RoomManager();
        roomManager.StartPVPMatch();
    }

    private void CancelMatch()
    {
        KLog.I(TAG, "CancelMatch");
        HideArea();
        roomManager.CancelPVPMatch();
        roomManager = null;
    }

    private void HideArea()
    {
        isMatching = false;
        gameObject.SetActive(false);
    }
}
