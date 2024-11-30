using System.Collections.Generic;
using System.Linq;
using System.Threading;

// 对战ai管理器接口，由PlaySceneManager调用
public class PlaySceneAI
{
    public PlaySceneModel playSceneModel {  get; private set; }

    private bool isAbort = false;

    private Thread aiThread;

    public PlaySceneAI()
    {
        playSceneModel = new PlaySceneModel(false);
    }

    ~PlaySceneAI()
    {
        isAbort = true;
        if (aiThread.IsAlive) {
            aiThread.Join();
            aiThread = null;
        }
    }

    public void Start(bool selfStart = false)
    {
        List<int> infoIdList = Enumerable.Range(2001, 15).ToList();
        playSceneModel.SetBackupCardInfoIdList(infoIdList);
        aiThread = new Thread(() => {
            AIThread();
        });
        aiThread.Start();
    }

    private void AIThread()
    {
        while (!isAbort && playSceneModel.tracker.curState != PlayStateTracker.State.WAIT_INIT_HAND_CARD) {
            Thread.Sleep(1);
        }
        playSceneModel.DrawInitHandCard();
        while (!isAbort && playSceneModel.tracker.curState != PlayStateTracker.State.WAIT_START) {
            Thread.Sleep(1);
        }
        playSceneModel.StartSet(false);
        while (!isAbort) {
            if (playSceneModel.tracker.curState != PlayStateTracker.State.WAIT_SELF_ACTION ||
                playSceneModel.selfSinglePlayerAreaModel.handRowAreaModel.cardList.Count == 0) {
                Thread.Sleep(1);
                continue;
            }
            Thread.Sleep(1000); // 延迟一秒
            CardModel handCard = playSceneModel.selfSinglePlayerAreaModel.handRowAreaModel.cardList[0];
            playSceneModel.ChooseCard(handCard);
            while (playSceneModel.tracker.curState == PlayStateTracker.State.WAIT_SELF_ACTION &&
                playSceneModel.tracker.actionState != PlayStateTracker.ActionState.None) {
                if (playSceneModel.tracker.actionState == PlayStateTracker.ActionState.ATTACKING) {
                    CardModel targetCard = null;
                    if (playSceneModel.selfSinglePlayerAreaModel.woodRowAreaModel.cardList.Count > 0) {
                        targetCard = playSceneModel.selfSinglePlayerAreaModel.woodRowAreaModel.cardList[0];
                    } else if (playSceneModel.selfSinglePlayerAreaModel.brassRowAreaModel.cardList.Count > 0) {
                        targetCard = playSceneModel.selfSinglePlayerAreaModel.brassRowAreaModel.cardList[0];
                    } else {
                        targetCard = playSceneModel.selfSinglePlayerAreaModel.percussionRowAreaModel.cardList[0];
                    }
                    playSceneModel.ChooseCard(targetCard);
                } else if (playSceneModel.tracker.actionState == PlayStateTracker.ActionState.MEDICING) {
                    playSceneModel.ChooseCard(playSceneModel.selfSinglePlayerAreaModel.discardAreaModel.rowAreaList[0].cardList[0]);
                }
            }
        }
    }
}
