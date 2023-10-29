// 卡牌管理器，保存已弃用的卡牌
public class EnemyDiscardCardManager : DiscardCardManager
{
    private static readonly EnemyDiscardCardManager instance = new EnemyDiscardCardManager();

    static EnemyDiscardCardManager() {}

    public static EnemyDiscardCardManager Instance
    {
        get
        {
            return instance;
        }
    }
}
