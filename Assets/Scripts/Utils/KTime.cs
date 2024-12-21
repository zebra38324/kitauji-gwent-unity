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

    // 获取当前毫秒时间，格式"yyyy-MM-dd HH:mm:ss.fff"
    public static string CurrentFormatMill()
    {
        return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
    }
}
