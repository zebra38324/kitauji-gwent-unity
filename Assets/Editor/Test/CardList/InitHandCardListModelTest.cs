using NUnit.Framework;
using System.Collections.Immutable;

public class InitHandCardListModelTest
{
    private CardModel cardA = new CardModel(new CardInfo());
    private CardModel cardB = new CardModel(new CardInfo());
    private CardModel cardC = new CardModel(new CardInfo());

    [SetUp]
    public void SetUp()
    {
        cardA = cardA with {
            cardLocation = CardLocation.None,
            isSelected = false
        };
        cardB = cardB with {
            cardLocation = CardLocation.None,
            isSelected = false
        };
        cardC = cardC with {
            cardLocation = CardLocation.None,
            isSelected = false
        };
    }

    [Test]
    public void Constructor_InitializesEmptyLists()
    {
        var model = new InitHandCardListModel();

        // 初始时，基类的 cardList 应为空，selectedCardList 也应为空
        Assert.IsEmpty(model.cardList, "默认构造时，cardList 应为空");
        Assert.IsEmpty(model.selectedCardList, "默认构造时，selectedCardList 应为空");
    }

    [Test]
    public void AddCard_SetsLocationToInitHandArea_AndReturnsDerivedType()
    {
        var model = new InitHandCardListModel();

        // Act
        var result = model.AddCard(cardA);

        // Assert
        Assert.IsInstanceOf<InitHandCardListModel>(result,
            "AddCard 应返回 InitHandCardListModel 实例");
        var derived = (InitHandCardListModel)result;
        Assert.AreEqual(1, derived.cardList.Count,
            "添加一张卡牌后，cardList 长度应为 1");
        Assert.AreEqual(CardLocation.InitHandArea, derived.cardList[0].cardLocation,
            "添加的卡牌位置应被设置为 InitHandArea");
        Assert.AreEqual(CardSelectType.ReDrawHandCard, derived.cardList[0].cardSelectType);
        Assert.IsEmpty(derived.selectedCardList,
            "AddCard 不应该影响 selectedCardList");
    }

    [Test]
    public void AddCardList_SetsAllLocationsToInitHandArea()
    {
        var model = new InitHandCardListModel();
        var list = ImmutableList.Create(cardA, cardB, cardC);

        var result = model.AddCardList(list) as InitHandCardListModel;

        Assert.IsNotNull(result, "AddCardList 应返回 InitHandCardListModel");
        Assert.AreEqual(3, result.cardList.Count,
            "添加三张卡牌后，cardList 长度应为 3");
        // 检查所有元素的位置
        foreach (var card in result.cardList) {
            Assert.AreEqual(CardLocation.InitHandArea, card.cardLocation,
                $"卡牌 {card.cardInfo.chineseName} 的位置应为 InitHandArea");
            Assert.AreEqual(CardSelectType.ReDrawHandCard, card.cardSelectType);
        }
        Assert.IsEmpty(result.selectedCardList,
            "AddCardList 不应改变 selectedCardList");
    }

    [Test]
    public void SelectCard_FirstTime_SelectsAndMarksIsSelected()
    {
        // 先添加两张卡
        var model = new InitHandCardListModel()
            .AddCard(cardA) as InitHandCardListModel;
        Assert.IsNotNull(model);

        // Act: 选中 cardA
        var afterSelect = model.SelectCard(model.cardList[0]);

        // Assert
        Assert.IsInstanceOf<InitHandCardListModel>(afterSelect);
        var derived = (InitHandCardListModel)afterSelect;
        Assert.AreEqual(1, derived.selectedCardList.Count,
            "第一次 SelectCard 后，selectedCardList 应包含 1 张卡");
        Assert.IsTrue(derived.selectedCardList.Contains(
            derived.cardList[0]),
            "selectedCardList 中应包含被选中的卡牌实例");
        Assert.IsTrue(derived.cardList[0].isSelected,
            "cardList 中对应的卡牌 isSelected 应为 true");
    }

    [Test]
    public void SelectCard_SecondTime_DeselectsAndRemovesFromSelectedList()
    {
        // 添加并选中 cardA
        var model = new InitHandCardListModel()
            .AddCard(cardA) as InitHandCardListModel;
        model = model.SelectCard(model.cardList[0]);
        Assert.IsNotNull(model);
        Assert.AreEqual(1, model.selectedCardList.Count);

        // Act: 再次对同一张卡调用 SelectCard
        var afterDeselect = model.SelectCard(model.cardList[0]);

        // Assert
        var derived = (InitHandCardListModel)afterDeselect;
        Assert.IsEmpty(derived.selectedCardList,
            "第二次 SelectCard 后，selectedCardList 应为空");
        Assert.IsFalse(derived.cardList[0].isSelected,
            "cardList 中对应的卡牌 isSelected 应为 false");
    }

    [Test]
    public void SelectCard_ExceedMaxSelection_DoesNotChangeSelection()
    {
        // 添加三张卡
        var baseModel = new InitHandCardListModel()
            .AddCard(cardA) as InitHandCardListModel;
        baseModel = baseModel.AddCard(cardB) as InitHandCardListModel;
        baseModel = baseModel.AddCard(cardC) as InitHandCardListModel;
        Assert.AreEqual(3, baseModel.cardList.Count);

        // 先选中两张（达到最大可重抽数 RE_DRAW_HAND_CARD_NUM_MAX = 2）
        var afterTwo = baseModel
            .SelectCard(baseModel.cardList[0]) as InitHandCardListModel;
        afterTwo = afterTwo.SelectCard(afterTwo.cardList[1]) as InitHandCardListModel;
        Assert.AreEqual(2, afterTwo.selectedCardList.Count);

        // Act: 再次尝试选中第三张
        var afterThree = afterTwo.SelectCard(afterTwo.cardList[2]) as InitHandCardListModel;

        // Assert: 选中数不变，第三张既不在 selectedList，也不被标记
        Assert.AreEqual(2, afterThree.selectedCardList.Count,
            "超过最大可选数后，selectedCardList 长度不应增加");
        Assert.IsFalse(afterThree.selectedCardList.Contains(afterThree.cardList[2]),
            "第三张卡不应被加入 selectedCardList");
        Assert.IsFalse(afterThree.cardList[2].isSelected,
            "第三张卡的 isSelected 应仍为 false");
    }
}
