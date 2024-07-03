using Gebhardt.Acc.Common.TelegramCommunication.Telegram;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.Common.TelegramCommunication.Communication;

/// <summary>
/// Represents a sender for telegrams of type <typeparam name="TTelegramType"/>
/// </summary>
public class Sender<TTelegramType> : SocketCommunicator<TTelegramType>
    where TTelegramType : TelegramBase
{
    /// <inheritdoc cref="Sender{TTelegramType}"/>>
    public Sender(SocketBase socket, string name, TelegramProcessorBase<TTelegramType> telegramProcessor, ILogger<SocketCommunicator<TTelegramType>> logger) : base(socket, name, telegramProcessor, logger)
    {
    }

    /// <inheritdoc cref="SocketCommunicator{TTelegramType}"/>>
    public override bool RestartMe
    {
        get => TelegramProcessor.ShouldRestart();

        protected set
        {
            if (value != true)
            {
                return;
            }
            if (!TelegramProcessor.ShouldRestart())
            {
                TelegramProcessor.RestartConnection();
            }
            else
            {
                Logger.LogWarning("Already restarted");
            }
        }
    }

    /// <inheritdoc cref="SocketCommunicator{TTelegramType}"/>>
    public override void BeginStop()
    {
        base.BeginStop();

        TelegramProcessor.Cancel();
    }

    /// <inheritdoc cref="SocketCommunicator{TTelegramType}"/>>
    protected override void Stop(bool reset = false)
    {
        base.Stop(reset);
        Logger.LogDebug("Resetting counter");
        if (reset)
        {
            TelegramProcessor.ResetCounter();
        }
    }

    /// <inheritdoc cref="SocketCommunicator{TTelegramType}"/>>
    protected override bool ExchangeMessages()
    {
        try
        {
            if (!TelegramProcessor.ShouldReceiveTelegram())
            {
                // Send next telegram
                if (TelegramProcessor.GetNextTelegramToSend(out string telegram))
                {
                    Logger.LogDebug("Sending telegram {Telegram}", telegram);

                    if (!Socket.TrySend(telegram, TelegramProcessor.GetEncoding()))
                    {
                        Logger.LogError("Failed sending telegram {Telegram}", telegram);
                    }
                    else
                    {
                        Logger.LogDebug("Telegram {Telegram} sent", telegram);
                    }
                }
                else
                {
                   Logger.LogDebug("No telegram for sending available");
                }
            }

            if (TelegramProcessor.ShouldReceiveTelegram())
            {
                // Receive acknowledgement
                Logger.LogDebug("Receiving acknowledgement");
                if (Socket.TryReceive(out string acknowledgment, TelegramProcessor.GetTelegramLength(), TelegramProcessor.GetWaitingTime(), TelegramProcessor.GetEncoding()))
                {
                    Logger.LogDebug("Received acknowledgement {Acknowledgement}", acknowledgment);
                    TelegramProcessor.ProcessReceivedTelegram(acknowledgment);
                }
                else
                {
                    Logger.LogWarning("No acknowledgement {Acknowledgement} received", acknowledgment);
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError("{Name}: Could not exchange messages, got : {ErrorMessage} : {Trace}", Name, ex.Message, ex.StackTrace);
            return false;
        }
    }
}