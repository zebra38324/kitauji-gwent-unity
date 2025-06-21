using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using System.Linq;
using UnityEngine.TestTools;
using System.Collections;

public class KRandomTest
{
    [Test]
    public void Next()
    {
        int seed = 10;
        var kRandom1 = new KRandom(seed);
        var kRandom2 = new KRandom(seed);
        for (int i = 0; i < 100; i++) {
            kRandom1 = kRandom1.Next(i * 10, i * 20, out var nextVal1);
            kRandom2 = kRandom2.Next(i * 10, i * 20, out var nextVal2);
            Assert.AreEqual(nextVal1, nextVal2);
        }
    }
}
