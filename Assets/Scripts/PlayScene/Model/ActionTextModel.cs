using System.Collections.Generic;

/**
 * 记录游戏中玩家操作，用于ui展示
 */
public class ActionTextModel
{
    public string totalText { get; private set; }

    // 通过toast展示的文本
    public string toastText { get; set; }

    private string selfName;

    private string enemyName;

    public ActionTextModel(string selfName_, string enemyName_)
    {
        totalText = "";
        toastText = null;
        selfName = selfName_;
        enemyName = enemyName_;
    }

    // 完成初始手牌抽取
    public void FinishInitHandCard(bool isSelf)
    {
        string name = GetName(isSelf);
        totalText += string.Format("{0} 完成了初始手牌抽取\n", name);
    }

    public void ChooseCard(bool isSelf, CardModel card, CardSelectType selectType)
    {
        string name = GetName(isSelf);
        string cardName = string.Format("<b>{0}</b>", card.cardInfo.chineseName);
        switch (selectType) {
            case CardSelectType.PlayCard: {
                totalText += string.Format("{0} 打出卡牌：{1}\n", name, cardName);
                break;
            }
            case CardSelectType.WithstandAttack: {
                totalText += string.Format("{0} 攻击卡牌：{1}\n", name, cardName);
                break;
            }
            case CardSelectType.DecoyWithdraw: {
                totalText += string.Format("{0} 撤回卡牌：{1}\n", name, cardName);
                break;
            }
        }
    }

    public void InterruptAction(bool isSelf)
    {
        string name = GetName(isSelf);
        totalText += string.Format("{0} 未选择目标\n", name);
    }

    public void Pass(bool isSelf)
    {
        string name = GetName(isSelf);
        totalText += string.Format("{0} 放弃跟牌\n", name);
    }

    // 1: self胜
    // 0: 平局
    // -1: self负
    public void SetFinish(int result)
    {
        if (result > 0) {
            totalText += string.Format("本局结果：{0} 胜利\n", GetName(true));
        } else if (result < 0) {
            totalText += string.Format("本局结果：{0} 胜利\n", GetName(false));
        } else {
            totalText += string.Format("本局结果：双方平局\n");
        }
    }

    public void SetStart(bool isSelfFirst)
    {
        string name = GetName(isSelfFirst);
        totalText += string.Format("新一局开始，{0} 先手\n", name);
    }

    public void ApplyScorch(List<CardModel> cardList)
    {
        if (cardList.Count == 0) {
            return;
        }
        string cardName = "";
        foreach (CardModel card in cardList) {
            cardName += string.Format("<b>{0}</b>、", card.cardInfo.chineseName);
        }
        cardName = cardName.Substring(0, cardName.Length - 1);
        totalText += string.Format("移除卡牌：{0}\n", cardName);
    }

    public void EnemyExit()
    {
        string name = GetName(false);
        totalText += string.Format("{0} 退出房间\n", name);
    }

    // 返回带颜色的玩家名
    private string GetName(bool isSelf)
    {
        string name = isSelf ? selfName : enemyName;
        string color = isSelf ? "green" : "red";
        return string.Format("<color={0}>{1}</color>", color, name);
    }
}
