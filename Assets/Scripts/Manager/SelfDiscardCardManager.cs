// 卡牌管理器，保存已弃用的卡牌
public class SelfDiscardCardManager : DiscardCardManager
{
    private static readonly SelfDiscardCardManager instance = new SelfDiscardCardManager();

    static SelfDiscardCardManager() {}

    public static SelfDiscardCardManager Instance
    {
        get
        {
            return instance;
        }
    }
}
