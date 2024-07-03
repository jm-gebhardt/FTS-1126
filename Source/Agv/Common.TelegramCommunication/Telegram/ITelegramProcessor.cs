using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.TelegramCommunication.Telegram;

/// <summary>
/// Provides an interface for telegram processors
/// </summary>
public interface ITelegramProcessor
{
    /// <summary>
    /// Enqueue a telegram for sending
    /// </summary>
    /// <param name="telegram">The telegram</param>
    [PublicAPI]
    bool EnqueueTelegram(TelegramBase telegram);

    /// <summary>
    /// Get the next telegram to send (e.g. acknowledgement)
    /// </summary>
    /// <param name="telegram">The next telegram to send.</param>
    /// <returns>True, if getting the telegram was successful.</returns>
    bool GetNextTelegramToSend(out string? telegram);

    /// <summary>
    /// If true, next step is to receive a telegram (either acknowledgement or message telegram)
    /// </summary>
    /// <returns></returns>
    [PublicAPI]
    public bool ShouldReceiveTelegram();

    /// <summary>
    /// Process received telegram
    /// </summary>
    /// <param name="telegram"></param>
    public void ProcessReceivedTelegram(string telegram);

    /// <summary>
    /// If true the processor needs to send
    /// </summary>
    /// <returns></returns>
    [PublicAPI]
    public bool HasSendDemand();

    /// <summary>
    /// Cancel telegram processing
    /// </summary>
    public void Cancel();
}