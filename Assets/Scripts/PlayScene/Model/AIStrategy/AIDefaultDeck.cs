using System.Collections;
using System.Collections.Generic;

public class AIDefaultDeck
{
    // 每个牌组由前至后，强度逐渐提高
    public static readonly Dictionary<CardGroup, int[][]> deckConfigDic = new Dictionary<CardGroup, int[][]>
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
                    1010, // 宫桐子 (Wood) - Muster，相关卡片：平尾澄子、桥弘江
                    1011, // 桥弘江 (Wood) - Muster，相关卡片：宫桐子、平尾澄子
                    1012, // 平尾澄子 (Wood) - Muster，相关卡片：宫桐子、桥弘江
                    1013, // 斋藤葵 (Wood) - Scorch
                    1022, // 小笠原晴香 (Wood) - Morale
                    // brass
                    1021, // 田中明日香 (Brass) - Morale
                    1023, // 中世古香织 (Brass) - Medic
                    1024, // 瞳拉拉 (Brass) - Spy
                    1044, // 吉川优子 (Brass) - Guard，相关卡片：中世古香织
                    // percussion
                    1019, // 田边名来 (Percussion) - Morale
                    1020, // 加山沙希 (Percussion) - 无技能
                    1029, // 井上顺菜 (Percussion) - Bond，相关卡片：堺万纱子
                    1030, // 堺万纱子 (Percussion) - Bond，相关卡片：井上顺菜
                    1041, // 川岛绿辉 (Percussion) - TubaAlliance
                    // util
                    5002, // 退部申请书 - Scorch（工具卡）
                    5004, // 第三乐章 - Daisangakushou（工具卡）
                    5006, // 大吉山 - ClearWeather（工具卡）
                    // leader
                    5011, // 泷升 - Daisangakushou（领袖卡）
                },
                new int[] {
                    // wood
                    1003, // 喜多村来南 (Wood) - Bond，相关卡片：冈美贵乃
                    1004, // 冈美贵乃 (Wood) - Bond，相关卡片：喜多村来南
                    1007, // 鸟冢弘音 (Wood) - Tunning
                    1008, // 铃鹿咲子 (Wood) - Spy
                    1010, // 宫桐子 (Wood) - Muster，相关卡片：平尾澄子、桥弘江
                    1011, // 桥弘江 (Wood) - Muster，相关卡片：宫桐子、平尾澄子
                    1012, // 平尾澄子 (Wood) - Muster，相关卡片：宫桐子、桥弘江
                    1013, // 斋藤葵 (Wood) - Scorch
                    1022, // 小笠原晴香 (Wood) - Morale
                    // brass
                    1015, // 加桥比吕 (Brass) - Attack
                    1016, // 泽田树理 (Brass) - Spy
                    1021, // 田中明日香 (Brass) - Morale
                    1023, // 中世古香织 (Brass) - Medic
                    1027, // 加藤叶月 (Brass) - Horn
                    1036, // 岩田慧菜 (Brass) - Medic
                    1040, // 高坂丽奈 (Brass) - Hero卡，无特殊技能
                    1044, // 吉川优子 (Brass) - Guard，相关卡片：中世古香织
                    1045, // 中川夏纪 (Brass) - Monaka
                    // percussion
                    1019, // 田边名来 (Percussion) - Morale
                    1029, // 井上顺菜 (Percussion) - Bond，相关卡片：堺万纱子
                    1030, // 堺万纱子 (Percussion) - Bond，相关卡片：井上顺菜
                    1041, // 川岛绿辉 (Percussion) - Hero卡，TubaAlliance
                    // util
                    5001, // 大号君 - Decoy（工具卡）
                    5002, // 退部申请书 - Scorch（工具卡）
                    5006, // 大吉山 - ClearWeather（工具卡）
                    5008, // 新山老师 - HornUtil（工具卡）
                    // leader
                    1080, // 田中明日香 - Medic（领袖卡）
                },
                new int[] {
                    // wood
                    1003, // 喜多村来南 (Wood) - Bond，相关卡片：冈美贵乃
                    1004, // 冈美贵乃 (Wood) - Bond，相关卡片：喜多村来南
                    1005, // 加濑舞菜 (Wood) - Bond，相关卡片：高久智惠理
                    1006, // 高久智惠理 (Wood) - Bond，相关卡片：加濑舞菜
                    1007, // 鸟冢弘音 (Wood) - Tunning
                    1008, // 铃鹿咲子 (Wood) - Spy
                    1010, // 宫桐子 (Wood) - Muster，相关卡片：平尾澄子、桥弘江
                    1011, // 桥弘江 (Wood) - Muster，相关卡片：宫桐子、平尾澄子
                    1012, // 平尾澄子 (Wood) - Muster，相关卡片：宫桐子、桥弘江
                    1013, // 斋藤葵 (Wood) - Scorch
                    1022, // 小笠原晴香 (Wood) - Morale
                    1037, // 牧誓 (Wood) - TubaAlliance
                    1051, // 铠冢霙 (Wood)
                    1052, // 伞木希美 (Wood) - Kasa
                    // brass
                    1015, // 加桥比吕 (Brass) - Attack
                    1016, // 泽田树理 (Brass) - Spy
                    1021, // 田中明日香 (Brass) - Morale
                    1023, // 中世古香织 (Brass) - Medic
                    1024, // 铃鹿咲子 (瞳拉拉) - Spy
                    1027, // 加藤叶月 (Brass) - Horn
                    1036, // 岩田慧菜 (Brass) - Medic
                    1040, // 高坂丽奈 (Brass) - Hero卡，无特殊技能
                    1044, // 吉川优子 (Brass) - Guard，相关卡片：中世古香织
                    // percussion
                    1019, // 田边名来 (Percussion) - Morale
                    1029, // 井上顺菜 (Percussion) - Bond，相关卡片：堺万纱子
                    1030, // 堺万纱子 (Percussion) - Bond，相关卡片：井上顺菜
                    1041, // 川岛绿辉 (Percussion) - Hero卡，TubaAlliance
                    // util
                    5001, // 大号君 - Decoy（工具卡）
                    5002, // 退部申请书 - Scorch（工具卡）
                    5006, // 大吉山 - ClearWeather（工具卡）
                    5008, // 新山老师 - HornUtil（工具卡）
                    5013, // 大号君 - Decoy（工具卡）
                    5014, // 退部申请书 - Scorch（工具卡）
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
                    2001, // 岛理惠 (Wood) - Tunning
                    2005, // 铠冢霙 (Wood) - Hero卡，无特殊技能
                    2006, // 剑崎梨梨花 (Wood) - Bond，相关卡片：兜谷爱瑠、笼手山骏河
                    2007, // 兜谷爱瑠 (Wood) - Bond，相关卡片：剑崎梨梨花、笼手山骏河
                    2008, // 笼手山骏河 (Wood) - Bond，相关卡片：剑崎梨梨花、兜谷爱瑠
                    2011, // 小田芽衣子 (Wood) - Muster，相关卡片：高桥沙里、中野蕾实
                    2012, // 高桥沙里 (Wood) - Muster，相关卡片：小田芽衣子、中野蕾实
                    2013, // 中野蕾实 (Wood) - Muster，相关卡片：小田芽衣子、高桥沙里
                    // brass
                    2020, // 吉川优子 (Brass) - Hero卡，Morale技能
                    2023, // 高坂丽奈 (Brass) - Hero卡，Attack技能
                    2028, // 冢本秀一 (Brass) - 无技能
                    2030, // 叶加濑满 (Brass) - Spy技能
                    2032, // 瞳拉拉 (Brass) - Spy技能
                    2034, // 后藤卓也 (Brass) - Bond，相关卡片：长濑梨子
                    2035, // 长濑梨子 (Brass) - Bond，相关卡片：后藤卓也
                    2040, // 黄前久美子 (Brass) - Hero卡，Medic技能
                    // percussion
                    2042, // 川岛绿辉 (Percussion) - Hero卡，TubaAlliance
                    2047, // 堺万纱子 (Percussion) - Bond，相关卡片：井上顺菜
                    2048, // 东浦心子 (Percussion) - 无技能
                    // util
                    5002, // 退部申请书 - Scorch（工具卡）
                    // leader
                    2080, // 黄前久美子 - HornBrass（领袖卡）
                },
                new int[] {
                    // wood
                    2001, // 岛理惠 (Wood) - Tunning
                    2003, // 高久智惠理 (Wood) - Bond，相关卡片：泷川近夫
                    2005, // 铠冢霙 (Wood) - Hero卡，无特殊技能
                    2006, // 剑崎梨梨花 (Wood) - Bond，相关卡片：兜谷爱瑠、笼手山骏河
                    2007, // 兜谷爱瑠 (Wood) - Bond，相关卡片：剑崎梨梨花、笼手山骏河
                    2008, // 笼手山骏河 (Wood) - Bond，相关卡片：剑崎梨梨花、兜谷爱瑠
                    2010, // 伞木希美 (Wood) - Hero卡，ScorchWood技能
                    2011, // 小田芽衣子 (Wood) - Muster，相关卡片：高桥沙里、中野蕾实
                    2012, // 高桥沙里 (Wood) - Muster，相关卡片：小田芽衣子、中野蕾实
                    2013, // 中野蕾实 (Wood) - Muster，相关卡片：小田芽衣子、高桥沙里
                    2018, // 泷川近夫 (Wood) - Bond，相关卡片：高久智惠理
                    // brass
                    2020, // 吉川优子 (Brass) - Hero卡，Morale技能
                    2021, // 加部友惠 (Brass) - Medic技能
                    2023, // 高坂丽奈 (Brass) - Hero卡，Attack技能
                    2027, // 岩田慧菜 (Brass) - Medic技能
                    2032, // 瞳拉拉 (Brass) - Spy技能
                    2034, // 后藤卓也 (Brass) - Bond，相关卡片：长濑梨子
                    2035, // 长濑梨子 (Brass) - Bond，相关卡片：后藤卓也
                    2040, // 黄前久美子 (Brass) - Hero卡，Medic技能
                    2041, // 久石奏 (Brass) - Hero卡，Spy技能
                    // percussion
                    2042, // 川岛绿辉 (Percussion) - Hero卡，TubaAlliance
                    2045, // 井上顺菜 (Percussion) - Bond，相关卡片：堺万纱子
                    2047, // 堺万纱子 (Percussion) - Bond，相关卡片：井上顺菜
                    // util
                    5001, // 大号君 - Decoy（工具卡）
                    5002, // 退部申请书 - Scorch（工具卡）
                    5003, // 日升祭
                    5004, // 第三乐章
                    5008, // 新山老师 - HornUtil（工具卡）
                    // leader
                    5010, // 泷昇 - 指挥技能（领袖卡）
                },
                new int[] {
                    // wood
                    2001, // 岛理惠 (Wood) - Tunning
                    2003, // 高久智惠理 (Wood) - Bond，相关卡片：泷川近夫
                    2005, // 铠冢霙 (Wood) - Hero卡，无特殊技能
                    2006, // 剑崎梨梨花 (Wood) - Bond，相关卡片：兜谷爱瑠、笼手山骏河
                    2007, // 兜谷爱瑠 (Wood) - Bond，相关卡片：剑崎梨梨花、笼手山骏河
                    2008, // 笼手山骏河 (Wood) - Bond，相关卡片：剑崎梨梨花、兜谷爱瑠
                    2010, // 伞木希美 (Wood) - Hero卡，ScorchWood技能
                    2011, // 小田芽衣子 (Wood) - Muster，相关卡片：高桥沙里、中野蕾实
                    2012, // 高桥沙里 (Wood) - Muster，相关卡片：小田芽衣子、中野蕾实
                    2013, // 中野蕾实 (Wood) - Muster，相关卡片：小田芽衣子、高桥沙里
                    2018, // 泷川近夫 (Wood) - Bond，相关卡片：高久智惠理
                    2019, // 牧誓 (Wood) - TubaAlliance
                    // brass
                    2020, // 吉川优子 (Brass) - Hero卡，Morale技能
                    2021, // 加部友惠 (Brass) - Medic技能
                    2023, // 高坂丽奈 (Brass) - Hero卡，Attack技能
                    2027, // 岩田慧菜 (Brass) - Medic技能
                    2030, // 叶加濑满 (Brass) - Spy技能
                    2032, // 瞳拉拉 (Brass) - Spy技能
                    2034, // 后藤卓也 (Brass) - Bond，相关卡片：长濑梨子
                    2035, // 长濑梨子 (Brass) - Bond，相关卡片：后藤卓也
                    2036, // 加藤叶月 (Brass) - Horn
                    2040, // 黄前久美子 (Brass) - Hero卡，Medic技能
                    2041, // 久石奏 (Brass) - Hero卡，Spy技能
                    // percussion
                    2042, // 川岛绿辉 (Percussion) - Hero卡，TubaAlliance
                    2045, // 井上顺菜 (Percussion) - Bond，相关卡片：堺万纱子
                    2047, // 堺万纱子 (Percussion) - Bond，相关卡片：井上顺菜
                    // util
                    5001, // 大号君 - Decoy（工具卡）
                    5002, // 退部申请书 - Scorch（工具卡）
                    5003, // 日升祭
                    5004, // 第三乐章
                    5008, // 新山老师 - HornUtil（工具卡）
                    5013, // 大号君 - Decoy（工具卡）
                    5014, // 退部申请书 - Scorch（工具卡）
                    // leader
                    5012, // 泷千寻 - 指挥技能（领袖卡）
                },
            }
        },
        {
            CardGroup.KumikoThirdYear,
            new int[][] {
                new int[] {
                    // wood
                    3001, // 义井沙里 (Wood) - K5Leader
                    3004, // 吉田巧美 (Wood) -
                    3005, // 加千须实久 (Wood) - 
                    3006, // 河口纯夏 (Wood) - Muster，相关卡片：梶原支音
                    3007, // 梶原支音 (Wood) - Muster，相关卡片：河口纯夏
                    3011, // 针谷佳穗 (Wood) - Muster，相关卡片：上石弥生
                    3012, // 上石弥生 (Wood) - Muster，相关卡片：针谷佳穗
                    3013, // 釜屋雀 (Wood) - Bond，相关卡片：釜屋燕
                    3021, // 剑崎梨梨花 (Wood) - Tunning
                    3022, // 兜谷爱瑠 (Wood) - Bond，相关卡片：笼手山骏河
                    3023, // 笼手山骏河 (Wood) - Bond，相关卡片：兜谷爱瑠
                    3037, // 高久智惠理 (Wood) - Bond，相关卡片：泷川近夫
                    3040, // 松绮洋子 (Wood) - Spy技能
                    3045, // 泷川近夫 (Wood) - Bond，相关卡片：高久智惠理
                    // brass
                    3046, // 高坂丽奈 (Brass) - Hero卡，Pressure
                    3051, // 瞳拉拉 (Brass) - Spy技能
                    3053, // 黄前久美子 (Brass) - Hero卡，Medic技能
                    // percussion
                    3059, // 釜屋燕 (Percussion) - Bond，相关卡片：釜屋雀
                    // util
                    5002, // 退部申请书 - Scorch（工具卡）
                    5004, // 第三乐章
                    5005, // 忘带鼓槌
                    // leader
                    5012, // 泷千寻 - 指挥技能（领袖卡）
                }
            }
        }
    };
}
