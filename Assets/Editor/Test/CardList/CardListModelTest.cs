using NUnit.Framework;
using System.Collections.Generic;
using System.Collections.Immutable;

public class CardListModelTest
{
    private CardModel sampleCardA = new CardModel(new CardInfo());
    private CardModel sampleCardB = new CardModel(new CardInfo());

    [Test]
    public void Constructor_InitializesEmptyList()
    {
        var model = new CardListModel();
        Assert.IsEmpty(model.cardList, "默认构造函数应创建空列表");
    }

    [Test]
    public void AddCard_AddsSingleCard()
    {
        var model = new CardListModel();
        var result = model.AddCard(sampleCardA);

        Assert.AreEqual(1, result.cardList.Count, "添加单张卡牌后，列表长度应为1");
        Assert.Contains(sampleCardA, result.cardList, "添加的卡牌应存在于列表中");
    }

    [Test]
    public void AddCardList_AddsMultipleCards()
    {
        var model = new CardListModel();
        var newCards = ImmutableList.Create(sampleCardA, sampleCardB);
        var result = model.AddCardList(newCards);

        Assert.AreEqual(2, result.cardList.Count, "添加卡牌列表后，列表长度应为2");
        CollectionAssert.AreEquivalent(newCards, result.cardList, "添加的卡牌列表应与结果一致");
    }

    [Test]
    public void RemoveCard_RemovesExistingCard()
    {
        var model = new CardListModel()
            .AddCard(sampleCardA)
            .AddCard(sampleCardB);

        var result = model.RemoveCard(sampleCardA, out var removedCard);

        Assert.AreEqual(CardLocation.None, removedCard.cardLocation, "移除卡牌后，location为none");
        Assert.AreEqual(1, result.cardList.Count, "移除卡牌后，列表长度应减1");
        Assert.IsFalse(result.cardList.Contains(sampleCardA), "已移除的卡牌不应再出现在列表中");
    }

    [Test]
    public void RemoveCard_NonexistentCard_ReturnsSameModel()
    {
        var model = new CardListModel().AddCard(sampleCardA);
        var result = model.RemoveCard(TestUtil.MakeCard(id: 2), out var removedCard);

        Assert.AreSame(model, result, "尝试移除不存在的卡牌时，应返回原始实例");
    }

    [Test]
    public void RemoveAllCard_ClearsListAndReturnsRemoved()
    {
        var model = new CardListModel()
            .AddCard(sampleCardA)
            .AddCard(sampleCardB);

        var clearedModel = model.RemoveAllCard(out var removedList);

        Assert.IsEmpty(clearedModel.cardList, "RemoveAllCard 后，列表应为空");
        CollectionAssert.AreEquivalent(
            new List<CardModel>(new [] { sampleCardA, sampleCardB }),
            removedList,
            "out 参数应返回原始的卡牌列表"
        );
        foreach (var card in removedList) {
            Assert.AreEqual(CardLocation.None, card.cardLocation);
        }
    }
}
