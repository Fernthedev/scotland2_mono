namespace Scotland2_Mono;

public interface ILog
{
    void Info(string message);
    void Warn(string message);
    void Error(string message);
    void Debug(string message);
}

public class StaticLog
{
    public static void Initialize(ILog logger)
    {
        Log = logger;
    }
    

    public static ILog Log { get; private set; } = null!;
}