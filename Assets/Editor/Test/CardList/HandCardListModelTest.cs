using NUnit.Framework;
using System.Collections.Immutable;

public class HandCardListModelTest
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
    public void AddCard_SetsLocationToHandAreaAndAdds()
    {
        // Arrange
        var model = new HandCardListModel();

        // Act
        var result = model.AddCard(cardA);

        // Assert
        Assert.IsInstanceOf<HandCardListModel>(result, "AddCard 应返回 HandCardListModel 实例");
        Assert.AreEqual(1, result.cardList.Count, "添加一张卡牌后，列表长度应为1");
        Assert.AreEqual(CardLocation.HandArea, result.cardList[0].cardLocation, "卡牌位置应被设置为 HandArea");
        Assert.AreEqual(CardSelectType.PlayCard, result.cardList[0].cardSelectType);
    }

    [Test]
    public void AddCardList_SetsAllLocationsToHandAreaAndAdds()
    {
        // Arrange
        var model = new HandCardListModel();
        var list = ImmutableList.CreateRange(new[] { cardA, cardB });

        // Act
        var result = model.AddCardList(list);

        // Assert
        Assert.IsInstanceOf<HandCardListModel>(result, "AddCardList 应返回 HandCardListModel 实例");
        Assert.AreEqual(2, result.cardList.Count, "添加多张卡牌后，列表长度应为2");
        // 验证所有卡牌位置都被重写为 HandArea
        foreach (var card in result.cardList) {
            Assert.AreEqual(CardLocation.HandArea, card.cardLocation);
            Assert.AreEqual(CardSelectType.PlayCard, card.cardSelectType);
        }
    }
}
