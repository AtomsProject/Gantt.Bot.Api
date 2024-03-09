namespace Gantt.Bot.Scheduler.Logger;

/// <summary>
/// Provides a factory for building a logger used for debugging.
/// </summary>
/// <remarks>
/// This logger is used to write out large amounts of data to a file or console for debugging purposes.
/// It writes data out to a single file, and intended to be used with a text editor that can to hot reload of file.
/// It will also have issues with concurrent access, so it is not intended to be used in an environment taking concurrent requests.
/// </remarks>
public sealed class DebugLoggerFactor
{
    //private readonly string _sessionName = DateTime.Now.ToString("O").Replace(":", "-");

    public IDebugWriter OpenLog(string name)
    {
#if DEBUG
        var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", $"{name}.log");
        Console.WriteLine("Opening log file: " + path);

        var directory = Path.GetDirectoryName(path);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        return StreamDebugWriter.OpenFile(path);
#else
            return NullLogger.Instance;
#endif
    }

    private sealed class NullLogger : IDebugWriter
    {
        public static NullLogger Instance = new NullLogger();

        public void Dispose()
        {
        }

        public void WriteLine()
        {
        }

        public void WriteLine(string message)
        {
        }

        public void Write(string message)
        {
        }

        public void Write(byte value)
        {
        }

        public void WriteLine(Exception message)
        {
        }

        public void WriteLine(char message)
        {
        }

        public void WriteLine<T>(T message)
        {
        }

        public void Write(char message)
        {
        }
    }
}

public interface IDebugWriter : IDisposable
{
    void WriteLine();
    void WriteLine(string message);
    void Write(string message);
    void Write(byte value);
    void WriteLine(Exception message);
    void WriteLine(char message);
    void WriteLine<T>(T message);
    void Write(char message);
}