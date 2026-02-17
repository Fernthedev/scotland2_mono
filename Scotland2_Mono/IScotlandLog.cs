namespace Scotland2_Mono;

public interface IScotlandLog
{
    void Info(string message);
    void Warn(string message);
    void Error(string message);
    void Debug(string message);
}

public class StaticLog
{
    public static void Initialize(IScotlandLog logger)
    {
        ScotlandLog = logger;
    }
    

    public static IScotlandLog ScotlandLog { get; private set; } = null!;
}