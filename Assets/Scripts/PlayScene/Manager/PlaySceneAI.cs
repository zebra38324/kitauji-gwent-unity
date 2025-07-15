using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;

// 对战ai管理器接口，由PlaySceneManager调用
public class PlaySceneAI
{
    public PlaySceneModel playSceneModel { get; private set; }

    public enum AIType
    {
        // L1
        L1K1 = 0,
        L1K2,
        L1K3,
        // L2
        L2K1,
        L2K2,
        L2K3,
    }

    private bool isAbort = false;

    private AIModelInterface aiModelInterface;

    public PlaySceneAI(bool isHost_,
        string selfName,
        string enemyName,
        AIType aiType)
    {
        playSceneModel = new PlaySceneModel(isHost_,
            selfName,
            enemyName,
            GetAIGroup(aiType),
            events => {}
        );
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
        while (!isAbort && playSceneModel.wholeAreaModel.gameState.curState != GameState.State.WAIT_INIT_HAND_CARD) {
            await UniTask.Delay(1);
        }
        aiModelInterface.DoInitHandCard();
        while (!isAbort &&
               playSceneModel.wholeAreaModel.gameState.curState != GameState.State.WAIT_SELF_ACTION && 
               playSceneModel.wholeAreaModel.gameState.curState != GameState.State.WAIT_ENEMY_ACTION) {
            await UniTask.Delay(1);
        }
        while (!isAbort) {
            if (playSceneModel.wholeAreaModel.gameState.curState != GameState.State.WAIT_SELF_ACTION) {
                await UniTask.Delay(1);
                continue;
            }
            await UniTask.Delay(1000); // 延迟一秒
            await aiModelInterface.DoPlayAction();
        }
    }

    private CardGroup GetAIGroup(AIType aiType)
    {
        switch (aiType) {
            case AIType.L1K1:
            case AIType.L2K1: {
                return CardGroup.KumikoFirstYear;
            }
            case AIType.L1K2:
            case AIType.L2K2: {
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
            case AIType.L1K1:
            case AIType.L1K2: {
                return new AIModelL1(playSceneModel);
            }
            case AIType.L2K1:
            case AIType.L2K2: {
                return new AIModelL2(playSceneModel);
            }
            default: {
                return null;
            }
        }
    }
}
