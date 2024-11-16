using NUnit.Framework;
using System.Collections.Generic;

public class PlaySceneModelTest
{
    [Test]
    public void Demo()
    {
        List<int> selfInfoIdList = new List<int> {2001, 2002, 2003};
        List<int> enemyInfoIdList = new List<int> {2004, 2005, 2006};
        PlaySceneModel.Instance.SetAllCardInfoIdList(selfInfoIdList, enemyInfoIdList);
        List<CardModel> cardModelList = PlaySceneModel.Instance.GetSelfHandCards();
        Assert.AreEqual(selfInfoIdList.Count, cardModelList.Count);
        for (int i = 0; i < selfInfoIdList.Count; i++) {
            Assert.AreEqual(selfInfoIdList[i], cardModelList[i].GetCardInfo().infoId);
        }
    }
}
