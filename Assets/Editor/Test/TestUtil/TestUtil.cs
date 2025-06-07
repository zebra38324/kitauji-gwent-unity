public class TestUtil
{
    public static CardModel MakeCard(CardAbility ability = CardAbility.None,
        int originPower = 0,
        CardType cardType = CardType.Normal,
        string chineseName = "TestName",
        int id = 0,
        string musterType = "",
        string bondType = "",
        CardBadgeType badgeType = CardBadgeType.Wood,
        bool isMale = false,
        int grade = 1)
    {
        var cardInfo = new CardInfo {
            ability = ability,
            originPower = originPower,
            cardType = cardType,
            chineseName = chineseName,
            id = id,
            musterType = musterType,
            bondType = bondType,
            badgeType = badgeType,
            isMale = isMale,
            grade = grade,
        };
        return new CardModel(cardInfo);
    }
}
