using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

// 等级1 AI逻辑
class AIModelL1 : AIModelInterface
{
    private static string TAG = "AIModelL1";

    private Dictionary<CardGroup, int[][]> deckConfigDic = new Dictionary<CardGroup, int[][]>
    {
        {
            CardGroup.KumikoFirstYear,
            new int[][] {
                new int[] {
                    // wood
                    1003, // 喜多村来南 (Wood) - Bond，相关卡片：冈美贵乃
                    1004, // 冈美贵乃 (Wood) - Bond，相关卡片：喜多村来南
                    1005, // 加濑舞菜 (Wood) - Bond，相关卡片：高久智惠理
                    1006, // 高久智惠理 (Wood) - Bond，相关卡片：加濑舞菜
                    1007, // 鸟冢弘音 (Wood) - Tunning
                    1008, // 铃鹿咲子 (Wood) - Spy
                    1013, // 斋藤葵 (Wood) - Scorch
                    // brass
                    1021, // 田中明日香 (Brass) - Morale
                    1022, // 小笠原晴香 (Wood) - Morale
                    1024, // 瞳拉拉 (Brass) - Spy
                    // percussion
                    1019, // 田边名来 (Percussion) - Morale
                    1020, // 加山沙希 (Percussion) - 无技能
                    1029, // 井上顺菜 (Percussion) - Bond，相关卡片：堺万纱子
                    1030, // 堺万纱子 (Percussion) - Bond，相关卡片：井上顺菜
                    1041, // 川岛绿辉 (Percussion) - 无技能
                    // util
                    5002, // 退部申请书 - Scorch（工具卡）
                    5004, // 第三乐章 - Daisangakushou（工具卡）
                    // leader
                    5011, // 泷升 - Daisangakushou（领袖卡）
                },
                new int[] {
                    // wood
                    1003, // 喜多村来南 (Wood) - Bond，相关卡片：冈美贵乃
                    1004, // 冈美贵乃 (Wood) - Bond，相关卡片：喜多村来南
                    1008, // 铃鹿咲子 (Wood) - Spy
                    1022, // 小笠原晴香 (Wood) - Morale
                    // brass
                    1015, // 加桥比吕 (Brass) - Attack
                    1016, // 泽田树理 (Brass) - Spy
                    1021, // 田中明日香 (Brass) - Morale
                    1023, // 中世古香织 (Brass) - Medic
                    1027, // 加藤叶月 (Brass) - Horn
                    1036, // 岩田慧菜 (Brass) - Medic
                    1040, // 高坂丽奈 (Brass) - Hero卡，无特殊技能
                    1045, // 中川夏纪 (Brass) - Monaka
                    // percussion
                    1029, // 井上顺菜 (Percussion) - Bond，相关卡片：堺万纱子
                    1030, // 堺万纱子 (Percussion) - Bond，相关卡片：井上顺菜
                    1041, // 川岛绿辉 (Percussion) - Hero卡，无特殊技能
                    // util
                    5001, // 大号君 - Decoy（工具卡）
                    5002, // 退部申请书 - Scorch（工具卡）
                    5008, // 新山老师 - HornUtil（工具卡）
                    // leader
                    1080, // 田中明日香 - Medic（领袖卡）
                },
            }
        },
        {
            CardGroup.KumikoSecondYear,
            new int[][] {
                new int[] {
                    // wood
                    2005, // 铠冢霙 (Wood) - Hero卡，无特殊技能
                    2006, // 剑崎梨梨花 (Wood) - Bond，相关卡片：兜谷爱瑠、笼手山骏河
                    2007, // 兜谷爱瑠 (Wood) - Bond，相关卡片：剑崎梨梨花、笼手山骏河
                    2008, // 笼手山骏河 (Wood) - Bond，相关卡片：剑崎梨梨花、兜谷爱瑠
                    2011, // 小田芽衣子 (Wood) - Muster，相关卡片：高桥沙里、中野蕾实
                    2012, // 高桥沙里 (Wood) - Muster，相关卡片：小田芽衣子、中野蕾实
                    2013, // 中野蕾实 (Wood) - Muster，相关卡片：小田芽衣子、高桥沙里
                    // brass
                    2028, // 冢本秀一 (Brass) - 无技能
                    2030, // 叶加濑满 (Brass) - Spy技能
                    2032, // 瞳拉拉 (Brass) - Spy技能
                    2034, // 后藤卓也 (Brass) - Bond，相关卡片：长濑梨子
                    2035, // 长濑梨子 (Brass) - Bond，相关卡片：后藤卓也
                    // percussion
                    2042, // 川岛绿辉 (Percussion) - Hero卡，无特殊技能
                    2047, // 堺万纱子 (Percussion) - Bond，相关卡片：井上顺菜
                    2048, // 东浦心子 (Percussion) - 无技能
                    // util
                    5002, // 退部申请书 - Scorch（工具卡）
                    // leader
                    2080, // 黄前久美子 - HornBrass（领袖卡）
                },
                new int[] {
                    // wood
                    2005, // 铠冢霙 (Wood) - Hero卡，无特殊技能
                    2006, // 剑崎梨梨花 (Wood) - Bond，相关卡片：兜谷爱瑠、笼手山骏河
                    2007, // 兜谷爱瑠 (Wood) - Bond，相关卡片：剑崎梨梨花、笼手山骏河
                    2008, // 笼手山骏河 (Wood) - Bond，相关卡片：剑崎梨梨花、兜谷爱瑠
                    2010, // 伞木希美 (Wood) - Hero卡，ScorchWood技能
                    // brass
                    2020, // 吉川优子 (Brass) - Hero卡，Morale技能
                    2023, // 高坂丽奈 (Brass) - Hero卡，Attack技能
                    2027, // 岩田慧菜 (Brass) - Medic技能
                    2032, // 瞳拉拉 (Brass) - Spy技能
                    2034, // 后藤卓也 (Brass) - Bond，相关卡片：长濑梨子
                    2035, // 长濑梨子 (Brass) - Bond，相关卡片：后藤卓也
                    2040, // 黄前久美子 (Brass) - Hero卡，Medic技能
                    2041, // 久石奏 (Brass) - Hero卡，Spy技能
                    // percussion
                    2042, // 川岛绿辉 (Percussion) - Hero卡，无特殊技能
                    2044, // 大野美代子 (Percussion)
                    2045, // 井上顺菜 (Percussion) - Bond，相关卡片：堺万纱子
                    2047, // 堺万纱子 (Percussion) - Bond，相关卡片：井上顺菜
                    // util
                    5002, // 退部申请书 - Scorch（工具卡）
                    5003, // 日升祭
                    5004, // 第三乐章
                    // leader
                    5010, // 泷昇 - 指挥技能（领袖卡）
                },
            }
        },
    };

    // 初始化并设置牌组
    public AIModelL1(PlaySceneModel playSceneModel_) : base(playSceneModel_)
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
        var returnList = AIModelCommon.GetAllAction(playSceneModel.wholeAreaModel);
        var actionReturn = SelectActionReturn(returnList);
        if (returnList.Count == 0 || AIModelCommon.NeedPass(playSceneModel, returnList[0]) || actionReturn == null) {
            playSceneModel.Pass();
        } else {
            foreach (var action in actionReturn.actionList) {
                AIModelCommon.ApplyAction(playSceneModel, action);
            }
        }
    }

    private void SetDeckInfoIdList()
    {
        CardGroup cardGroup = playSceneModel.wholeAreaModel.playTracker.selfPlayerInfo.cardGroup;
        var groupDeckList = deckConfigDic[cardGroup];
        var selectDeckIndex = new Random().Next(groupDeckList.Length);
        var selectDeckList = new List<int>(groupDeckList[selectDeckIndex]);
        playSceneModel.SetBackupCardInfoIdList(selectDeckList);
    }

    // 选择操作
    private AIModelCommon.ActionReturn SelectActionReturn(List<AIModelCommon.ActionReturn> allAction)
    {
        var filterList = new List<AIModelCommon.ActionReturn>();
        foreach (var action in allAction) {
            if (action.Total() < 0) {
                break;
            }
            filterList.Add(action);
        }
        if (filterList.Count == 0) {
            if (allAction.Count > 3) {
                KLog.I(TAG, "SelectAction: use first action, allAction.Count = " + allAction.Count);
                return allAction[0]; // 还有后续操作，暂时负收益试一试
            } else {
                KLog.I(TAG, "SelectAction: no valid action found, allAction.Count = " + allAction.Count);
                return null;
            }
        }
        int actionIndex = GetActionIndexLinearProbabilities(filterList.Count);
        KLog.I(TAG, "SelectAction: actionIndex = " + actionIndex + ", filterList.Count = " + filterList.Count + ", total count = " + allAction.Count);
        return allAction[actionIndex];
    }

    // 线性分布 获取操作index
    private int GetActionIndexLinearProbabilities(int num)
    {
        double total = num * (num + 1) / 2.0;
        List<double> probabilities = new List<double>();
        for (int i = num; i >= 1; i--) {
            probabilities.Add(i / total);
        }
        double randomValue = new Random().NextDouble(); // 生成一个0到1之间的随机数
        double cumulativeProbability = 0.0;
        for (int i = 0; i < num; i++) {
            cumulativeProbability += probabilities[i];
            if (randomValue <= cumulativeProbability) {
                return i;
            }
        }
        return 0;
    }
}
