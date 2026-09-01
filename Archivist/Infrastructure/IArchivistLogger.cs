namespace dRz.GPT_Utilities.Archivist.Infrastructure;

/// <summary>
/// Абстракция диагностического вывода Archivist.
/// </summary>
internal interface IArchivistLogger
{
    void Trace(string message);
    void Warning(string message);
    void Success(string message);
    void Update(string message);
    void Error(string message, Exception? exception = null);
}

/// <summary>
/// Адаптер консольного вывода приложения.
/// </summary>
internal sealed class ConsoleArchivistLogger : IArchivistLogger
{
    public void Trace(string message) => ConsoleWriter.Trace(message);

    public void Warning(string message) => ConsoleWriter.Warn(message);

    public void Success(string message) => ConsoleWriter.Success(message);

    public void Update(string message) => ConsoleWriter.Update(message);

    public void Error(string message, Exception? exception = null) =>
        ConsoleWriter.Error(message, exception);
}
