using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;

// AI逻辑模块统一接口
public class AIModelInterface
{
    private static string TAG = "AIModelInterface";

    public PlaySceneModel playSceneModel;

    protected AIModelInit aiModelInit;

    protected AIModelCommon aiModelCommon;

    protected AIBase.AIMode aiMode; // 耗时操作中，是否需要await来避免ui卡死

    // 初始化并设置牌组
    public AIModelInterface(PlaySceneModel playSceneModel_, AIBase.AIMode aiMode_)
    {
        playSceneModel = playSceneModel_;
        aiMode = aiMode_;
        aiModelInit = new AIModelInit(playSceneModel);
    }

    // 初始化手牌，并完成重抽手牌操作
    public virtual void DoInitHandCard()
    {

    }

    // WAIT_SELF_ACTION时的操作
    public virtual async UniTask DoPlayAction()
    {
        await UniTask.Yield();
    }

    protected void SetDeckInfoIdList(List<int> deckList)
    {
        if (deckList == null) {
            CardGroup cardGroup = playSceneModel.wholeAreaModel.playTracker.selfPlayerInfo.cardGroup;
            var groupDeckList = AIDefaultDeck.deckConfigDic[cardGroup];
            var selectDeckIndex = new Random().Next(groupDeckList.Length);
            deckList = new List<int>(groupDeckList[selectDeckIndex]);
        }
        playSceneModel.SetBackupCardInfoIdList(deckList);
    }

    protected void ApplyAction(PlaySceneModel model, ActionEvent actionEvent)
    {
        switch (actionEvent.args[0]) {
            case BattleModel.ActionType.ChooseCard: {
                int id = (int)actionEvent.args[1];
                CardModel card = model.wholeAreaModel.FindCard(id);
                if (card == null) {
                    KLog.E(TAG, "ApplyAction: ChooseCard: invalid id: " + id);
                    return;
                }
                model.ChooseCard(card);
                break;
            }
            case BattleModel.ActionType.Pass: {
                model.Pass();
                break;
            }
            case BattleModel.ActionType.ChooseHornUtilArea: {
                model.ChooseHornUtilArea((CardBadgeType)actionEvent.args[1]);
                break;
            }
            default: {
                KLog.E(TAG, "ApplyAction: invalid type = " + actionEvent.args[0]);
                break;
            }
        }
    }
}
