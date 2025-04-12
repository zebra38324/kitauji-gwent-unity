using System;

// AI逻辑模块统一接口
class AIModelInterface
{
    protected PlaySceneModel playSceneModel;

    // 初始化并设置牌组
    public AIModelInterface(PlaySceneModel playSceneModelParam)
    {
        playSceneModel = playSceneModelParam;
    }

    // 初始化手牌，并完成重抽手牌操作
    public virtual void DoInitHandCard()
    {

    }

    // WAIT_SELF_ACTION时的操作
    public virtual void DoPlayAction()
    {

    }

    // 计算退部技能得到的收益
    protected int GetScorchReturn()
    {
        int selfMaxPower = 0;
        int enemyMaxPower = 0;
        int maxPower = 0;
        playSceneModel.selfSinglePlayerAreaModel.CountBattleAreaCard((CardModel targetCard) => {
            if (targetCard.cardInfo.cardType == CardType.Normal && targetCard.currentPower > selfMaxPower) {
                selfMaxPower = targetCard.currentPower;
            }
            return true;
        });
        playSceneModel.enemySinglePlayerAreaModel.CountBattleAreaCard((CardModel targetCard) => {
            if (targetCard.cardInfo.cardType == CardType.Normal && targetCard.currentPower > enemyMaxPower) {
                enemyMaxPower = targetCard.currentPower;
            }
            return true;
        });
        maxPower = Math.Max(selfMaxPower, enemyMaxPower);

        int selfPowerDiff = 0;
        int enemyPowerDiff = 0;
        playSceneModel.selfSinglePlayerAreaModel.CountBattleAreaCard((CardModel targetCard) => {
            if (targetCard.cardInfo.cardType == CardType.Normal && targetCard.currentPower == maxPower) {
                selfPowerDiff += -targetCard.currentPower;
            }
            return true;
        });
        playSceneModel.enemySinglePlayerAreaModel.CountBattleAreaCard((CardModel targetCard) => {
            if (targetCard.cardInfo.cardType == CardType.Normal && targetCard.currentPower == maxPower) {
                enemyPowerDiff += -targetCard.currentPower;
            }
            return true;
        });
        return selfPowerDiff - enemyPowerDiff;
    }

    // 兜底逻辑，打出点数最高的角色牌。成功返回true，没打就返回false
    protected bool TryPlayMaxRole()
    {
        if (playSceneModel.selfSinglePlayerAreaModel.handRowAreaModel.cardList.Count == 0) {
            return false;
        }
        int selfMaxPower = -1;
        Func<CardModel, bool> canPlayCard = (card) => {
            if ((card.cardInfo.cardType == CardType.Normal || card.cardInfo.cardType == CardType.Hero) &&
                (card.cardInfo.ability != CardAbility.Scorch || GetScorchReturn() + card.cardInfo.originPower > 0)) {
                // 退部角色牌要小心，避免自损
                return true;
            }
            return false;
        };
        foreach (CardModel card in playSceneModel.selfSinglePlayerAreaModel.handRowAreaModel.cardList) {
            if (canPlayCard(card) && card.cardInfo.originPower > selfMaxPower) {
                selfMaxPower = card.cardInfo.originPower;
            }
        }
        if (selfMaxPower == -1) {
            return false;
        }
        foreach (CardModel card in playSceneModel.selfSinglePlayerAreaModel.handRowAreaModel.cardList) {
            if (canPlayCard(card) && card.cardInfo.originPower == selfMaxPower) {
                playSceneModel.ChooseCard(card);
                return true;
            }
        }
        return false;
    }
}
