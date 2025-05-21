using System.Text;

namespace Gebhardt.Acc.Common.TelegramCommunication.Telegram;

/// <summary>
/// Provides a base class for telegram processors.
/// </summary>
/// <typeparam name="TTelegramType"></typeparam>
public abstract class TelegramProcessorBase<TTelegramType> : ITelegramProcessor
where TTelegramType : TelegramBase
{
    /// <summary>
    /// Set to true, if the connection should be restarted.
    /// </summary>
    protected bool RestartRequested { get; set; }

    /// <summary>
    /// Enqueue a telegram for sending
    /// </summary>
    /// <param name="telegram">The telegram</param>
    public abstract bool EnqueueTelegram(TTelegramType telegram);

    /// <summary>
    /// Get the next telegram to send (e.g. acknowledgement)
    /// </summary>
    /// <param name="telegram">The next telegram to send.</param>
    /// <returns>True, if getting the telegram was successful.</returns>
    public abstract bool GetNextTelegramToSend(out string telegram);

    bool ITelegramProcessor.EnqueueTelegram(TelegramBase telegram)
    {
        return EnqueueTelegram((TTelegramType)telegram);
    }

    /// <inheritdoc cref="ITelegramProcessor"/>>
    public abstract bool HasSendDemand();

    /// <summary>
    /// Get the encoding for binary conversion
    /// </summary>
    /// <returns></returns>
    public abstract Encoding GetEncoding();

    /// <summary>
    /// If true, next step is to receive a telegram (either acknowledgement or message telegram)
    /// </summary>
    /// <returns></returns>
    public abstract bool ShouldReceiveTelegram();

    /// <summary>
    /// Process received telegram
    /// </summary>
    /// <param name="telegram"></param>
    public abstract void ProcessReceivedTelegram(string telegram);

    /// <summary>
    /// Get the length of the telegram (bytes).
    /// </summary>
    public abstract int GetTelegramLength();

    /// <summary>
    /// Get the time to wait for a telegram.
    /// </summary>
    public abstract int GetWaitingTime();

    /// <summary>
    /// True if the connection should be restarted
    /// </summary>
    /// <returns></returns>
    public bool ShouldRestart() => RestartRequested;

    /// <summary>
    /// Cancel telegram processing
    /// </summary>
    public abstract void Cancel();

    /// <summary>
    /// Reset <see cref="TelegramBase.Seq"/> counter
    /// </summary>
    public abstract void ResetCounter();

    /// <summary>
    /// True, if life telegrams are used on the connection.
    /// </summary>
    public abstract bool GetUseLifeTelegrams();

    /// <summary>
    /// Initialize the processor, like resetting things.
    /// </summary>
    public void Init()
    {
        RestartRequested = false;
    }
}