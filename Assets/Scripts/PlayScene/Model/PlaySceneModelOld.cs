using System;
using System.Collections;
using System.Collections.Generic;
/**
 * 游戏对局场景的逻辑管理，单例
 * 流程：
 * 1. SetBackupCardInfoIdList，设置本方备选卡牌，生成CardModelOld并发送信息至BattleModel
 * 2. EnemyMsgCallback，等待收到对方备选卡牌信息，并生成对应CardModelOld
 * 3. DrawInitHandCard，抽取本方手牌，等待用户选择重抽的牌
 * 4. ReDrawInitHandCard，确定初始手牌，并发送信息至BattleModel
 * 5. EnemyMsgCallback，收到对方手牌信息
 * 6. StartGame，开始整场比赛（不需要外部调用，内部自动流转）
 * 7. ChooseCard，双方出牌
 * 8. Pass，跳过本局出牌。双方都pass后，结束本局。满足结束条件时，结束整场游戏
 */
public class PlaySceneModelOld
{
    private string TAG = "PlaySceneModelOld";

    public PlaySceneModelOld(bool isHost_ = true,
        string selfName = "",
        string enemyName = "",
        CardGroup selfGroup = CardGroup.KumikoFirstYear)
    {
        isHost = isHost_;
        TAG += isHost ? "-Host" : "-Player";
        selfSinglePlayerAreaModel = new SinglePlayerAreaModelOld(isHost);
        enemySinglePlayerAreaModel = new SinglePlayerAreaModelOld(isHost);
        battleModel = new BattleModel(isHost);
        battleModel.EnemyMsgCallback += EnemyMsgCallback;
        tracker = new PlayStateTracker(isHost, selfName, enemyName, selfGroup);
        actionTextModel = new ActionTextModelOld(selfName, enemyName);
        weatherCardAreaModel = new WeatherCardAreaModel();
    }

    public void Release()
    {
        KLog.I(TAG, "Release");
        selfSinglePlayerAreaModel = null;
        enemySinglePlayerAreaModel = null;
        battleModel.Release();
        battleModel = null;
        tracker = null;
        actionTextModel = null;
        weatherCardAreaModel = null;
    }

    public SinglePlayerAreaModelOld selfSinglePlayerAreaModel { get; private set; }

    // 待确定的初始手牌区
    public RowAreaModel selfPrepareHandCardAreaModel { get; private set; }

    public SinglePlayerAreaModelOld enemySinglePlayerAreaModel { get; private set; }

    public BattleModel battleModel { get; private set; }

    // 对方操作是否发生了更新，用于指示是否需要更新ui
    public bool hasEnemyUpdate = false;

    private CardModelOld attackCard = null; // ActionState.ATTACKING时，记录发动攻击的牌

    private CardModelOld decoyCard = null; // ActionState.DECOYING时，记录大号君的牌

    private CardModelOld hornUtilCard = null; // ActionState.HORN_UTILING时，记录指导老师的牌

    public PlayStateTracker tracker { get; private set; }

    public ActionTextModelOld actionTextModel { get; private set; }

    public WeatherCardAreaModel weatherCardAreaModel { get; private set; }

    // 需要音效播放时，回调
    public delegate void SfxCallbackDelegate(AudioManager.SFXType type);
    public SfxCallbackDelegate SfxCallback;

    private bool isHost;

    private bool hasSelfReDrawInitHandCard = false; // TODO: 这个设计很不好

    public void EnemyMsgCallback(BattleModel.ActionType actionType, params object[] list)
    {
        switch (actionType) {
            case BattleModel.ActionType.Init: {
                CardGroup cardGroup = (CardGroup)list[0];
                List<int> infoIdList = (List<int>)list[1];
                List<int> idList = (List<int>)list[2];
                tracker.enemyGroup = cardGroup;
                enemySinglePlayerAreaModel.SetBackupCardInfoIdList(infoIdList, idList);
                lock (this) {
                    if (selfSinglePlayerAreaModel.backupCardList.Count > 0) {
                        tracker.TransState(PlayStateTracker.State.WAIT_INIT_HAND_CARD); // 多线程问题加锁
                    }
                }
                break;
            }
            case BattleModel.ActionType.DrawHandCard: {
                List<int> idList = (List<int>)list[0];
                enemySinglePlayerAreaModel.DrawHandCards(idList);
                // 对面间谍抽手牌也可能走到这里，下面的状态流转要添加判断
                if (tracker.curState == PlayStateTracker.State.DOING_INIT_HAND_CARD || tracker.curState == PlayStateTracker.State.WAIT_INIT_HAND_CARD) {
                    actionTextModel.FinishInitHandCard(false);
                    lock (this) { // 多线程问题加锁
                        if (selfSinglePlayerAreaModel.handRowAreaModel.cardList.Count > 0) {
                            WillWaitStartGame();
                        }
                    }
                }
                break;
            }
            case BattleModel.ActionType.StartGame: {
                bool hostFirst = (bool)list[0];
                tracker.StartGamePlayer(hostFirst);
                break;
            }
            case BattleModel.ActionType.ChooseCard: {
                List<int> idList = (List<int>)list[0];
                int id = idList[0];
                CardModelOld card = selfSinglePlayerAreaModel.FindCard(id);
                if (card == null) {
                    card = enemySinglePlayerAreaModel.FindCard(id);
                    if (card == null) {
                        KLog.E(TAG, "EnemyMsgCallback: ChooseCard: invalid id: " + id);
                        return;
                    }
                }
                ChooseCard(card, false);
                break;
            }
            case BattleModel.ActionType.Pass: {
                Pass(false);
                break;
            }
            case BattleModel.ActionType.InterruptAction: {
                InterruptAction(false);
                break;
            }
            case BattleModel.ActionType.ChooseHornUtilArea: {
                BattleModel.HornUtilAreaType hornUtilAreaType = (BattleModel.HornUtilAreaType)list[0];
                BattleRowAreaModelOld battleRowAreaModel;
                if (hornUtilAreaType == BattleModel.HornUtilAreaType.Wood) {
                    battleRowAreaModel = enemySinglePlayerAreaModel.woodRowAreaModel;
                } else if (hornUtilAreaType == BattleModel.HornUtilAreaType.Brass) {
                    battleRowAreaModel = enemySinglePlayerAreaModel.brassRowAreaModel;
                } else if (hornUtilAreaType == BattleModel.HornUtilAreaType.Percussion) {
                    battleRowAreaModel = enemySinglePlayerAreaModel.percussionRowAreaModel;
                } else {
                    KLog.E(TAG, "EnemyMsgCallback: ChooseHornUtilArea: invalid hornUtilAreaType: " + hornUtilAreaType);
                    break;
                }
                ChooseHornUtilArea(battleRowAreaModel, false);
                break;
            }
        }
        hasEnemyUpdate = true;
    }

    // 设置本方备选卡牌
    public void SetBackupCardInfoIdList(List<int> selfInfoIdList)
    {
        if (tracker.curState != PlayStateTracker.State.WAIT_BACKUP_INFO) {
            KLog.E(TAG, "SetBackupCardInfoIdList: state invalid: " + tracker.curState);
            return;
        }
        selfSinglePlayerAreaModel.SetBackupCardInfoIdList(selfInfoIdList);
        // 发送self备选卡牌信息
        Action SendSelfBackupCardInfo = () => {
            List<int> infoIdList = new List<int>();
            List<int> idList = new List<int>();
            foreach (CardModelOld card in selfSinglePlayerAreaModel.backupCardList) {
                infoIdList.Add(card.cardInfo.infoId);
                idList.Add(card.cardInfo.id);
            }
            if (selfSinglePlayerAreaModel.leaderCardAreaModel.cardList.Count > 0) {
                infoIdList.Add(selfSinglePlayerAreaModel.leaderCardAreaModel.cardList[0].cardInfo.infoId);
                idList.Add(selfSinglePlayerAreaModel.leaderCardAreaModel.cardList[0].cardInfo.id);
            }
            battleModel.AddSelfActionMsg(BattleModel.ActionType.Init, tracker.selfGroup, infoIdList, idList);
            return;
        };
        SendSelfBackupCardInfo();
        lock (this) {
            if (enemySinglePlayerAreaModel.backupCardList.Count > 0) {
                tracker.TransState(PlayStateTracker.State.WAIT_INIT_HAND_CARD); // 多线程问题加锁
            }
        }
    }

    public void DrawInitHandCard()
    {
        if (tracker.curState != PlayStateTracker.State.WAIT_INIT_HAND_CARD) {
            KLog.E(TAG, "DrawInitHandCard: state invalid: " + tracker.curState);
            return;
        }
        // self抽取十张初始手牌
        selfSinglePlayerAreaModel.DrawInitHandCard();
        // 等待玩家确定需要重新抽取的手牌
        tracker.TransState(PlayStateTracker.State.DOING_INIT_HAND_CARD);
    }

    public void ReDrawInitHandCard()
    {
        if (tracker.curState != PlayStateTracker.State.DOING_INIT_HAND_CARD) {
            KLog.E(TAG, "DrawInitHandCard: state invalid: " + tracker.curState);
            return;
        }
        hasSelfReDrawInitHandCard = true;
        selfSinglePlayerAreaModel.ReDrawInitHandCard();
        // 发送self初始手牌信息
        Action SendSelfInitHandCardInfo = () => {
            List<int> idList = new List<int>();
            foreach (CardModelOld card in selfSinglePlayerAreaModel.handRowAreaModel.cardList) {
                idList.Add(card.cardInfo.id);
            }
            battleModel.AddSelfActionMsg(BattleModel.ActionType.DrawHandCard, idList);
            return;
        };
        SendSelfInitHandCardInfo();
        actionTextModel.FinishInitHandCard(true);
        lock (this) { // 多线程问题加锁
            if (enemySinglePlayerAreaModel.handRowAreaModel.cardList.Count > 0) {
                WillWaitStartGame();
            }
        }
    }

    // 跳过本局出牌
    public void Pass(bool isSelf = true)
    {
        tracker.Pass(isSelf);
        if (isSelf) {
            // 发送本方pass动作
            battleModel.AddSelfActionMsg(BattleModel.ActionType.Pass);
        }
        actionTextModel.Pass(isSelf);
        if (tracker.curState == PlayStateTracker.State.SET_FINFISH) {
            SetFinish();
        }
    }

    // 是否是本方/对方出牌回合
    public bool IsTurn(bool isSelf)
    {
        if (isSelf) {
            return tracker.curState == PlayStateTracker.State.WAIT_SELF_ACTION;
        } else {
            return tracker.curState == PlayStateTracker.State.WAIT_ENEMY_ACTION;
        }
    }

    // 是否允许选择卡牌
    public bool EnableChooseCard(bool isSelf = true)
    {
        if (isSelf) {
            return tracker.curState == PlayStateTracker.State.WAIT_SELF_ACTION ||
                (tracker.curState == PlayStateTracker.State.DOING_INIT_HAND_CARD && !hasSelfReDrawInitHandCard);
        } else {
            return tracker.curState == PlayStateTracker.State.WAIT_ENEMY_ACTION;
        }
    }

    // 选择卡牌，用户ui操作的处理接口
    // 双方都会调用这个接口
    public void ChooseCard(CardModelOld card, bool isSelf = true)
    {
        if (!EnableChooseCard(isSelf)) {
            KLog.E(TAG, "ChooseCard: state invalid: " + tracker.curState + ", isSelf = " + isSelf);
        }
        // 从选择卡牌方的视角来看
        SinglePlayerAreaModelOld selfArea = isSelf ? selfSinglePlayerAreaModel : enemySinglePlayerAreaModel;
        SinglePlayerAreaModelOld enemyArea = isSelf ? enemySinglePlayerAreaModel : selfSinglePlayerAreaModel;
        CardSelectType originSelectType = card.selectType;
        KLog.I(TAG, "ChooseCard: " + card.cardInfo.chineseName + ", isSelf = " + isSelf);
        switch (card.selectType) {
            case CardSelectType.None: {
                KLog.I(TAG, "ChooseCard: selectType = None, can not choose");
                return;
            }
            case CardSelectType.ReDrawHandCard: {
                // 记录要选择的重抽手牌
                selfSinglePlayerAreaModel.initHandRowAreaModel.SelectCard(card);
                break;
            }
            case CardSelectType.PlayCard: {
                if (tracker.actionState == PlayStateTracker.ActionState.ATTACKING) {
                    KLog.I(TAG, "ChooseCard: actionState = ATTACKING, can not play card");
                    return;
                }
                if (tracker.actionState == PlayStateTracker.ActionState.MEDICING) {
                    if (card.cardLocation != CardLocation.DiscardArea) {
                        // 此时不许打出非弃牌区的牌
                        KLog.I(TAG, "ChooseCard: actionState = MEDICING, card location invalid = " + card.cardLocation);
                        return;
                    }
                    FinishMedic(selfArea);
                }
                tracker.TransActionState(PlayStateTracker.ActionState.None); // medic技能可能递归打牌，先置空再进行后续
                PlayCard(card, selfArea, enemyArea);
                break;
            }
            case CardSelectType.WithstandAttack: {
                ApplyBeAttacked(card, enemyArea);
                FinishAttack(enemyArea);
                tracker.TransActionState(PlayStateTracker.ActionState.None); // 攻击技能，先完成再置空
                break;
            }
            case CardSelectType.DecoyWithdraw: {
                ApplyBeWithdraw(card, selfArea);
                FinishDecoy(selfArea);
                tracker.TransActionState(PlayStateTracker.ActionState.None);
                break;
            }
            case CardSelectType.Monaka: {
                ApplyBeMonaka(card);
                FinishMonaka(selfArea);
                tracker.TransActionState(PlayStateTracker.ActionState.None);
                break;
            }
        }
        if (isSelf && tracker.curState == PlayStateTracker.State.WAIT_SELF_ACTION) {
            // 发送本方选择牌动作
            battleModel.AddSelfActionMsg(BattleModel.ActionType.ChooseCard, card.cardInfo.id);
        }
        // 流转双方出牌回合
        if (tracker.actionState == PlayStateTracker.ActionState.None) {
            if (isSelf && tracker.curState == PlayStateTracker.State.WAIT_SELF_ACTION) {
                tracker.TransState(PlayStateTracker.State.WAIT_ENEMY_ACTION);
            } else if (tracker.curState == PlayStateTracker.State.WAIT_ENEMY_ACTION) {
                tracker.TransState(PlayStateTracker.State.WAIT_SELF_ACTION);
            }
        }
        actionTextModel.ChooseCard(isSelf, card, originSelectType);
    }

    // actionState不为None时，一些特殊情况，中止技能流程并流转curState
    public void InterruptAction(bool isSelf = true)
    {
        if (!(isSelf && tracker.curState == PlayStateTracker.State.WAIT_SELF_ACTION) &&
            !(!isSelf && tracker.curState == PlayStateTracker.State.WAIT_ENEMY_ACTION)) {
            KLog.E(TAG, "InterruptAction: state invalid: " + tracker.curState + ", isSelf = " + isSelf);
        }
        if (tracker.actionState == PlayStateTracker.ActionState.None) {
            KLog.E(TAG, "InterruptAction: actionState = None, invalid, isSelf = " + isSelf);
            return;
        }
        SinglePlayerAreaModelOld selfArea = isSelf ? selfSinglePlayerAreaModel : enemySinglePlayerAreaModel;
        SinglePlayerAreaModelOld enemyArea = isSelf ? enemySinglePlayerAreaModel : selfSinglePlayerAreaModel;
        KLog.I(TAG, "InterruptAction: actionState = " + tracker.actionState + ", isSelf = " + isSelf);
        if (tracker.actionState == PlayStateTracker.ActionState.ATTACKING) {
            FinishAttack(enemyArea);
        } else if (tracker.actionState == PlayStateTracker.ActionState.MEDICING) {
            FinishMedic(selfArea);
        } else if (tracker.actionState == PlayStateTracker.ActionState.DECOYING) {
            FinishDecoy(selfArea);
        } else if (tracker.actionState == PlayStateTracker.ActionState.HORN_UTILING) {
            FinishHornUtil(selfArea);
        } else if (tracker.actionState == PlayStateTracker.ActionState.MONAKAING) {
            FinishMonaka(selfArea);
        }
        tracker.TransActionState(PlayStateTracker.ActionState.None);
        if (isSelf) {
            battleModel.AddSelfActionMsg(BattleModel.ActionType.InterruptAction);
            tracker.TransState(PlayStateTracker.State.WAIT_ENEMY_ACTION);
        } else {
            tracker.TransState(PlayStateTracker.State.WAIT_SELF_ACTION);
        }
        actionTextModel.InterruptAction(isSelf);
    }

    public void ChooseHornUtilArea(BattleRowAreaModelOld battleRowAreaModel, bool isSelf = true)
    {
        if (tracker.actionState != PlayStateTracker.ActionState.HORN_UTILING) {
            KLog.E(TAG, "ChooseHornUtilArea: actionState invalid: " + tracker.actionState + ", isSelf = " + isSelf);
            return;
        }
        SinglePlayerAreaModelOld selfArea = isSelf ? selfSinglePlayerAreaModel : enemySinglePlayerAreaModel;
        SinglePlayerAreaModelOld enemyArea = isSelf ? enemySinglePlayerAreaModel : selfSinglePlayerAreaModel;
        BattleModel.HornUtilAreaType hornUtilAreaType = BattleModel.HornUtilAreaType.Wood;
        if (battleRowAreaModel == selfArea.woodRowAreaModel) {
            selfArea.woodRowAreaModel.AddCard(hornUtilCard);
            hornUtilAreaType = BattleModel.HornUtilAreaType.Wood;
        } else if (battleRowAreaModel == selfArea.brassRowAreaModel) {
            selfArea.brassRowAreaModel.AddCard(hornUtilCard);
            hornUtilAreaType = BattleModel.HornUtilAreaType.Brass;
        } else if (battleRowAreaModel == selfArea.percussionRowAreaModel) {
            selfArea.percussionRowAreaModel.AddCard(hornUtilCard);
            hornUtilAreaType = BattleModel.HornUtilAreaType.Percussion;
        }
        FinishHornUtil(selfArea);
        tracker.TransActionState(PlayStateTracker.ActionState.None);
        if (isSelf) {
            battleModel.AddSelfActionMsg(BattleModel.ActionType.ChooseHornUtilArea, hornUtilAreaType);
            tracker.TransState(PlayStateTracker.State.WAIT_ENEMY_ACTION);
        } else {
            tracker.TransState(PlayStateTracker.State.WAIT_SELF_ACTION);
        }
    }

    private void WillWaitStartGame()
    {
        tracker.TransState(PlayStateTracker.State.WAIT_START);
        if (isHost) {
            tracker.StartGameHost();
            battleModel.AddSelfActionMsg(BattleModel.ActionType.StartGame, tracker.curState == PlayStateTracker.State.WAIT_SELF_ACTION);
        }
        actionTextModel.SetStart(tracker.setRecordList[tracker.curSet].selfFirst);
    }

    /**
     * 打出卡牌，可能来源于
     * 1. 手牌主动打出
     * 2. 手牌被动打出
     * 3. 备选卡牌区打出
     * 4. 弃牌区复活打出
     * 5. 指挥牌（TODO）
     */
    private void PlayCard(CardModelOld card, SinglePlayerAreaModelOld selfArea, SinglePlayerAreaModelOld enemyArea)
    {
        KLog.I(TAG, "PlayCard: " + card.cardInfo.chineseName);
        // 从原区域移除
        switch (card.cardLocation) {
            case CardLocation.HandArea: {
                selfArea.handRowAreaModel.RemoveCard(card);
                break;
            }
            case CardLocation.DiscardArea: {
                selfArea.discardAreaModel.RemoveCard(card);
                break;
            }
            case CardLocation.LeaderCardArea: {
                selfArea.leaderCardAreaModel.RemoveCard(card);
                break;
            }
            default: {
                break;
            }
        }

        // 实施卡牌技能
        // 涉及双方的技能放在这一层，其他的放到下层各自实现即可
        // Medic较为特殊，放到下层的话，无法很好地确定结束技能流程的时机，因此也放在这里
        switch (card.cardInfo.ability) {
            case CardAbility.Spy: {
                enemyArea.AddBattleAreaCard(card);
                // 仅self时需要实际抽牌
                if (selfArea == selfSinglePlayerAreaModel) {
                    DrawHandCardAndSend(2);
                }
                break;
            }
            case CardAbility.Attack: {
                selfArea.AddBattleAreaCard(card);
                ApplyAttack(card, enemyArea);
                break;
            }
            case CardAbility.Tunning: {
                selfArea.AddBattleAreaCard(card);
                PlaySfx(AudioManager.SFXType.Tunning);
                break;
            }
            case CardAbility.ScorchWood: {
                selfArea.AddBattleAreaCard(card);
                ApplyScorchWood(enemyArea);
                break;
            }
            case CardAbility.Medic: {
                if (card.cardInfo.cardType == CardType.Normal ||
                    card.cardInfo.cardType == CardType.Hero) {
                    // 指挥牌可能是medic，不放入对战区
                    selfArea.AddBattleAreaCard(card);
                }
                ApplyMedic(selfArea);
                break;
            }
            case CardAbility.Decoy: {
                ApplyDecoy(card, selfArea);
                break;
            }
            case CardAbility.Scorch: {
                ApplyScorch(selfArea, enemyArea);
                if (card.cardInfo.cardType == CardType.Normal ||
                    card.cardInfo.cardType == CardType.Hero) {
                    // 角色牌带Scorch
                    selfArea.AddBattleAreaCard(card);
                }
                break;
            }
            case CardAbility.SunFes:
            case CardAbility.Daisangakushou:
            case CardAbility.Drumstick: {
                ApplyWeather(card);
                break;
            }
            case CardAbility.ClearWeather: {
                card.cardLocation = CardLocation.None; // 用完了直接丢入虚空
                RemoveWeather();
                break;
            }
            case CardAbility.HornUtil: {
                ApplyHornUtil(card, selfArea);
                break;
            }
            case CardAbility.Lip: {
                selfArea.AddBattleAreaCard(card);
                ApplyLip(enemyArea);
                break;
            }
            case CardAbility.Guard: {
                selfArea.AddBattleAreaCard(card);
                ApplyGuard(card, selfArea, enemyArea);
                break;
            }
            case CardAbility.Monaka: {
                ApplyMonaka(card, selfArea);
                selfArea.AddBattleAreaCard(card); // 要先判断monaka，再把牌打出去。不然就加强自己了
                break;
            }
            default: {
                selfArea.AddBattleAreaCard(card);
                break;
            }
        }
    }

    // 实施攻击牌技能。Gurad技能也复用这部分逻辑
    private void ApplyAttack(CardModelOld card, SinglePlayerAreaModelOld enemyArea)
    {
        // 统计可被攻击的牌数量
        int count = enemyArea.CountBattleAreaCard((CardModelOld targetCard) => {
            return targetCard.cardInfo.cardType == CardType.Normal;
        });
        if (count == 0) {
            UpdateActionToast(null, enemyArea, "无可攻击目标");
            KLog.I(TAG, "ApplyAttack: no target card");
            return;
        }
        UpdateActionToast(null, enemyArea, "请选择攻击目标");
        // 目标卡牌准备被攻击
        enemyArea.ApplyBattleAreaAction((CardModelOld targetCard) => {
            return targetCard.cardInfo.cardType == CardType.Normal;
        }, (CardModelOld targetCard) => {
            targetCard.selectType = CardSelectType.WithstandAttack;
        });
        attackCard = card;
        tracker.TransActionState(PlayStateTracker.ActionState.ATTACKING);
    }

    // 为被攻击的牌添加debuff
    private void ApplyBeAttacked(CardModelOld card, SinglePlayerAreaModelOld enemyArea)
    {
        if (attackCard == null) {
            KLog.E(TAG, "ApplyBeAttacked: attackCard is null");
            return;
        }
        int attackNum = attackCard.cardInfo.attackNum;
        if (attackNum == 2) {
            card.AddBuff(CardBuffType.Attack2, 1);
        } else {
            // 注意，此处guard技能也会走到
            card.AddBuff(CardBuffType.Attack4, 1);
        }
        // 攻击可能导致卡牌被移除
        if (card.IsDead()) {
            PlaySfx(AudioManager.SFXType.Scorch);
            RemoveDeadCard(enemyArea, new List<CardModelOld>{ card });
        } else {
            // 卡牌没被移除时，才播放攻击音效
            PlaySfx(AudioManager.SFXType.Attack);
        }
    }

    // 结束攻击技能的流程
    private void FinishAttack(SinglePlayerAreaModelOld enemyArea)
    {
        enemyArea.ApplyBattleAreaAction((CardModelOld card) => {
            return card.cardInfo.cardType == CardType.Normal;
        }, (CardModelOld card) => {
            card.selectType = CardSelectType.None;
        });
        attackCard = null;
    }

    // 应用伞击技能
    private void ApplyScorchWood(SinglePlayerAreaModelOld enemyArea)
    {
        List<CardModelOld> targetCardList = enemyArea.woodRowAreaModel.ApplyScorchWood();
        if (targetCardList.Count > 0) {
            PlaySfx(AudioManager.SFXType.Scorch);
        }
        RemoveDeadCard(enemyArea, targetCardList);
    }

    // 应用复活技能
    private void ApplyMedic(SinglePlayerAreaModelOld selfArea)
    {
        if (selfArea.discardAreaModel.normalCardList.Count == 0) {
            UpdateActionToast(selfArea, null, "无可复活目标");
            KLog.I(TAG, "ApplyMedic: no target card");
            return;
        }
        UpdateActionToast(selfArea, null, "请选择复活目标");
        foreach (CardModelOld card in selfArea.discardAreaModel.normalCardList) {
            card.selectType = CardSelectType.PlayCard;
        }
        selfArea.discardAreaModel.SetRow(true);
        tracker.TransActionState(PlayStateTracker.ActionState.MEDICING);
    }

    // 结束复活技能的流程
    private void FinishMedic(SinglePlayerAreaModelOld selfArea)
    {
        foreach (CardModelOld card in selfArea.discardAreaModel.normalCardList) {
            card.selectType = CardSelectType.None;
        }
        selfArea.discardAreaModel.ClearRow();
    }

    // 应用大号君技能
    private void ApplyDecoy(CardModelOld card, SinglePlayerAreaModelOld selfArea)
    {
        // 统计可被撤回的牌数量
        int count = selfArea.CountBattleAreaCard((CardModelOld targetCard) => {
            return targetCard.cardInfo.cardType == CardType.Normal;
        });
        if (count == 0) {
            card.cardLocation = CardLocation.None; // 直接丢入虚空
            UpdateActionToast(selfArea, null, "无可使用大号君的目标");
            KLog.I(TAG, "ApplyDecoy: no target card");
            return;
        }
        UpdateActionToast(selfArea, null, "请选择使用大号君的目标");
        // 目标卡牌准备被撤回
        selfArea.ApplyBattleAreaAction((CardModelOld targetCard) => {
            return targetCard.cardInfo.cardType == CardType.Normal;
        }, (CardModelOld targetCard) => {
            targetCard.selectType = CardSelectType.DecoyWithdraw;
        });
        decoyCard = card;
        tracker.TransActionState(PlayStateTracker.ActionState.DECOYING);
    }

    // 将decoy目标撤回手牌区
    private void ApplyBeWithdraw(CardModelOld card, SinglePlayerAreaModelOld selfArea)
    {
        if (decoyCard == null) {
            KLog.E(TAG, "ApplyBeWithdraw: decoyCard is null");
            return;
        }
        if (card.cardInfo.badgeType == CardBadgeType.Wood) {
            selfArea.woodRowAreaModel.AddCard(decoyCard);
            selfArea.woodRowAreaModel.RemoveCard(card);
        } else if (card.cardInfo.badgeType == CardBadgeType.Brass) {
            selfArea.brassRowAreaModel.AddCard(decoyCard);
            selfArea.brassRowAreaModel.RemoveCard(card);
        } else {
            selfArea.percussionRowAreaModel.AddCard(decoyCard);
            selfArea.percussionRowAreaModel.RemoveCard(card);
        }
        selfArea.handRowAreaModel.AddCard(card);
    }

    // 应用退部技能
    private void ApplyScorch(SinglePlayerAreaModelOld selfArea, SinglePlayerAreaModelOld enemyArea)
    {
        // 场上非英雄牌最高点数
        int maxPower = 0;
        selfArea.CountBattleAreaCard((CardModelOld targetCard) => {
            if (targetCard.cardInfo.cardType == CardType.Normal && targetCard.currentPower > maxPower) {
                maxPower = targetCard.currentPower;
            }
            return true;
        });
        enemyArea.CountBattleAreaCard((CardModelOld targetCard) => {
            if (targetCard.cardInfo.cardType == CardType.Normal && targetCard.currentPower > maxPower) {
                maxPower = targetCard.currentPower;
            }
            return true;
        });
        // 统计需要移除的牌
        List<CardModelOld> cardList = new List<CardModelOld>();
        List<CardModelOld> selfCardList = new List<CardModelOld>();
        List<CardModelOld> enemyCardList = new List<CardModelOld>();
        selfArea.CountBattleAreaCard((CardModelOld targetCard) => {
            if (targetCard.cardInfo.cardType == CardType.Normal && targetCard.currentPower == maxPower) {
                cardList.Add(targetCard);
                selfCardList.Add(targetCard);
            }
            return true;
        });
        enemyArea.CountBattleAreaCard((CardModelOld targetCard) => {
            if (targetCard.cardInfo.cardType == CardType.Normal && targetCard.currentPower == maxPower) {
                cardList.Add(targetCard);
                enemyCardList.Add(targetCard);
            }
            return true;
        });
        if (cardList.Count == 0) {
            UpdateActionToast(selfArea, enemyArea, "无退部目标");
            KLog.I(TAG, "ApplyScorch: no target card");
            return;
        }
        // 移除卡牌，比较丑陋，但是凑合着吧
        if (cardList.Count > 0) {
            PlaySfx(AudioManager.SFXType.Scorch);
        }
        RemoveDeadCard(selfArea, selfCardList);
        RemoveDeadCard(enemyArea, enemyCardList);
    }

    // 结束大号君技能的流程
    private void FinishDecoy(SinglePlayerAreaModelOld selfArea)
    {
        selfArea.ApplyBattleAreaAction((CardModelOld card) => {
            return card.cardInfo.cardType == CardType.Normal;
        }, (CardModelOld card) => {
            card.selectType = CardSelectType.None;
        });
        decoyCard = null;
    }

    // 应用天气技能
    private void ApplyWeather(CardModelOld card)
    {
        KLog.I(TAG, "ApplyWeather");
        List<CardModelOld> deadCardList = new List<CardModelOld>();
        weatherCardAreaModel.AddCard(card);
        // 添加天气buff
        if (card.cardInfo.ability == CardAbility.SunFes) {
            selfSinglePlayerAreaModel.woodRowAreaModel.hasWeatherBuff = true;
            enemySinglePlayerAreaModel.woodRowAreaModel.hasWeatherBuff = true;
        } else if (card.cardInfo.ability == CardAbility.Daisangakushou) {
            selfSinglePlayerAreaModel.brassRowAreaModel.hasWeatherBuff = true;
            enemySinglePlayerAreaModel.brassRowAreaModel.hasWeatherBuff = true;
        } else if (card.cardInfo.ability == CardAbility.Drumstick) {
            selfSinglePlayerAreaModel.percussionRowAreaModel.hasWeatherBuff = true;
            enemySinglePlayerAreaModel.percussionRowAreaModel.hasWeatherBuff = true;
        }
        // 可能造成卡牌移除
        bool hasDeadCard = false;
        selfSinglePlayerAreaModel.CountBattleAreaCard((CardModelOld targetCard) => {
            if (targetCard.IsDead()) {
                deadCardList.Add(targetCard);
            }
            return true;
        });
        hasDeadCard = hasDeadCard || deadCardList.Count > 0;
        RemoveDeadCard(selfSinglePlayerAreaModel, deadCardList);

        deadCardList = new List<CardModelOld>();
        enemySinglePlayerAreaModel.CountBattleAreaCard((CardModelOld targetCard) => {
            if (targetCard.IsDead()) {
                deadCardList.Add(targetCard);
            }
            return true;
        });
        hasDeadCard = hasDeadCard || deadCardList.Count > 0;
        RemoveDeadCard(enemySinglePlayerAreaModel, deadCardList);
        if (hasDeadCard) {
            PlaySfx(AudioManager.SFXType.Scorch);
        }
    }

    // 移除所有天气牌
    private void RemoveWeather()
    {
        KLog.I(TAG, "RemoveWeather");
        weatherCardAreaModel.RemoveAllCard();
        selfSinglePlayerAreaModel.woodRowAreaModel.hasWeatherBuff = false;
        selfSinglePlayerAreaModel.brassRowAreaModel.hasWeatherBuff = false;
        selfSinglePlayerAreaModel.percussionRowAreaModel.hasWeatherBuff = false;
        enemySinglePlayerAreaModel.woodRowAreaModel.hasWeatherBuff = false;
        enemySinglePlayerAreaModel.brassRowAreaModel.hasWeatherBuff = false;
        enemySinglePlayerAreaModel.percussionRowAreaModel.hasWeatherBuff = false;
    }

    // 应用HornUtil技能
    private void ApplyHornUtil(CardModelOld card, SinglePlayerAreaModelOld selfArea)
    {
        UpdateActionToast(selfArea, null, "请选择指导老师的目标行");
        hornUtilCard = card;
        tracker.TransActionState(PlayStateTracker.ActionState.HORN_UTILING);
    }

    // 结束HornUtil技能流程
    private void FinishHornUtil(SinglePlayerAreaModelOld selfArea)
    {
        hornUtilCard = null;
    }

    // 应用Lip技能
    private void ApplyLip(SinglePlayerAreaModelOld enemyArea)
    {
        int attackCardCount = 0;
        List<CardModelOld> deadCardList = new List<CardModelOld>();
        enemyArea.ApplyBattleAreaAction((CardModelOld card) => {
            return card.cardInfo.isMale;
        }, (CardModelOld card) => {
            attackCardCount += 1;
            card.AddBuff(CardBuffType.Attack2, 1);
            if (card.IsDead()) {
                deadCardList.Add(card);
            }
        });
        if (deadCardList.Count > 0) {
            PlaySfx(AudioManager.SFXType.Scorch);
        } else if (attackCardCount > 0) {
            PlaySfx(AudioManager.SFXType.Attack);
        }
        RemoveDeadCard(enemyArea, deadCardList);
    }

    // 应用Guard技能
    private void ApplyGuard(CardModelOld card, SinglePlayerAreaModelOld selfArea, SinglePlayerAreaModelOld enemyArea)
    {
        int relatedCardNum = 0; // 统计场上的Guard目标卡牌数量
        relatedCardNum += selfArea.CountBattleAreaCard((CardModelOld targetCard) => {
            return targetCard.cardInfo.chineseName == card.cardInfo.relatedCard;
        });
        relatedCardNum += enemyArea.CountBattleAreaCard((CardModelOld targetCard) => {
            return targetCard.cardInfo.chineseName == card.cardInfo.relatedCard;
        });
        if (selfArea.leaderCardAreaModel.cardList.Find(x => x.cardInfo.chineseName == card.cardInfo.chineseName) != null) {
            relatedCardNum += 1;
        }
        if (enemyArea.leaderCardAreaModel.cardList.Find(x => x.cardInfo.chineseName == card.cardInfo.chineseName) != null) {
            relatedCardNum += 1;
        }
        if (relatedCardNum == 0) {
            UpdateActionToast(null, enemyArea, "守卫目标不在场上，无法攻击");
            KLog.I(TAG, "ApplyAttack: no target card");
            return;
        }
        ApplyAttack(card, enemyArea);
    }

    // 应用monaka技能
    private void ApplyMonaka(CardModelOld card, SinglePlayerAreaModelOld selfArea)
    {
        // 统计可选择的牌数量
        int count = selfArea.CountBattleAreaCard((CardModelOld targetCard) => {
            return targetCard.cardInfo.cardType == CardType.Normal;
        });
        if (count == 0) {
            card.cardLocation = CardLocation.None; // 直接丢入虚空
            UpdateActionToast(selfArea, null, "无可使用monaka的目标");
            KLog.I(TAG, "ApplyMonaka: no target card");
            return;
        }
        UpdateActionToast(selfArea, null, "请选择使用monaka的目标");
        // 目标卡牌准备被monaka
        selfArea.ApplyBattleAreaAction((CardModelOld targetCard) => {
            return targetCard.cardInfo.cardType == CardType.Normal;
        }, (CardModelOld targetCard) => {
            targetCard.selectType = CardSelectType.Monaka;
        });
        tracker.TransActionState(PlayStateTracker.ActionState.MONAKAING);
    }

    // 生效monaka
    private void ApplyBeMonaka(CardModelOld card)
    {
        card.AddBuff(CardBuffType.Monaka, 1);
    }

    // 结束monaka技能的流程
    private void FinishMonaka(SinglePlayerAreaModelOld selfArea)
    {
        selfArea.ApplyBattleAreaAction((CardModelOld card) => {
            return card.cardInfo.cardType == CardType.Normal;
        }, (CardModelOld card) => {
            card.selectType = CardSelectType.None;
        });
    }

    private void RemoveDeadCard(SinglePlayerAreaModelOld playArea, List<CardModelOld> cardList)
    {
        if (cardList.Count > 0) {
            actionTextModel.ApplyScorch(cardList);
        }
        foreach (CardModelOld card in cardList) {
            KLog.I(TAG, "RemoveDeadCard: remove card: " + card.cardInfo.chineseName);
            if (playArea.woodRowAreaModel.cardList.Contains(card)) {
                playArea.woodRowAreaModel.RemoveCard(card);
            } else if (playArea.brassRowAreaModel.cardList.Contains(card)) {
                playArea.brassRowAreaModel.RemoveCard(card);
            } else if (playArea.percussionRowAreaModel.cardList.Contains(card)) {
                playArea.percussionRowAreaModel.RemoveCard(card);
            }
            playArea.discardAreaModel.AddCard(card);
        }
    }

    private void SetFinish()
    {
        int selfScore = selfSinglePlayerAreaModel.GetCurrentPower();
        int enemyScore = enemySinglePlayerAreaModel.GetCurrentPower();
        selfSinglePlayerAreaModel.RemoveAllBattleCard();
        enemySinglePlayerAreaModel.RemoveAllBattleCard();
        RemoveWeather();
        int lastSet = tracker.curSet;
        tracker.SetFinish(selfScore, enemyScore);
        actionTextModel.SetFinish(tracker.setRecordList[lastSet].result);
        if (tracker.curState == PlayStateTracker.State.WAIT_SELF_ACTION ||
            tracker.curState == PlayStateTracker.State.WAIT_ENEMY_ACTION) {
            PlaySfx(AudioManager.SFXType.SetFinish);
            actionTextModel.SetStart(tracker.setRecordList[tracker.curSet].selfFirst);
        } else if (tracker.isSelfWinner) {
            PlaySfx(AudioManager.SFXType.Win);
        }
        if (tracker.selfGroup == CardGroup.KumikoFirstYear && tracker.setRecordList[lastSet].result > 0) {
            DrawHandCardAndSend(1);
        }
    }

    private void UpdateActionToast(SinglePlayerAreaModelOld selfArea, SinglePlayerAreaModelOld enemyArea, string toastText)
    {
        if (!(selfArea == selfSinglePlayerAreaModel || enemyArea == enemySinglePlayerAreaModel)) {
            // 仅self时显示toast
            return;
        }
        actionTextModel.toastText = toastText;
    }

    private void PlaySfx(AudioManager.SFXType type)
    {
        if (SfxCallback != null) {
            SfxCallback(type);
        }
    }

    // 抽取备用卡牌，并发送消息
    private void DrawHandCardAndSend(int count)
    {
        List<CardModelOld> tempList = new List<CardModelOld>(selfSinglePlayerAreaModel.handRowAreaModel.cardList);
        selfSinglePlayerAreaModel.DrawHandCards(count);
        List<int> idList = new List<int>();
        foreach (CardModelOld handCard in selfSinglePlayerAreaModel.handRowAreaModel.cardList) {
            if (!tempList.Contains(handCard)) {
                idList.Add(handCard.cardInfo.id);
            }
        }
        if (idList.Count > 0) {
            battleModel.AddSelfActionMsg(BattleModel.ActionType.DrawHandCard, idList);
        }
    }
}
