namespace Gantt.Bot.Scheduler.Logger;

public sealed class StreamDebugWriter(StreamWriter streamWriter) : IDebugWriter
{
    public static IDebugWriter Console => new StreamDebugWriter(new StreamWriter(System.Console.OpenStandardOutput()));
    public static IDebugWriter OpenFile(string path) => new StreamDebugWriter(new StreamWriter(path, false));

    public void WriteLine()
    {
        streamWriter.WriteLine();
    }

    public void WriteLine(string message)
    {
        streamWriter.WriteLine(message);
    }

    public void Write(string message)
    {
        streamWriter.Write(message);
    }

    public void Write(byte value)
    {
        streamWriter.Write(value);
    }

    public void WriteLine(Exception message)
    {
        streamWriter.WriteLine(message);
    }

    public void WriteLine(char message)
    {
        streamWriter.WriteLine(message);
    }

    public void WriteLine<T>(T value)
    {
        if (value == null)
            WriteLine();
        else if (value is IFormattable formidable)
            WriteLine(formidable.ToString(null!, streamWriter.FormatProvider));
        else
            WriteLine(value.ToString() ?? "");
    }

    public void Write(char message)
    {
        streamWriter.Write(message);
    }

    public void Dispose()
    {
        streamWriter.Dispose();
    }
}