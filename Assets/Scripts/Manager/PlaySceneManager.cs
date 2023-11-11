using UnityEngine;

// 统一管理全局通知行为
public class PlaySceneManager
{
    public enum PlaySceneMsg
    {
        MedicSelectDiscardCard = 0, // 复活技能，已点击选取弃用卡牌
    }

    private static readonly PlaySceneManager instance = new PlaySceneManager();

    private GameObject discardArea;

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
                break;
            }
        }
    }
}
