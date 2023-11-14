using UnityEngine;

// 统一管理全局通知行为
public class PlaySceneManager
{
    public enum PlaySceneMsg
    {
        MedicSelectDiscardCard = 0, // 复活技能，已点击选取弃用卡牌
        PlayCardFromHandArea, // 从手牌区打出牌
    }

    private static readonly PlaySceneManager instance = new PlaySceneManager();

    private GameObject discardArea;

    private GameObject handArea;

    private GameObject selfPlayArea;

    static PlaySceneManager() {}

    public static PlaySceneManager Instance
    {
        get
        {
            return instance;
        }
    }

    public void HandleMessage(PlaySceneMsg msg, params object[] list)
    {
        switch (msg)
        {
            case PlaySceneMsg.MedicSelectDiscardCard: {
                if (discardArea == null) {
                    discardArea = GameObject.Find("DiscardArea");
                }
                GameObject card = (GameObject)list[0];
                discardArea.GetComponent<DiscardArea>().RemoveCard(card);
                discardArea.GetComponent<DiscardArea>().CloseArea();
                SelfDiscardCardManager.Instance.RemoveCard(card);

                bool isSelf = (bool)list[1]; // TODO: 间谍牌
                AddCardToPlayArea(card, isSelf);
                break;
            }
            case PlaySceneMsg.PlayCardFromHandArea: {
                if (handArea == null) {
                    handArea = GameObject.Find("HandArea");
                }
                GameObject card = (GameObject)list[0];
                handArea.GetComponent<HandArea>().RemoveCard(card);

                bool isSelf = (bool)list[1]; // TODO: 间谍牌
                AddCardToPlayArea(card, isSelf);
                break;
            }
        }
    }

    private void AddCardToPlayArea(GameObject card, bool isSelf)
    {
        if (selfPlayArea == null) {
            selfPlayArea = GameObject.Find("SelfPlayArea");
        }
        if (isSelf) {
            selfPlayArea.GetComponent<SinglePlayerArea>().AddNormalCard(card);
        }
    }
}
