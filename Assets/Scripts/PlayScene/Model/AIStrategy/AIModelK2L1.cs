using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

// 久一年牌组AI逻辑，kumiko 1
class AIModelK2L1 : AIModelInterface
{
    private static string TAG = "AIModelK1L1";

    // 初始化并设置牌组
    public AIModelK2L1(PlaySceneModel playSceneModel_) : base(playSceneModel_)
    {
        // 设置牌组
        SetDeckInfoIdList();
    }

    // 初始化手牌，并完成重抽手牌操作
    public override void DoInitHandCard()
    {
        playSceneModel.DrawInitHandCard();
        aiModelInit.ChooseReDrawInitHandCard();
        playSceneModel.ReDrawInitHandCard();
    }

    // WAIT_SELF_ACTION时的操作
    public override void DoPlayAction()
    {
        int scoreDiff = AIModelCommon.GetScoreDiff(playSceneModel.wholeAreaModel);
        if (scoreDiff > 0 && playSceneModel.wholeAreaModel.gameState.enemyPass) {
            KLog.I(TAG, "DoPlayAction: scoreDiff: " + scoreDiff + ", enemyPass: true, will pass");
            playSceneModel.Pass();
            return;
        }
        var maxReturn = AIModelCommon.GetMaxReturnAction(playSceneModel.wholeAreaModel, out List<ActionEvent> actionList);
        if (actionList.Count == 0 || AIModelCommon.NeedPass(playSceneModel, maxReturn)) {
            playSceneModel.Pass();
        } else {
            foreach (var action in actionList) {
                AIModelCommon.ApplyAction(playSceneModel, action);
            }
        }
    }

    private void SetDeckInfoIdList()
    {
        List<int> infoIdList = new List<int>();
        // wood
        infoIdList.Add(2005); // 铠冢霙 (Wood) - Hero卡，无特殊技能
        infoIdList.Add(2006); // 剑崎梨梨花 (Wood) - Bond，相关卡片：兜谷爱瑠、笼手山骏河
        infoIdList.Add(2007); // 兜谷爱瑠 (Wood) - Bond，相关卡片：剑崎梨梨花、笼手山骏河
        infoIdList.Add(2008); // 笼手山骏河 (Wood) - Bond，相关卡片：剑崎梨梨花、兜谷爱瑠
        infoIdList.Add(2011); // 小田芽衣子 (Wood) - Muster，相关卡片：高桥沙里、中野蕾实
        infoIdList.Add(2012); // 高桥沙里 (Wood) - Muster，相关卡片：小田芽衣子、中野蕾实
        infoIdList.Add(2013); // 中野蕾实 (Wood) - Muster，相关卡片：小田芽衣子、高桥沙里

        // brass
        infoIdList.Add(2028); // 冢本秀一 (Brass) - 无技能
        infoIdList.Add(2034); // 后藤卓也 (Brass) - Bond，相关卡片：长濑梨子
        infoIdList.Add(2035); // 长濑梨子 (Brass) - Bond，相关卡片：后藤卓也

        // percussion
        infoIdList.Add(2042); // 川岛绿辉 (Percussion) - Hero卡，无特殊技能
        infoIdList.Add(2047); // 堺万纱子 (Percussion) - Bond，相关卡片：井上顺菜
        infoIdList.Add(2048); // 东浦心子 (Percussion) - 无技能

        // util
        infoIdList.Add(5002); // 退部申请书 - Scorch（工具卡）

        // leader
        infoIdList.Add(2080); // 黄前久美子 - HornBrass（领袖卡）
        playSceneModel.SetBackupCardInfoIdList(infoIdList);
    }
}
