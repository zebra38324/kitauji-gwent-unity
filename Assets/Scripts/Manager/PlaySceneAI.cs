using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;

// 对战ai管理器接口，由PlaySceneManager调用
public class PlaySceneAI
{
    public PlaySceneModel playSceneModel {  get; private set; }

    private bool isAbort = false;

    public PlaySceneAI()
    {
        playSceneModel = new PlaySceneModel(false);
    }

    public void Release()
    {
        isAbort = true;
    }

    public void Start(bool selfStart = false)
    {
        List<int> infoIdList = Enumerable.Range(2001, 9).ToList();
        infoIdList.Add(5001);
        infoIdList.Add(5010);
        playSceneModel.SetBackupCardInfoIdList(infoIdList);
        AICoroutine();
    }

    private async void AICoroutine()
    {
        int selfPlayCardNum = 0;
        while (!isAbort && playSceneModel.tracker.curState != PlayStateTracker.State.WAIT_INIT_HAND_CARD) {
            await UniTask.Delay(1);
        }
        playSceneModel.DrawInitHandCard();
        playSceneModel.ReDrawInitHandCard();
        while (!isAbort &&
               playSceneModel.tracker.curState != PlayStateTracker.State.WAIT_SELF_ACTION && 
               playSceneModel.tracker.curState != PlayStateTracker.State.WAIT_ENEMY_ACTION) {
            await UniTask.Delay(1);
        }
        while (!isAbort) {
            if (playSceneModel.tracker.curState != PlayStateTracker.State.WAIT_SELF_ACTION ||
                playSceneModel.selfSinglePlayerAreaModel.handRowAreaModel.cardList.Count == 0) {
                await UniTask.Delay(1);
                continue;
            }
            await UniTask.Delay(1000); // 延迟一秒
            if (selfPlayCardNum >= 3) {
                selfPlayCardNum = 0;
                playSceneModel.Pass();
                continue;
            }
            selfPlayCardNum += 1;
            if (playSceneModel.selfSinglePlayerAreaModel.leaderCardAreaModel.cardList.Count > 0) {
                playSceneModel.ChooseCard(playSceneModel.selfSinglePlayerAreaModel.leaderCardAreaModel.cardList[0]);
                continue;
            }
            CardModel handCard = playSceneModel.selfSinglePlayerAreaModel.handRowAreaModel.cardList[0];
            playSceneModel.ChooseCard(handCard);
            while (playSceneModel.tracker.curState == PlayStateTracker.State.WAIT_SELF_ACTION &&
                playSceneModel.tracker.actionState != PlayStateTracker.ActionState.None) {
                await UniTask.Delay(1000); // 延迟一秒
                if (playSceneModel.tracker.actionState == PlayStateTracker.ActionState.ATTACKING) {
                    playSceneModel.ChooseCard(FindNormalCard(playSceneModel.enemySinglePlayerAreaModel));
                } else if (playSceneModel.tracker.actionState == PlayStateTracker.ActionState.MEDICING) {
                    playSceneModel.ChooseCard(playSceneModel.selfSinglePlayerAreaModel.discardAreaModel.rowAreaList[0].cardList[0]);
                } else if (playSceneModel.tracker.actionState == PlayStateTracker.ActionState.DECOYING) {
                    playSceneModel.ChooseCard(FindNormalCard(playSceneModel.selfSinglePlayerAreaModel));
                }
            }
        }
    }

    private CardModel FindNormalCard(SinglePlayerAreaModel singlePlayerAreaModel)
    {
        foreach (CardModel cardModel in singlePlayerAreaModel.woodRowAreaModel.cardList) {
            if (cardModel.cardInfo.cardType == CardType.Normal) {
                return cardModel;
            }
        }
        foreach (CardModel cardModel in singlePlayerAreaModel.brassRowAreaModel.cardList) {
            if (cardModel.cardInfo.cardType == CardType.Normal) {
                return cardModel;
            }
        }
        foreach (CardModel cardModel in singlePlayerAreaModel.percussionRowAreaModel.cardList) {
            if (cardModel.cardInfo.cardType == CardType.Normal) {
                return cardModel;
            }
        }
        return null;
    }
}
