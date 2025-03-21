namespace Gebhardt.Acc.KaeserSidecar.Domain.Models.Telegram;

/// <summary>
/// The AgvAplCargoState telegram
/// </summary>
public class AgvAplCargoStateTelegram : Telegram
{
    /// <inheritdoc cref="AgvAplCargoStateTelegram"/>>
    public AgvAplCargoStateTelegram(Guid id, string message, string function, DateTime queuedDate) : base(id, message, function, queuedDate)
    {
    }

    /// <inheritdoc cref="AgvAplCargoStateTelegram"/>>
    public AgvAplCargoStateTelegram(string message, string function) : base(message, function)
    {
    }
}