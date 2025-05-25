using NUnit.Framework;
using System.Collections.Immutable;

public class SingleCardListModelTest
{
    private CardModel cardA = new CardModel(new CardInfo());
    private CardModel cardB = new CardModel(new CardInfo());

    [SetUp]
    public void SetUp()
    {
        cardA = cardA with { cardLocation = CardLocation.BattleArea };
        cardB = cardB with { cardLocation = CardLocation.DiscardArea };
    }

    [Test]
    public void Constructor_InitializesEmptyList()
    {
        var model = new SingleCardListModel(CardLocation.None);
        Assert.IsEmpty(model.cardList, "默认构造应生成空列表");
    }

    [Test]
    public void AddCard_WhenEmpty_AddsCard()
    {
        var model = new SingleCardListModel(CardLocation.None);
        var result = model.AddCard(cardA) as SingleCardListModel;
        Assert.IsNotNull(result, "AddCard 应返回 SingleCardListModel 实例");
        Assert.AreEqual(1, result.cardList.Count, "添加一张卡牌后，列表长度应为1");
        Assert.Contains(cardA, result.cardList, "列表中应包含添加的卡牌");
    }

    [Test]
    public void AddCard_WhenNotEmpty()
    {
        var model = new SingleCardListModel(CardLocation.None)
                        .AddCard(cardA) as SingleCardListModel;
        Assert.IsNotNull(model);
        var result = model.AddCard(cardB);
        Assert.AreEqual(1, result.cardList.Count, "列表长度保持为1");
        Assert.AreEqual(cardB, result.cardList[0]);
    }
}
