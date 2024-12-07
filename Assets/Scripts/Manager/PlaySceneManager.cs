using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;

// PlayScene统一管理器
public class PlaySceneManager : MonoBehaviour
{
    private static string TAG = "PlaySceneManager";

    static PlaySceneManager() { }

    private PlaySceneManager() { }

    public static PlaySceneManager Instance;

    public enum PlaySceneMsg
    {
        ShowCardInfo, // 显示卡牌信息
        HideCardInfo, // 隐藏卡牌信息
        ShowDiscardArea, // 显示弃牌区
        HideDiscardArea, // 隐藏弃牌区
        ChooseCard, // 选择卡牌
        ClickPass, // 点击pass按钮
        ReDrawInitHandCard, // 重抽初始手牌
    }

    private GameObject discardArea;

    private GameObject selfPlayArea;
    private GameObject enemyPlayArea;
    private GameObject cardInfoArea;
    private GameObject initReDrawHandCardAreaView;

    private GameObject cardPrefab;

    private PlaySceneModel playSceneModel;

    private PlaySceneAI playSceneAI;

    void Awake()
    {
        KLog.I(TAG, "will init PlaySceneManager");
        Instance = this;
        Init();
        KLog.I(TAG, "finish init PlaySceneManager");
    }

    // 每场比赛初始化
    public void Init()
    {
        playSceneModel = new PlaySceneModel();
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
        if (initReDrawHandCardAreaView == null) {
            initReDrawHandCardAreaView = GameObject.Find("Canvas/Background/InitReDrawHandCardArea");
        }

        // 配置AI模块
        playSceneAI = new PlaySceneAI();
        playSceneAI.playSceneModel.battleModel.SendToEnemyFunc += playSceneModel.battleModel.AddEnemyActionMsg;
        playSceneModel.battleModel.SendToEnemyFunc += playSceneAI.playSceneModel.battleModel.AddEnemyActionMsg;
        playSceneAI.Start();

        // playSceneModel初始化
        List<int> selfInfoIdList = KConfig.Instance.GetBattleCardInfoIdList();
        playSceneModel.SetBackupCardInfoIdList(selfInfoIdList);
        InitViewModel();
        StartCoroutine(StartGame());
    }

    public void HandleMessage(PlaySceneMsg msg, params object[] list)
    {
        switch (msg)
        {
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
                if (playSceneModel.tracker.actionState == PlayStateTracker.ActionState.MEDICING && playSceneModel.tracker.curState == PlayStateTracker.State.WAIT_SELF_ACTION) {
                    // medic时，不复活直接关闭需要通知转状态
                    playSceneModel.InterruptAction();
                }
                discardArea.GetComponent<DiscardAreaView>().HideArea();
                break;
            }
            case PlaySceneMsg.ChooseCard: {
                CardModel cardModel = (CardModel)list[0];
                if (playSceneModel.EnableChooseCard(true)) {
                    // self turn时才允许选择
                    playSceneModel.ChooseCard(cardModel);
                    // 复活技能流程
                    if (playSceneModel.tracker.actionState == PlayStateTracker.ActionState.MEDICING) {
                        HandleMessage(PlaySceneMsg.ShowDiscardArea, playSceneModel.selfSinglePlayerAreaModel.discardAreaModel, true);
                    } else if (discardArea.GetComponent<DiscardAreaView>().model != null) {
                        HandleMessage(PlaySceneMsg.HideDiscardArea);
                    }
                    UpdateUI();
                }
                break;
            }
            case PlaySceneMsg.ClickPass: {
                if (playSceneModel.IsTurn(true)) {
                    // self turn时才允许选择
                    playSceneModel.Pass();
                    UpdateUI();
                }
                break;
            }
            case PlaySceneMsg.ReDrawInitHandCard: {
                playSceneModel.ReDrawInitHandCard();
                break;
            }
        }
    }

    private IEnumerator StartGame()
    {
        while (playSceneModel.tracker.curState != PlayStateTracker.State.WAIT_INIT_HAND_CARD) {
            yield return null;
        }
        playSceneModel.DrawInitHandCard();
        UpdateUI();
        while (playSceneModel.tracker.curState != PlayStateTracker.State.WAIT_SELF_ACTION &&
               playSceneModel.tracker.curState != PlayStateTracker.State.WAIT_ENEMY_ACTION) {
            if (KTime.CurrentMill() - initReDrawHandCardAreaView.GetComponent<InitReDrawHandCardAreaView>().startTs > InitReDrawHandCardAreaView.MAX_TIME) {
                // 超时未操作，结束重抽手牌
                KLog.I(TAG, "initReDrawHandCard: self action timeout");
                initReDrawHandCardAreaView.GetComponent<InitReDrawHandCardAreaView>().ConfirmButton();
            }
            yield return null;
        }
        UpdateUI();
        StartCoroutine(ListenModel());
    }

    private void InitViewModel()
    {
        selfPlayArea.GetComponent<SinglePlayerAreaView>().model = playSceneModel.selfSinglePlayerAreaModel;
        enemyPlayArea.GetComponent<SinglePlayerAreaView>().model = playSceneModel.enemySinglePlayerAreaModel;
        selfPlayArea.GetComponent<SinglePlayerAreaView>().playStat.GetComponent<PlayStatAreaView>().Init(playSceneModel, true);
        enemyPlayArea.GetComponent<SinglePlayerAreaView>().playStat.GetComponent<PlayStatAreaView>().Init(playSceneModel, false);
        initReDrawHandCardAreaView.GetComponent<InitReDrawHandCardAreaView>().model = playSceneModel.selfSinglePlayerAreaModel;
        UpdateUI();
    }

    private void UpdateUI()
    {
        selfPlayArea.GetComponent<SinglePlayerAreaView>().UpdateUI();
        enemyPlayArea.GetComponent<SinglePlayerAreaView>().UpdateUI();
        initReDrawHandCardAreaView.GetComponent<InitReDrawHandCardAreaView>().UpdateUI();
        if (playSceneModel.tracker.curState != PlayStateTracker.State.WAIT_BACKUP_INFO &&
            playSceneModel.tracker.curState != PlayStateTracker.State.WAIT_INIT_HAND_CARD &&
            playSceneModel.tracker.curState != PlayStateTracker.State.DOING_INIT_HAND_CARD) {
            initReDrawHandCardAreaView.GetComponent<InitReDrawHandCardAreaView>().Close();
        }
    }

    // 监听playSceneModel的状态，状态变化时更新UI
    private IEnumerator ListenModel()
    {
        while (playSceneModel.tracker.curState == PlayStateTracker.State.WAIT_SELF_ACTION ||
            playSceneModel.tracker.curState == PlayStateTracker.State.WAIT_ENEMY_ACTION) {
            if (playSceneModel.hasEnemyUpdate) {
                playSceneModel.hasEnemyUpdate = false;
                UpdateUI();
            }
            if (playSceneModel.tracker.curState == PlayStateTracker.State.WAIT_SELF_ACTION &&
                KTime.CurrentMill() - playSceneModel.tracker.stateChangeTs > PlayStateTracker.TURN_TIME) {
                KLog.I(TAG, "ListenModel: self action timeout");
                // 超时未操作
                // 若还未出牌，则pass
                // 若还在技能流程中，则中止流程并流转状态，但不pass
                // 时序操作，尽量跟随unity每帧的更新逻辑走，因此不放在model里。对应的，不考虑ai超时未操作的情况
                if (playSceneModel.tracker.actionState == PlayStateTracker.ActionState.None) {
                    playSceneModel.Pass();
                } else if (playSceneModel.tracker.actionState == PlayStateTracker.ActionState.MEDICING) {
                    HandleMessage(PlaySceneMsg.HideDiscardArea);
                } else if (playSceneModel.tracker.actionState == PlayStateTracker.ActionState.ATTACKING) {
                    playSceneModel.InterruptAction();
                }
                UpdateUI();
            }
            yield return null;
        }
    }
}
