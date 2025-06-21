using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using System.Linq;
using UnityEngine.TestTools;
using System.Collections;

public class AIMockTest
{
    private IEnumerator AIMock()
    {
        long startTs = KTime.CurrentMill();
        var mockBattle = new MockBattle(new MockBattle.Side { name = "A" }, new MockBattle.Side { name = "B" });
        mockBattle.Start();
        while (!mockBattle.IsFinish()) {
            if (KTime.CurrentMill() - startTs > 20000) {
                KLog.E("AIMockTest", "Timeout!!!");
                yield break;
            }
            yield return null;
        }
    }

    [UnityTest]
    public IEnumerator MockTest()
    {
        //KLog.redirectToFile = true;
        //for (int i = 0; i < 100; i++) {
        //    yield return AIMock();
        //}
        //KLog.redirectToFile = false;
        yield break;
    }
}
