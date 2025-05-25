using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using System.Linq;

public class PlayTrackerTest
{
    [Test]
    public void Constructor_InitializesFieldsCorrectly()
    {
        var tracker = new PlayTracker(true, "Self", "Enemy", CardGroup.KumikoSecondYear);
        Assert.IsTrue(tracker.isHost);
        Assert.AreEqual("Self", tracker.selfPlayerInfo.name);
        Assert.AreEqual(CardGroup.KumikoSecondYear, tracker.selfPlayerInfo.cardGroup);
        Assert.AreEqual("Enemy", tracker.enemyPlayerInfo.name);
        Assert.AreEqual(0, tracker.selfPlayerInfo.setScore);
        Assert.AreEqual(0, tracker.enemyPlayerInfo.setScore);
        Assert.IsEmpty(tracker.setRecordList);
        Assert.AreEqual(0, tracker.curSet);
    }

    [Test]
    public void ConfigFirstSet_OnFirstSet_AddsRecord()
    {
        var tracker = new PlayTracker(false, "S", "E", CardGroup.KumikoFirstYear);
        var updated = tracker.ConfigFirstSet(true);
        Assert.AreEqual(1, updated.setRecordList.Count);
        Assert.IsTrue(updated.setRecordList[0].selfFirst);
        // curSet should remain at 0
        Assert.AreEqual(0, updated.curSet);
    }

    [Test]
    public void SetFinish_SelfWin_UpdatesScoresAndAdvancesSet()
    {
        var tracker = new PlayTracker(false, "S", "E", CardGroup.KumikoFirstYear)
            .ConfigFirstSet(true);
        var after = tracker.SetFinish(5, 3);
        Assert.AreEqual(1, after.selfPlayerInfo.setScore);
        Assert.AreEqual(0, after.enemyPlayerInfo.setScore);
        Assert.AreEqual(1, after.curSet);
        Assert.AreEqual(2, after.setRecordList.Count);
        Assert.AreEqual(1, after.setRecordList[0].result);
        Assert.AreEqual(true, after.setRecordList[0].selfFirst);
    }

    [Test]
    public void SetFinish_EnemyWin_UpdatesScoresAndAdvancesSet()
    {
        var tracker = new PlayTracker(false, "S", "E", CardGroup.KumikoFirstYear)
            .ConfigFirstSet(false);
        var after = tracker.SetFinish(2, 4);
        Assert.AreEqual(0, after.selfPlayerInfo.setScore);
        Assert.AreEqual(1, after.enemyPlayerInfo.setScore);
        Assert.AreEqual(1, after.curSet);
        Assert.AreEqual(-1, after.setRecordList[0].result);
        Assert.AreEqual(false, after.setRecordList[0].selfFirst);
    }

    [Test]
    public void SetFinish_Draw_SecondYearRules_AppliesWinner()
    {
        var tracker = new PlayTracker(false, "S", "E", CardGroup.KumikoSecondYear)
            .ConfigFirstSet(true);
        var after = tracker.SetFinish(3, 3);
        // self has SecondYear and enemy not, so treated as win
        Assert.AreEqual(1, after.selfPlayerInfo.setScore);
        Assert.AreEqual(0, after.enemyPlayerInfo.setScore);
        Assert.AreEqual(1, after.curSet);
        Assert.AreEqual(1, after.setRecordList[0].result);
        Assert.AreEqual(true, after.setRecordList[0].selfFirst);
    }

    [Test]
    public void IsGameFinish_BeforeTwoWins_NotFinished()
    {
        var t = new PlayTracker(false, "S", "E", CardGroup.KumikoFirstYear)
            .ConfigFirstSet(true)
            .SetFinish(5, 5); // draw: both +1 => 1:1
        Assert.IsFalse(t.isGameFinish);
    }

    [Test]
    public void IsGameFinish_AfterTwoWins_Finished()
    {
        var t = new PlayTracker(false, "S", "E", CardGroup.KumikoFirstYear)
            .ConfigFirstSet(true)
            .SetFinish(5, 2)   // 1:0
            .SetFinish(5, 1);  // 2:0 => finish
        Assert.IsTrue(t.isGameFinish);
    }

    [Test]
    public void GetNameText_FormatsCorrectly()
    {
        var t = new PlayTracker(false, "SName", "EName", CardGroup.KumikoFirstYear);
        Assert.AreEqual("<color=green>SName</color>", t.GetNameText(true));
        Assert.AreEqual("<color=red>EName</color>", t.GetNameText(false));
    }

    [Test]
    public void GetAbilityNameText_FormatsCorrectly()
    {
        var text = PlayTracker.GetAbilityNameText(CardAbility.Spy);
        Assert.AreEqual("<b>间谍</b>", text);
    }

    [Test]
    public void GetCardNameText_FormatsCorrectly()
    {
        var card = TestUtil.MakeCard(chineseName: "test");
        var text = PlayTracker.GetCardNameText(card);
        Assert.AreEqual("<b>test</b>", text);
    }

    // 以下测试后缀：W胜，L负，D平
    [Test]
    public void IsSelfWinner_WW()
    {
        var t = new PlayTracker(false, "S", "E", CardGroup.KumikoFirstYear)
            .ConfigFirstSet(true)
            .SetFinish(5, 0)
            .SetFinish(5, 0);
        Assert.IsTrue(t.isSelfWinner);
        Assert.AreEqual(2, t.selfPlayerInfo.setScore);
        Assert.AreEqual(0, t.enemyPlayerInfo.setScore);
    }

    [Test]
    public void IsSelfWinner_WD()
    {
        var t = new PlayTracker(false, "S", "E", CardGroup.KumikoFirstYear)
            .ConfigFirstSet(true)
            .SetFinish(5, 0)
            .SetFinish(5, 5);
        Assert.IsTrue(t.isSelfWinner);
        Assert.AreEqual(2, t.selfPlayerInfo.setScore);
        Assert.AreEqual(1, t.enemyPlayerInfo.setScore);
    }

    [Test]
    public void IsSelfWinner_WLW()
    {
        var t = new PlayTracker(false, "S", "E", CardGroup.KumikoFirstYear)
            .ConfigFirstSet(true)
            .SetFinish(5, 0)
            .SetFinish(5, 10)
            .SetFinish(5, 0);
        Assert.IsTrue(t.isSelfWinner);
        Assert.AreEqual(2, t.selfPlayerInfo.setScore);
        Assert.AreEqual(1, t.enemyPlayerInfo.setScore);
    }

    [Test]
    public void IsSelfWinner_WLD()
    {
        var t = new PlayTracker(false, "S", "E", CardGroup.KumikoFirstYear)
            .ConfigFirstSet(true)
            .SetFinish(5, 0)
            .SetFinish(5, 10)
            .SetFinish(5, 5);
        Assert.IsTrue(t.isSelfWinner);
        Assert.AreEqual(2, t.selfPlayerInfo.setScore);
        Assert.AreEqual(2, t.enemyPlayerInfo.setScore);
    }

    [Test]
    public void IsSelfWinner_WLL()
    {
        var t = new PlayTracker(false, "S", "E", CardGroup.KumikoFirstYear)
            .ConfigFirstSet(true)
            .SetFinish(5, 0)
            .SetFinish(5, 10)
            .SetFinish(5, 10);
        Assert.IsFalse(t.isSelfWinner);
        Assert.AreEqual(1, t.selfPlayerInfo.setScore);
        Assert.AreEqual(2, t.enemyPlayerInfo.setScore);
    }

    [Test]
    public void IsSelfWinner_DW()
    {
        var t = new PlayTracker(false, "S", "E", CardGroup.KumikoFirstYear)
            .ConfigFirstSet(true)
            .SetFinish(5, 5)
            .SetFinish(5, 0);
        Assert.IsTrue(t.isSelfWinner);
        Assert.AreEqual(2, t.selfPlayerInfo.setScore);
        Assert.AreEqual(1, t.enemyPlayerInfo.setScore);
    }

    [Test]
    public void IsSelfWinner_DDW()
    {
        var t = new PlayTracker(false, "S", "E", CardGroup.KumikoFirstYear)
            .ConfigFirstSet(true)
            .SetFinish(5, 5)
            .SetFinish(5, 5)
            .SetFinish(5, 0);
        Assert.IsTrue(t.isSelfWinner);
        Assert.AreEqual(3, t.selfPlayerInfo.setScore);
        Assert.AreEqual(2, t.enemyPlayerInfo.setScore);
    }

    [Test]
    public void IsSelfWinner_DDD()
    {
        var t = new PlayTracker(false, "S", "E", CardGroup.KumikoFirstYear)
            .ConfigFirstSet(true)  // Set1 selfFirst
            .SetFinish(3, 3)        // draw -> next first = false
            .SetFinish(3, 3)        // draw -> next first = true
            .SetFinish(3, 3);       // after third
        // scores equal (3 draws => 3:3 -> 3 points each), first count = 2-> self wins
        Assert.IsTrue(t.isSelfWinner);
        Assert.AreEqual(3, t.selfPlayerInfo.setScore);
        Assert.AreEqual(3, t.enemyPlayerInfo.setScore);
    }

    [Test]
    public void IsSelfWinner_DDL()
    {
        var t = new PlayTracker(false, "S", "E", CardGroup.KumikoFirstYear)
            .ConfigFirstSet(true)
            .SetFinish(5, 5)
            .SetFinish(5, 5)
            .SetFinish(5, 10);
        Assert.IsFalse(t.isSelfWinner);
        Assert.AreEqual(2, t.selfPlayerInfo.setScore);
        Assert.AreEqual(3, t.enemyPlayerInfo.setScore);
    }

    [Test]
    public void IsSelfWinner_DL()
    {
        var t = new PlayTracker(false, "S", "E", CardGroup.KumikoFirstYear)
            .ConfigFirstSet(true)
            .SetFinish(5, 5)
            .SetFinish(5, 10);
        Assert.IsFalse(t.isSelfWinner);
        Assert.AreEqual(1, t.selfPlayerInfo.setScore);
        Assert.AreEqual(2, t.enemyPlayerInfo.setScore);
    }

    [Test]
    public void IsSelfWinner_LWW()
    {
        var t = new PlayTracker(false, "S", "E", CardGroup.KumikoFirstYear)
            .ConfigFirstSet(true)
            .SetFinish(5, 10)
            .SetFinish(5, 0)
            .SetFinish(5, 0);
        Assert.IsTrue(t.isSelfWinner);
        Assert.AreEqual(2, t.selfPlayerInfo.setScore);
        Assert.AreEqual(1, t.enemyPlayerInfo.setScore);
    }

    [Test]
    public void IsSelfWinner_LWD()
    {
        var t = new PlayTracker(false, "S", "E", CardGroup.KumikoFirstYear)
            .ConfigFirstSet(true)
            .SetFinish(5, 10)
            .SetFinish(5, 0)
            .SetFinish(5, 5);
        Assert.IsTrue(t.isSelfWinner);
        Assert.AreEqual(2, t.selfPlayerInfo.setScore);
        Assert.AreEqual(2, t.enemyPlayerInfo.setScore);
    }

    [Test]
    public void IsSelfWinner_LWL()
    {
        var t = new PlayTracker(false, "S", "E", CardGroup.KumikoFirstYear)
            .ConfigFirstSet(true)
            .SetFinish(5, 10)
            .SetFinish(5, 0)
            .SetFinish(5, 10);
        Assert.IsFalse(t.isSelfWinner);
        Assert.AreEqual(1, t.selfPlayerInfo.setScore);
        Assert.AreEqual(2, t.enemyPlayerInfo.setScore);
    }

    [Test]
    public void IsSelfWinner_LD()
    {
        var t = new PlayTracker(false, "S", "E", CardGroup.KumikoFirstYear)
            .ConfigFirstSet(true)
            .SetFinish(5, 10)
            .SetFinish(5, 5);
        Assert.IsFalse(t.isSelfWinner);
        Assert.AreEqual(1, t.selfPlayerInfo.setScore);
        Assert.AreEqual(2, t.enemyPlayerInfo.setScore);
    }

    [Test]
    public void IsSelfWinner_LL()
    {
        var t = new PlayTracker(false, "S", "E", CardGroup.KumikoFirstYear)
            .ConfigFirstSet(true)
            .SetFinish(5, 10)
            .SetFinish(5, 10);
        Assert.IsFalse(t.isSelfWinner);
        Assert.AreEqual(0, t.selfPlayerInfo.setScore);
        Assert.AreEqual(2, t.enemyPlayerInfo.setScore);
    }
}
