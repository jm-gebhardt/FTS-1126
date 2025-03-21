using Gebhardt.Acc.Common.Domain.Utilities;
using JetBrains.Annotations;

namespace Gebhardt.Acc.KaeserSidecar.Domain.Models.Telegram;

/// <summary>
/// A telegram.
/// </summary>
[PublicAPI]
public class Telegram
{
    /// <summary>
    /// The id of the telegram.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// The message.
    /// </summary>
    public string Message { get; init; }

    /// <summary>
    /// The telegram function.
    /// </summary>
    public string Function { get; init; }

    /// <summary>
    /// The time the telegram was queued.
    /// </summary>
    public DateTime QueuedDate { get; init; }

    /// <inheritdoc cref="Telegram"/>>
    protected Telegram(Guid id, string message, string function, DateTime queuedDate)
    {
        Id = id;
        Message = message;
        Function = function;
        QueuedDate = queuedDate;
    }

    /// <inheritdoc cref="Telegram"/>>
    protected Telegram(string message, string function)
    {
        Id = message.ToGuid();
        Message = message;
        Function = function;
        QueuedDate = DateTime.UtcNow;
    }

    /// <summary>
    /// Create a telegram from its function and message.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <param name="function">The function.</param>
    /// <returns>A telegram.</returns>
    public static Telegram Create(string message, string function)
    {
        if (nameof(AgvAplCargoStateTelegram).Contains(function))
        {
            return new AgvAplCargoStateTelegram(message, function);
        }
        return new AgvWeStationTelegram(message, function);
    }
}