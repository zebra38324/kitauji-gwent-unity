using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// PlayScene统一管理器
public class PlaySceneManager : MonoBehaviour
{
    private static string TAG = "PlaySceneManager";

    public static PlaySceneManager Instance;

    private GameObject discardArea;

    private GameObject selfPlayArea;
    private GameObject enemyPlayArea;
    private GameObject cardInfoArea;
    private GameObject initReDrawHandCardAreaView;
    private GameObject actionTextAreaView;
    private GameObject actionToastAreaView;
    private GameObject gameFinishAreaView;
    private GameObject weatherCardAreaView;

    private GameObject cardPrefab;

    private PlaySceneModel playSceneModel;

    private PlaySceneAI playSceneAI;

    async void Awake()
    {
        KLog.I(TAG, "will init PlaySceneManager");
        Instance = this;
        await Init();
        KLog.I(TAG, "finish init PlaySceneManager");
    }

    public void Reset()
    {
        discardArea = null;
        selfPlayArea = null;
        enemyPlayArea = null;
        cardInfoArea = null;
        initReDrawHandCardAreaView = null;
        actionTextAreaView = null;
        actionToastAreaView = null;
        gameFinishAreaView = null;
        weatherCardAreaView = null;
        cardPrefab = null;
        playSceneModel.Release();
        playSceneModel = null;
        playSceneAI.Release();
        playSceneAI = null;
        CardViewCollection.Instance.Clear();
    }

    // 每场比赛初始化
    public async UniTask Init()
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
        if (actionTextAreaView == null) {
            actionTextAreaView = GameObject.Find("Canvas/Background/ActionTextScrollView");
        }
        if (actionToastAreaView == null) {
            actionToastAreaView = GameObject.Find("Canvas/Background/ActionToastArea");
        }
        if (gameFinishAreaView  == null) {
            gameFinishAreaView = GameObject.Find("Canvas/Background/GameFinishArea");
        }
        if (weatherCardAreaView == null) {
            weatherCardAreaView = GameObject.Find("Canvas/Background/WeatherCardArea");
        }


        // 配置AI模块
        playSceneAI = new PlaySceneAI();
        playSceneAI.playSceneModel.battleModel.SendToEnemyFunc += playSceneModel.battleModel.AddEnemyActionMsg;
        playSceneModel.battleModel.SendToEnemyFunc += playSceneAI.playSceneModel.battleModel.AddEnemyActionMsg;
        playSceneAI.Start();

        InitViewModel();

        // playSceneModel初始化
        while (KConfig.Instance.deckInfoIdList == null) {
            await UniTask.Delay(1);
        }
        List<int> selfInfoIdList = KConfig.Instance.deckInfoIdList;
        playSceneModel.SetBackupCardInfoIdList(selfInfoIdList);
        initReDrawHandCardAreaView.GetComponent<InitReDrawHandCardAreaView>().StartUI();
        StartCoroutine(StartGame());
    }

    public void HandleMessage(SceneMsg msg, params object[] list)
    {
        switch (msg)
        {
            case SceneMsg.ShowCardInfo: {
                CardInfo info = (CardInfo)list[0];
                cardInfoArea.GetComponent<CardInfoAreaView>().ShowInfo(info);
                break;
            }
            case SceneMsg.HideCardInfo: {
                cardInfoArea.GetComponent<CardInfoAreaView>().HideInfo();
                break;
            }
            case SceneMsg.ShowDiscardArea: {
                DiscardAreaModel discardAreaModel = (DiscardAreaModel)list[0];
                bool isMedic = (bool)list[1];
                discardArea.GetComponent<DiscardAreaView>().ShowArea(discardAreaModel, isMedic);
                break;
            }
            case SceneMsg.HideDiscardArea: {
                if (playSceneModel.tracker.actionState == PlayStateTracker.ActionState.MEDICING && playSceneModel.tracker.curState == PlayStateTracker.State.WAIT_SELF_ACTION) {
                    // medic时，不复活直接关闭需要通知转状态
                    playSceneModel.InterruptAction();
                }
                discardArea.GetComponent<DiscardAreaView>().HideArea();
                break;
            }
            case SceneMsg.ChooseCard: {
                CardModel cardModel = (CardModel)list[0];
                if (playSceneModel.EnableChooseCard(true) && cardModel.selectType != CardSelectType.None) {
                    // self turn时才允许选择
                    playSceneModel.ChooseCard(cardModel);
                    // 复活技能流程
                    if (playSceneModel.tracker.actionState == PlayStateTracker.ActionState.MEDICING) {
                        HandleMessage(SceneMsg.ShowDiscardArea, playSceneModel.selfSinglePlayerAreaModel.discardAreaModel, true);
                    } else if (discardArea.GetComponent<DiscardAreaView>().model != null) {
                        HandleMessage(SceneMsg.HideDiscardArea);
                    }
                    // 选择指导老师的行
                    if (playSceneModel.tracker.curState == PlayStateTracker.State.WAIT_SELF_ACTION &&
                        playSceneModel.tracker.actionState == PlayStateTracker.ActionState.HORN_UTILING) {
                        HandleMessage(SceneMsg.ShowHornAreaViewButton);
                    }
                    UpdateUI();
                }
                break;
            }
            case SceneMsg.ClickPass: {
                if (playSceneModel.IsTurn(true)) {
                    // self turn时才允许选择
                    playSceneModel.Pass();
                    UpdateUI();
                }
                break;
            }
            case SceneMsg.ReDrawInitHandCard: {
                playSceneModel.ReDrawInitHandCard();
                break;
            }
            case SceneMsg.ClickHornAreaViewButton: {
                BattleRowAreaModel battleRowAreaModel = (BattleRowAreaModel)list[0];
                playSceneModel.ChooseHornUtilArea(battleRowAreaModel);
                HandleMessage(SceneMsg.HideHornAreaViewButton);
                UpdateUI();
                break;
            }
            case SceneMsg.ShowHornAreaViewButton: {
                // 显示按钮
                selfPlayArea.GetComponent<SinglePlayerAreaView>().woodRow.GetComponent<BattleRowAreaView>().ShowHornAreaViewButton();
                selfPlayArea.GetComponent<SinglePlayerAreaView>().brassRow.GetComponent<BattleRowAreaView>().ShowHornAreaViewButton();
                selfPlayArea.GetComponent<SinglePlayerAreaView>().percussionRow.GetComponent<BattleRowAreaView>().ShowHornAreaViewButton();
                break;
            }
            case SceneMsg.HideHornAreaViewButton: {
                // 隐藏按钮
                selfPlayArea.GetComponent<SinglePlayerAreaView>().woodRow.GetComponent<BattleRowAreaView>().HideHornAreaViewButton();
                selfPlayArea.GetComponent<SinglePlayerAreaView>().brassRow.GetComponent<BattleRowAreaView>().HideHornAreaViewButton();
                selfPlayArea.GetComponent<SinglePlayerAreaView>().percussionRow.GetComponent<BattleRowAreaView>().HideHornAreaViewButton();
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
        actionTextAreaView.GetComponent<ActionTextAreaView>().actionTextModel = playSceneModel.actionTextModel;
        actionToastAreaView.GetComponent<ActionToastAreaView>().actionTextModel = playSceneModel.actionTextModel;
        gameFinishAreaView.GetComponent<GameFinishAreaView>().tracker = playSceneModel.tracker;
        weatherCardAreaView.GetComponent<WeatherCardAreaView>().weatherCardAreaModel = playSceneModel.weatherCardAreaModel;
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
        actionTextAreaView.GetComponent<ActionTextAreaView>().UpdateUI();
        weatherCardAreaView.GetComponent<WeatherCardAreaView>().UpdateUI();
    }

    // 监听playSceneModel的状态，状态变化时更新UI
    private IEnumerator ListenModel()
    {
        while (playSceneModel.tracker.curState == PlayStateTracker.State.WAIT_SELF_ACTION ||
            playSceneModel.tracker.curState == PlayStateTracker.State.WAIT_ENEMY_ACTION ||
            playSceneModel.tracker.curState == PlayStateTracker.State.SET_FINFISH) {
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
                    HandleMessage(SceneMsg.HideDiscardArea);
                } else if (playSceneModel.tracker.actionState == PlayStateTracker.ActionState.HORN_UTILING) {
                    HandleMessage(SceneMsg.HideHornAreaViewButton);
                    playSceneModel.InterruptAction();
                }  else if (playSceneModel.tracker.actionState == PlayStateTracker.ActionState.ATTACKING ||
                    playSceneModel.tracker.actionState == PlayStateTracker.ActionState.DECOYING) {
                    playSceneModel.InterruptAction();
                }
                UpdateUI();
            }
            yield return null;
        }
        // 此时state理论上必为stop
        if (playSceneModel.tracker.curState != PlayStateTracker.State.STOP) {
            KLog.E(TAG, "ListenModel: break loop, state invalid = " + playSceneModel.tracker.curState);
        }
        // 展示结算页面
        KLog.I(TAG, "ListenModel: game finish");
        UpdateUI();
        gameFinishAreaView.SetActive(true);
    }
}
