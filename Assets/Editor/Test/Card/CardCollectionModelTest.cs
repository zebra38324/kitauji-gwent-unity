using NUnit.Framework;

public class CardCollectionModelTest
{
    [Test]
    public void Demo()
    {
        CardCollectionModel cardCollectionModel = new CardCollectionModel();
        for (int i = 0; i < 10; i++) {
            CardInfo cardInfo = new CardInfo();
            cardInfo.id = i;
            cardCollectionModel.AddCard(cardInfo);
        }
        cardCollectionModel.RemoveCard(3);
        cardCollectionModel.RemoveCard(10); // out of range
    }
}
