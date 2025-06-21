using System.Collections;
using System.Collections.Generic;

public record KRandom
{
    private int seed { get; init; } = 0;

    public KRandom(int seed_)
    {
        seed = seed_;
    }

    public KRandom Next(int minVal, int maxVal, out int nextVal)
    {
        var newRecord = this;
        var ran = new System.Random(newRecord.seed);
        nextVal = ran.Next(minVal, maxVal);
        newRecord = newRecord with {
            seed = ran.Next()
        };
        return newRecord;
    }
}
