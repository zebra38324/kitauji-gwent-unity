using System;
using System.Collections.Generic;

// AI逻辑模块统一接口
class AIModelInterface
{
    public PlaySceneModel playSceneModel;

    protected AIModelInit aiModelInit;

    // 初始化并设置牌组
    public AIModelInterface(PlaySceneModel playSceneModel_, List<int> deckList = null)
    {
        playSceneModel = playSceneModel_;
        aiModelInit = new AIModelInit(playSceneModel);
    }

    // 初始化手牌，并完成重抽手牌操作
    public virtual void DoInitHandCard()
    {

    }

    // WAIT_SELF_ACTION时的操作
    public virtual void DoPlayAction()
    {

    }
}
