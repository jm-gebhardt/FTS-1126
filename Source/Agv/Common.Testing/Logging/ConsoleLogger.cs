using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.Common.Testing.Logging;

/// <summary>
/// Minimal <see cref="ILogger{TCategoryName}"/> implementation that writes log entries to the console,
/// so they show up in the Visual Studio Test Explorer test output.
/// </summary>
[PublicAPI]
public sealed class ConsoleLogger<T> : ILogger<T>
{
    IDisposable? ILogger.BeginScope<TState>(TState state) => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        Console.WriteLine($"[{logLevel}] {typeof(T).Name}: {formatter(state, exception)}");
        if (exception is not null)
        {
            Console.WriteLine(exception);
        }
    }
}
