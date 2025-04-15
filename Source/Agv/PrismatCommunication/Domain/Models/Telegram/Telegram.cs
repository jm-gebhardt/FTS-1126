using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.PrismatCommunication.TelegramApi.Telegrams;
using JetBrains.Annotations;

namespace Gebhardt.Acc.PrismatCommunication.Domain.Models.Telegram;

/// <summary>
/// Represents a telegram.
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
    public Telegram(Guid id, string message, string function, DateTime queuedDate)
    {
        Id = id;
        Message = message;
        Function = function;
        QueuedDate = queuedDate;
    }

    /// <inheritdoc cref="Telegram"/>>
    public Telegram(string message, string function)
    {
        Id = message.ToGuid();
        Message = message;
        Function = function;
        QueuedDate = DateTime.UtcNow;
    }

    /// <summary>
    /// Convert to <see cref="PrismatTelegram"/>.
    /// </summary>
    public PrismatTelegram ToPrismatTelegram()
    {
        switch (Function)
        {
            case nameof(TordError):
            {
                var tordError = new TordError();
                tordError.Load(Message);
                return tordError;
            }

            case nameof(Pick):
            {
                var pick = new Pick("hu", "pos", DateTime.UtcNow);
                pick.Load(Message);
                return pick;
            }

            case nameof(Arrival):
            {
                var arrival = new Arrival(DateTime.UtcNow, "hu", "pos");
                arrival.Load(Message);
                return arrival;
            }

            case nameof(Status):
            {
                var status = new Status();
                status.Load(Message);
                return status;
            }
        }
        var prismatTelegram = new PrismatTelegram();
        prismatTelegram.Load(Message);
        return prismatTelegram;
    }
}