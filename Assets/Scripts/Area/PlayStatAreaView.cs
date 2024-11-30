using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PlayStatAreaView : MonoBehaviour
{
    private static string TAG = "PlayStatAreaView";
    public GameObject frame;
    public GameObject playerName;
    public GameObject handCardNum;
    public GameObject live1;
    public GameObject live2;
    public GameObject scoreNum;
    public GameObject countDown;

    private PlaySceneModel playSceneModel { get; set; }

    private bool isSelf = true;

    private SinglePlayerAreaModel singlePlayerAreaModel { get; set; }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        UpdateCountDown();
    }

    public void Init(PlaySceneModel model, bool isSelfModel)
    {
        playSceneModel = model;
        isSelf = isSelfModel;
        singlePlayerAreaModel = isSelf ? playSceneModel.selfSinglePlayerAreaModel : playSceneModel.enemySinglePlayerAreaModel;
    }

    // 每次操作完，更新ui
    public void UpdateUI()
    {
        handCardNum.GetComponent<TextMeshProUGUI>().text = singlePlayerAreaModel.handRowAreaModel.cardList.Count.ToString();
        scoreNum.GetComponent<TextMeshProUGUI>().text = singlePlayerAreaModel.GetCurrentPower().ToString();
        if (playSceneModel.IsTurn(isSelf)) {
            frame.SetActive(true);
            countDown.SetActive(true);
        } else {
            frame.SetActive(false);
            countDown.SetActive(false);
        }
    }

    private void UpdateCountDown()
    {
        if (!playSceneModel.IsTurn(isSelf)) {
            return;
        }
        long remainSecond = (PlayStateTracker.TURN_TIME - (KTime.CurrentMill() - playSceneModel.tracker.stateChangeTs)) / 1000;
        countDown.GetComponent<TextMeshProUGUI>().text = remainSecond.ToString();
    }
}
