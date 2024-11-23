using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;

// 统一管理全局通知行为
public class PlaySceneManager
{
    private static string TAG = "PlaySceneManager";
    public enum PlaySceneMsg
    {
        StartGame, // 开始一场游戏
        ShowCardInfo, // 显示卡牌信息
        HideCardInfo, // 隐藏卡牌信息
        ShowDiscardArea, // 显示弃牌区
        HideDiscardArea, // 隐藏弃牌区
        ChooseCard, // 选择卡牌
    }

    private static readonly PlaySceneManager instance = new PlaySceneManager();

    private GameObject discardArea;

    private GameObject selfPlayArea;
    private GameObject enemyPlayArea;
    private GameObject cardInfoArea;

    private GameObject cardPrefab;

    private PlaySceneModel playSceneModel;

    static PlaySceneManager() {}

    private PlaySceneManager()
    {
        playSceneModel = new PlaySceneModel();
    }

    public static PlaySceneManager Instance
    {
        get
        {
            return instance;
        }
    }

    // 单例模式，每次启动一局游戏都需要调用Reset进行初始化
    public void Reset()
    {

    }

    public void HandleMessage(PlaySceneMsg msg, params object[] list)
    {
        if (selfPlayArea == null) {
            selfPlayArea = GameObject.Find("SelfPlayArea");
        }
        if (enemyPlayArea == null) {
            enemyPlayArea = GameObject.Find("EnemyPlayArea");
        }
        if (cardInfoArea == null) {
            cardInfoArea = GameObject.Find("CardInfoArea");
        }
        if (cardPrefab == null) {
            cardPrefab = Resources.Load<GameObject>("Prefabs/HalfCard");
        }
        if (discardArea == null) {
            discardArea = GameObject.Find("Canvas/Background/DiscardArea");
        }
        switch (msg)
        {
            case PlaySceneMsg.StartGame: {
                List<int> selfInfoIdList = (List<int>)list[0];
                List<int> enemyInfoIdList = (List<int>)list[1];
                playSceneModel.SetBackupCardInfoIdList(selfInfoIdList, enemyInfoIdList);
                playSceneModel.StartSet(true);
                InitViewModel();
                break;
            }
            case PlaySceneMsg.ShowCardInfo: {
                CardInfo info = (CardInfo)list[0];
                cardInfoArea.GetComponent<CardInfoAreaView>().ShowInfo(info);
                break;
            }
            case PlaySceneMsg.HideCardInfo: {
                cardInfoArea.GetComponent<CardInfoAreaView>().HideInfo();
                break;
            }
            case PlaySceneMsg.ShowDiscardArea: {
                DiscardAreaModel discardAreaModel = (DiscardAreaModel)list[0];
                bool isMedic = (bool)list[1];
                discardArea.GetComponent<DiscardAreaView>().ShowArea(discardAreaModel, isMedic);
                break;
            }
            case PlaySceneMsg.HideDiscardArea: {
                // TODO: medic时，不复活直接关闭需要通知转状态
                discardArea.GetComponent<DiscardAreaView>().HideArea();
                break;
            }
            case PlaySceneMsg.ChooseCard: {
                CardModel cardModel = (CardModel)list[0];
                if (playSceneModel.IsTurn(true)) {
                    // self turn时才允许选择
                    playSceneModel.ChooseCard(cardModel, true);
                    // TODO: 判断action state
                    UpdateUI();
                }
                break;
            }
        }
    }

    private void InitViewModel()
    {
        selfPlayArea.GetComponent<SinglePlayerAreaView>().model = playSceneModel.selfSinglePlayerAreaModel;
        enemyPlayArea.GetComponent<SinglePlayerAreaView>().model = playSceneModel.enemySinglePlayerAreaModel;
        UpdateUI();
    }

    private void UpdateUI()
    {
        selfPlayArea.GetComponent<SinglePlayerAreaView>().UpdateUI();
        enemyPlayArea.GetComponent<SinglePlayerAreaView>().UpdateUI();
    }
}
