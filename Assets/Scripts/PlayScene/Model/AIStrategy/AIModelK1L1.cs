using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

// 久一年牌组AI逻辑，kumiko 1
class AIModelK1L1 : AIModelInterface
{
    private static string TAG = "AIModelK1L1";

    // 初始化并设置牌组
    public AIModelK1L1(PlaySceneModel playSceneModel_) : base(playSceneModel_)
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
        // 这套牌组的思路：主打木管、打击乐。点数主要靠bond。
        // 包含的技能：bond、tunning、morale、spy(2)、退部、铜管天气
        List<int> infoIdList = new List<int>();
        // wood
        infoIdList.Add(1003); // 喜多村来南 (Wood) - Bond，相关卡片：冈美贵乃
        infoIdList.Add(1004); // 冈美贵乃 (Wood) - Bond，相关卡片：喜多村来南
        infoIdList.Add(1005); // 加濑舞菜 (Wood) - Bond，相关卡片：高久智惠理
        infoIdList.Add(1006); // 高久智惠理 (Wood) - Bond，相关卡片：加濑舞菜
        infoIdList.Add(1007); // 鸟冢弘音 (Wood) - Tunning
        infoIdList.Add(1008); // 铃鹿咲子 (Wood) - Spy
        infoIdList.Add(1013); // 斋藤葵 (Wood) - Scorch
        // brass
        infoIdList.Add(1021); // 田中明日香 (Brass) - Morale
        infoIdList.Add(1022); // 小笠原晴香 (Wood) - Morale
        infoIdList.Add(1024); // 瞳拉拉 (Brass) - Spy
        // percussion
        infoIdList.Add(1019); // 田边名来 (Percussion) - Morale
        infoIdList.Add(1020); // 加山沙希 (Percussion) - 无技能
        infoIdList.Add(1029); // 井上顺菜 (Percussion) - Bond，相关卡片：堺万纱子
        infoIdList.Add(1030); // 堺万纱子 (Percussion) - Bond，相关卡片：井上顺菜
        infoIdList.Add(1041); // 川岛绿辉 (Percussion) - 无技能
        // util
        infoIdList.Add(5002); // 退部申请书 - Scorch（工具卡）
        infoIdList.Add(5004); // 第三乐章 - Daisangakushou（工具卡）
        // leader
        infoIdList.Add(5011); // 泷升 - Daisangakushou（领袖卡）
        playSceneModel.SetBackupCardInfoIdList(infoIdList);
    }
}
