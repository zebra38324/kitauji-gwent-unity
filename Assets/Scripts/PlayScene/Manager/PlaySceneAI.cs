using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;

// 对战ai管理器接口，由PlaySceneManager调用
public class PlaySceneAI
{
    public PlaySceneModel playSceneModel { get; private set; }

    public enum AIType
    {
        K1Basic = 0,
        K2Basic,
    }

    private bool isAbort = false;

    private AIModelInterface aiModelInterface;

    public PlaySceneAI(bool isHost_,
        string selfName,
        string enemyName,
        AIType aiType)
    {
        playSceneModel = new PlaySceneModel(isHost_, selfName, enemyName, GetAIGroup(aiType));
        aiModelInterface = GetAIImpl(aiType, playSceneModel);
    }

    public void Release()
    {
        isAbort = true;
    }

    public void Start(bool selfStart = false)
    {
        AICoroutine();
    }

    private async void AICoroutine()
    {
        while (!isAbort && playSceneModel.tracker.curState != PlayStateTracker.State.WAIT_INIT_HAND_CARD) {
            await UniTask.Delay(1);
        }
        aiModelInterface.DoInitHandCard();
        while (!isAbort &&
               playSceneModel.tracker.curState != PlayStateTracker.State.WAIT_SELF_ACTION && 
               playSceneModel.tracker.curState != PlayStateTracker.State.WAIT_ENEMY_ACTION) {
            await UniTask.Delay(1);
        }
        while (!isAbort) {
            if (playSceneModel.tracker.curState != PlayStateTracker.State.WAIT_SELF_ACTION) {
                await UniTask.Delay(1);
                continue;
            }
            await UniTask.Delay(1000); // 延迟一秒
            aiModelInterface.DoPlayAction();
        }
    }

    private CardGroup GetAIGroup(AIType aiType)
    {
        switch (aiType) {
            case AIType.K1Basic: {
                return CardGroup.KumikoFirstYear;
            }
            case AIType.K2Basic: {
                return CardGroup.KumikoSecondYear;
            }
            default: {
                return CardGroup.KumikoFirstYear;
            }
        }
    }

    private AIModelInterface GetAIImpl(AIType aiType, PlaySceneModel playSceneModel)
    {
        switch (aiType) {
            case AIType.K1Basic: {
                return new AIModelK1Basic(playSceneModel);
            }
            case AIType.K2Basic: {
                return new AIModelK2Basic(playSceneModel);
            }
            default: {
                return null;
            }
        }
    }
}
