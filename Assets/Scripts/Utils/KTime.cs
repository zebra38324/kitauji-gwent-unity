using System;

// kitauji time
public class KTime
{
    // 获取当前毫秒时间戳
    public static long CurrentMill()
    {
        long ts = ((DateTimeOffset)(DateTime.Now)).ToUnixTimeMilliseconds();
        return ts;
    }
}
