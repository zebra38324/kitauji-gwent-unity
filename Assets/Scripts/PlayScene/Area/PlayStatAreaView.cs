using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

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

    private WholeAreaModel wholeAreaModel;

    private bool isSelf = true;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        UpdateCountDown();
    }

    public void UpdateModel(WholeAreaModel model, bool isSelf_)
    {
        isSelf = isSelf_;
        if (wholeAreaModel == model) {
            return;
        }
        bool isFirstSet = wholeAreaModel == null;
        wholeAreaModel = model;
        if (isFirstSet) {
            playerName.GetComponent<TextMeshProUGUI>().text = string.Format("{0}", isSelf ? wholeAreaModel.playTracker.selfPlayerInfo.name : wholeAreaModel.playTracker.enemyPlayerInfo.name);
            StartCoroutine(InitPlayerName());
            return;
        }
        var singlePlayerAreaModel = isSelf ? wholeAreaModel.selfSinglePlayerAreaModel : wholeAreaModel.enemySinglePlayerAreaModel;
        handCardNum.GetComponent<TextMeshProUGUI>().text = singlePlayerAreaModel.handCardAreaModel.handCardListModel.cardList.Count.ToString();
        scoreNum.GetComponent<TextMeshProUGUI>().text = singlePlayerAreaModel.GetCurrentPower().ToString();
        if (wholeAreaModel.EnableChooseCard(isSelf)) {
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
            if (wholeAreaModel.gameState.curState == GameState.State.WAIT_BACKUP_INFO) {
                yield return null;
                continue;
            }
            var playerInfo = isSelf ? wholeAreaModel.playTracker.selfPlayerInfo : wholeAreaModel.playTracker.enemyPlayerInfo;
            playerName.GetComponent<TextMeshProUGUI>().text = string.Format("{0}（{1}）", playerInfo.name, CardText.cardGroupText[(int)playerInfo.cardGroup]);
            yield break;
        }
    }

    private void UpdateCountDown()
    {
        if (isAbort) {
            return;
        }
        if (wholeAreaModel == null) {
            return;
        }
        long remainSecond = (GameState.TURN_TIME - (KTime.CurrentMill() - wholeAreaModel.gameState.stateChangeTs)) / 1000;
        countDown.GetComponent<TextMeshProUGUI>().text = remainSecond.ToString();
    }

    private void UpdateSetScore()
    {
        int setScore = isSelf ? wholeAreaModel.playTracker.selfPlayerInfo.setScore : wholeAreaModel.playTracker.enemyPlayerInfo.setScore;
        if (setScore >= 1) {
            live1.GetComponent<Image>().sprite = KResources.Load<Sprite>(@"Image/texture/background/player_live_red");
        }
        if (setScore >= 2) {
            live2.GetComponent<Image>().sprite = KResources.Load<Sprite>(@"Image/texture/background/player_live_red");
        }
    }
}
