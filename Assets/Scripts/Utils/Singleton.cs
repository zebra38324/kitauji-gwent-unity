// 通用单例类
public class Singleton<T> where T : Singleton<T>, new()
{
    private static T instance = null;

    public static T GetInstance()
    {
        if (instance == null)
        {
            instance = new T();
        }
        return instance;
    }
}