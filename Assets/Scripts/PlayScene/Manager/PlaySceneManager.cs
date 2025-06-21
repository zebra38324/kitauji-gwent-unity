using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

// PlayScene统一管理器
public class PlaySceneManager : MonoBehaviour
{
    private static string TAG = "PlaySceneManager";

    public static PlaySceneManager Instance;

    public delegate void UpdateModelDelegate(WholeAreaModel wholeAreaModel);
    public UpdateModelDelegate UpdateModelBoradcast;

    private GameObject discardArea;

    private GameObject selfPlayArea;
    private GameObject enemyPlayArea;
    private GameObject cardInfoArea;
    private GameObject initReDrawHandCardAreaView;
    private GameObject actionTextAreaView;
    private GameObject actionToastAreaView;
    private GameObject gameFinishAreaView;
    private GameObject weatherCardAreaView;
    private GameObject toastView; // 普通toast
    private GameObject selfPlayStatView;
    private GameObject enemyPlayStatView;

    private GameObject cardPrefab;

    private PlaySceneModel playSceneModel;

    private PlaySceneAI playSceneAI;
    private PlayScenePVP playScenePVP;

    private bool isPVP = false;
    private bool isAbort = false;
    private bool isModelCoroutineFinish = false;
    private bool hasClickExitPlaySceneButton = false;

    async void Awake()
    {
        KLog.I(TAG, "will init PlaySceneManager");
        Instance = this;
        await Init();
        KLog.I(TAG, "finish init PlaySceneManager");
    }

    public void Reset()
    {
        isPVP = false;
        isAbort = false;
        isModelCoroutineFinish = false;
        hasClickExitPlaySceneButton = false;
        discardArea = null;
        selfPlayArea = null;
        enemyPlayArea = null;
        cardInfoArea = null;
        initReDrawHandCardAreaView = null;
        actionTextAreaView = null;
        actionToastAreaView = null;
        gameFinishAreaView = null;
        weatherCardAreaView = null;
        toastView = null;
        cardPrefab = null;
        playSceneModel.Release();
        playSceneModel = null;
        if (playSceneAI != null) {
            playSceneAI.Release();
            playSceneAI = null;
        }
        if (playScenePVP != null) {
            playScenePVP.Release();
            playScenePVP = null;
        }
        CardViewCollection.Instance.Clear();
    }

    public async void ExitPlaySceneButton(bool normalFinish)
    {
        // 两个退出按钮都走这里逻辑
        if (hasClickExitPlaySceneButton) {
            return;
        }
        KLog.I(TAG, $"Click ExitPlaySceneButton, normalFinish = {normalFinish}");
        hasClickExitPlaySceneButton = true;
        isAbort = true;
        if (isPVP) {
            // 发送退出消息
            playScenePVP.SendStopMsg();
        }
        while (!isModelCoroutineFinish) {
            await UniTask.Delay(1);
        }
        Reset();
        var gameConfig = GameConfig.Instance;
        gameConfig.normalFinish = normalFinish;
        SceneManager.LoadScene(gameConfig.fromScene);
    }

    // 每场比赛初始化
    public async UniTask Init()
    {
        var gameConfig = GameConfig.Instance;
        playSceneModel = new PlaySceneModel(gameConfig.isHost,
            gameConfig.selfName,
            gameConfig.enemyName,
            gameConfig.selfGroup,
            UpdateModel);
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
            cardPrefab = KResources.Load<GameObject>("Prefabs/HalfCard");
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
        if (toastView == null) {
            toastView = GameObject.Find("Canvas/Background/ToastView");
        }
        if (selfPlayStatView == null) {
            selfPlayStatView = GameObject.Find("Canvas/Background/SelfPlayStatView");
        }
        if (enemyPlayStatView == null) {
            enemyPlayStatView = GameObject.Find("Canvas/Background/EnemyPlayStatView");
        }

        isPVP = gameConfig.isPVP;
        if (isPVP) {
            int sessionId = gameConfig.pvpSessionId;
            playScenePVP = new PlayScenePVP(playSceneModel.battleModel, sessionId);
            playScenePVP.Start();
        } else {
            // 配置AI模块，注意信息要和host反过来
            playSceneAI = new PlaySceneAI(!gameConfig.isHost,
                gameConfig.selfName,
                gameConfig.enemyName,
                gameConfig.pveAIType);
            playSceneAI.playSceneModel.battleModel.SendToEnemyFunc += playSceneModel.battleModel.AddEnemyActionMsg;
            playSceneModel.battleModel.SendToEnemyFunc += playSceneAI.playSceneModel.battleModel.AddEnemyActionMsg;
            playSceneAI.Start();
        }

        InitViewModel();

        // playSceneModel初始化
        while (KConfig.Instance.GetDeckInfoIdList(KConfig.Instance.deckCardGroup) == null) {
            await UniTask.Delay(1);
        }
        List<int> selfInfoIdList = KConfig.Instance.GetDeckInfoIdList(KConfig.Instance.deckCardGroup);
        playSceneModel.SetBackupCardInfoIdList(selfInfoIdList);
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
                bool isSelf = (bool)list[0];
                bool isMedic = (bool)list[1];
                discardArea.GetComponent<DiscardAreaView>().ShowArea(isSelf, isMedic);
                break;
            }
            case SceneMsg.HideDiscardArea: {
                if (playSceneModel.wholeAreaModel.gameState.actionState == GameState.ActionState.MEDICING && playSceneModel.wholeAreaModel.gameState.curState == GameState.State.WAIT_SELF_ACTION) {
                    // medic时，不复活直接关闭需要通知转状态
                    playSceneModel.InterruptAction();
                }
                discardArea.GetComponent<DiscardAreaView>().HideArea();
                break;
            }
            case SceneMsg.ChooseCard: {
                CardModel cardModel = (CardModel)list[0];
                if (playSceneModel.EnableChooseCard() && cardModel.cardSelectType != CardSelectType.None && cardModel.cardLocation != CardLocation.EnemyLeaderCardArea) {
                    // self turn时才允许选择
                    playSceneModel.ChooseCard(cardModel);
                    // 复活技能流程
                    if (playSceneModel.wholeAreaModel.gameState.actionState == GameState.ActionState.MEDICING) {
                        HandleMessage(SceneMsg.ShowDiscardArea, true, true);
                    } else if (discardArea.GetComponent<DiscardAreaView>().isShowing) {
                        HandleMessage(SceneMsg.HideDiscardArea);
                    }
                    // 选择指导老师的行
                    if (playSceneModel.wholeAreaModel.gameState.curState == GameState.State.WAIT_SELF_ACTION &&
                        playSceneModel.wholeAreaModel.gameState.actionState == GameState.ActionState.HORN_UTILING) {
                        HandleMessage(SceneMsg.ShowHornAreaViewButton);
                    }
                }
                break;
            }
            case SceneMsg.ClickPass: {
                if (playSceneModel.EnableChooseCard()) {
                    // self turn时才允许选择
                    playSceneModel.Pass();
                }
                break;
            }
            case SceneMsg.ReDrawInitHandCard: {
                playSceneModel.ReDrawInitHandCard();
                break;
            }
            case SceneMsg.ClickHornAreaViewButton: {
                playSceneModel.ChooseHornUtilArea((CardBadgeType)list[0]);
                HandleMessage(SceneMsg.HideHornAreaViewButton);
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
            case SceneMsg.PVPEnemyExit: {
                // 对方退出，弹toast提示。并暂停各种倒计时
                actionTextAreaView.GetComponent<ActionTextAreaView>().AddText(string.Format("<color=red>{0}</color> 退出房间\n", playSceneModel.wholeAreaModel.playTracker.enemyPlayerInfo.name));
                isAbort = true;
                selfPlayStatView.GetComponent<PlayStatAreaView>().isAbort = true;
                enemyPlayStatView.GetComponent<PlayStatAreaView>().isAbort = true;
                toastView.GetComponent<ToastView>().ShowToast("对方已退出房间，请退出");
                break;
            }
        }
    }

    private IEnumerator StartGame()
    {
        while (playSceneModel.wholeAreaModel.gameState.curState != GameState.State.WAIT_INIT_HAND_CARD) {
            if (isAbort) {
                KLog.I(TAG, "StartGame: isAbort at WAIT_BACKUP_INFO, break loop");
                isModelCoroutineFinish = true;
                yield break;
            }
            yield return null;
        }
        initReDrawHandCardAreaView.GetComponent<InitReDrawHandCardAreaView>().StartUI();
        playSceneModel.DrawInitHandCard();
        while (playSceneModel.wholeAreaModel.gameState.curState != GameState.State.WAIT_SELF_ACTION &&
               playSceneModel.wholeAreaModel.gameState.curState != GameState.State.WAIT_ENEMY_ACTION) {
            if (isAbort) {
                KLog.I(TAG, "StartGame: isAbort at WAIT_INIT_HAND_CARD, break loop");
                isModelCoroutineFinish = true;
                yield break;
            }
            if (!initReDrawHandCardAreaView.GetComponent<InitReDrawHandCardAreaView>().isSelfConfirmed && 
                KTime.CurrentMill() - initReDrawHandCardAreaView.GetComponent<InitReDrawHandCardAreaView>().startTs > InitReDrawHandCardAreaView.MAX_TIME) {
                // 超时未操作，结束重抽手牌
                KLog.I(TAG, "initReDrawHandCard: self action timeout");
                initReDrawHandCardAreaView.GetComponent<InitReDrawHandCardAreaView>().ConfirmButton();
            }
            yield return null;
        }
        StartCoroutine(ModelCoroutine());
    }

    private void InitViewModel()
    {
        UpdateModel(null);
    }

    private void UpdateModel(List<ActionEvent> actionEventList)
    {
        WholeAreaModel model = playSceneModel.wholeAreaModel;
        if (UpdateModelBoradcast != null) {
            UpdateModelBoradcast(model); // 要先更新card的状态
        }
        selfPlayArea.GetComponent<SinglePlayerAreaView>().UpdateModel(model.selfSinglePlayerAreaModel);
        enemyPlayArea.GetComponent<SinglePlayerAreaView>().UpdateModel(model.enemySinglePlayerAreaModel);
        selfPlayStatView.GetComponent<PlayStatAreaView>().UpdateModel(model, true);
        enemyPlayStatView.GetComponent<PlayStatAreaView>().UpdateModel(model, false);
        initReDrawHandCardAreaView.GetComponent<InitReDrawHandCardAreaView>().UpdateModel(model.selfSinglePlayerAreaModel.handCardAreaModel.initHandCardListModel);
        weatherCardAreaView.GetComponent<WeatherCardAreaView>().UpdateModel(model.weatherAreaModel);
        discardArea.GetComponent<DiscardAreaView>().UpdateModel(model.selfSinglePlayerAreaModel.discardAreaModel, model.enemySinglePlayerAreaModel.discardAreaModel);
        gameFinishAreaView.GetComponent<GameFinishAreaView>().tracker = playSceneModel.wholeAreaModel.playTracker;
        if (model.gameState.curState != GameState.State.WAIT_BACKUP_INFO &&
            model.gameState.curState != GameState.State.WAIT_INIT_HAND_CARD) {
            initReDrawHandCardAreaView.GetComponent<InitReDrawHandCardAreaView>().Close();
        }
        if (actionEventList == null) {
            return;
        }
        foreach (ActionEvent actionEvent in actionEventList) {
            switch (actionEvent.type) {
                case ActionEvent.Type.ActionText: {
                    actionTextAreaView.GetComponent<ActionTextAreaView>().AddText((string)actionEvent.args[0]);
                    break;
                }
                case ActionEvent.Type.Toast: {
                    actionToastAreaView.GetComponent<ActionToastAreaView>().showText = (string)actionEvent.args[0];
                    break;
                }
                case ActionEvent.Type.Sfx: {
                    AudioManager.Instance.PlaySFX((AudioManager.SFXType)actionEvent.args[0]);
                    break;
                }
            }
        }
    }

    // 监听playSceneModel的状态，状态变化时更新UI
    private IEnumerator ModelCoroutine()
    {
        while (playSceneModel.wholeAreaModel.gameState.curState == GameState.State.WAIT_SELF_ACTION ||
            playSceneModel.wholeAreaModel.gameState.curState == GameState.State.WAIT_ENEMY_ACTION ||
            playSceneModel.wholeAreaModel.gameState.curState == GameState.State.SET_FINFISH) {
            if (isAbort) {
                isModelCoroutineFinish = true;
                KLog.I(TAG, "ModelCoroutine: isAbort, break loop");
                yield break;
            }
            if (playSceneModel.wholeAreaModel.gameState.curState == GameState.State.WAIT_SELF_ACTION &&
                KTime.CurrentMill() - playSceneModel.wholeAreaModel.gameState.stateChangeTs > GameState.TURN_TIME) {
                KLog.I(TAG, "ListenModel: self action timeout");
                // 超时未操作
                // 若还未出牌，则pass
                // 若还在技能流程中，则中止流程并流转状态，但不pass
                // 时序操作，尽量跟随unity每帧的更新逻辑走，因此不放在model里。对应的，不考虑ai超时未操作的情况
                if (playSceneModel.wholeAreaModel.gameState.actionState == GameState.ActionState.None) {
                    playSceneModel.Pass();
                } else if (playSceneModel.wholeAreaModel.gameState.actionState == GameState.ActionState.MEDICING) {
                    HandleMessage(SceneMsg.HideDiscardArea);
                } else if (playSceneModel.wholeAreaModel.gameState.actionState == GameState.ActionState.HORN_UTILING) {
                    HandleMessage(SceneMsg.HideHornAreaViewButton);
                    playSceneModel.InterruptAction();
                } else if (playSceneModel.wholeAreaModel.gameState.actionState == GameState.ActionState.ATTACKING ||
                    playSceneModel.wholeAreaModel.gameState.actionState == GameState.ActionState.DECOYING ||
                    playSceneModel.wholeAreaModel.gameState.actionState == GameState.ActionState.MONAKAING) {
                    playSceneModel.InterruptAction();
                }
            }
            yield return null;
        }
        // 此时state理论上必为stop
        if (playSceneModel.wholeAreaModel.gameState.curState != GameState.State.STOP) {
            KLog.E(TAG, "ModelCoroutine: break loop, state invalid = " + playSceneModel.wholeAreaModel.gameState.curState);
        }
        // 展示结算页面
        KLog.I(TAG, "ModelCoroutine: game finish");
        gameFinishAreaView.SetActive(true);
        isModelCoroutineFinish = true;
    }
}
