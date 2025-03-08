using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayStatAreaView : MonoBehaviour
{
    public GameObject frame;
    public GameObject playerName;
    public GameObject handCardNum;
    public GameObject live1;
    public GameObject live2;
    public GameObject scoreNum;
    public GameObject countDown;

    public bool isAbort = false;

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
        playerName.GetComponent<TextMeshProUGUI>().text = string.Format("{0}", isSelf ? playSceneModel.tracker.selfName : playSceneModel.tracker.enemyName);
        StartCoroutine(InitPlayerName());
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
        UpdateSetScore();
    }

    private IEnumerator InitPlayerName()
    {
        while (!isAbort) {
            if (playSceneModel.tracker.curState == PlayStateTracker.State.WAIT_BACKUP_INFO) {
                yield return null;
            }
            playerName.GetComponent<TextMeshProUGUI>().text = string.Format("{0}（{1}）",
                isSelf ? playSceneModel.tracker.selfName : playSceneModel.tracker.enemyName,
                isSelf ? CardText.cardGroupText[(int)playSceneModel.tracker.selfGroup] : CardText.cardGroupText[(int)playSceneModel.tracker.enemyGroup]);
            yield break;
        }
    }

    private void UpdateCountDown()
    {
        if (isAbort) {
            return;
        }
        if (playSceneModel == null || playSceneModel.tracker == null || !playSceneModel.IsTurn(isSelf)) {
            return;
        }
        long remainSecond = (PlayStateTracker.TURN_TIME - (KTime.CurrentMill() - playSceneModel.tracker.stateChangeTs)) / 1000;
        countDown.GetComponent<TextMeshProUGUI>().text = remainSecond.ToString();
    }

    private void UpdateSetScore()
    {
        int setScore = isSelf ? playSceneModel.tracker.selfSetScore : playSceneModel.tracker.enemySetScore;
        if (setScore >= 1) {
            live1.GetComponent<Image>().sprite = KResources.Load<Sprite>(@"Image/texture/background/player_live_red");
        }
        if (setScore >= 2) {
            live2.GetComponent<Image>().sprite = KResources.Load<Sprite>(@"Image/texture/background/player_live_red");
        }
    }
}
