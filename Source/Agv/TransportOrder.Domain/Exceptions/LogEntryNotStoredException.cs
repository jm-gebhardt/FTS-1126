namespace Gebhardt.Acc.TransportOrder.Domain.Exceptions;

/// <summary>
/// The exception raised when a log entry has not been stored.
/// </summary>
public class LogEntryNotStoredException: Exception
{
    /// <inheritdoc cref="LogEntryNotStoredException"/>>
    public LogEntryNotStoredException(int logCode, DateTime timestamp) : base($"Entry for log {logCode} at {timestamp} not stored"){}
}