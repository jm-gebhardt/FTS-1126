namespace Gebhardt.Acc.KaeserSidecar.Domain.Models.Telegram;

/// <summary>
/// The AgvWeStation telegram
/// </summary>
public class AgvWeStationTelegram: Telegram
{
    /// <inheritdoc cref="AgvWeStationTelegram"/>>
    public AgvWeStationTelegram(Guid id, string message, string function, DateTime queuedDate):base(id, message, function, queuedDate)
    {
    }

    /// <inheritdoc cref="AgvWeStationTelegram"/>>
    public AgvWeStationTelegram(string message, string function) : base(message, function)
    {
    }
}