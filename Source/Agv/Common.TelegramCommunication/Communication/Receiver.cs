using Gebhardt.Acc.Common.TelegramCommunication.Telegram;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.Common.TelegramCommunication.Communication;

/// <summary>
/// Represents a receiver for telegrams of type <typeparam name="TTelegramType"/>
/// </summary>
public class Receiver<TTelegramType> : SocketCommunicator<TTelegramType>
    where TTelegramType : TelegramBase
{
    /// <inheritdoc cref="Receiver{TTelegramType}"/>>
    public Receiver(SocketBase socket, string name, TelegramProcessorBase<TTelegramType> telegramProcessor, ILogger<SocketCommunicator<TTelegramType>> logger) : base(socket, name, telegramProcessor, logger)
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
            if (TelegramProcessor.ShouldReceiveTelegram())
            {
                // Receive telegram
                Logger.LogDebug("Receiving telegram");

                if (Socket.TryReceive(out string telegram, TelegramProcessor.GetTelegramLength(), TelegramProcessor.GetWaitingTime(), TelegramProcessor.GetEncoding()))
                {
                    Logger.LogDebug("Received telegram {Telegram}", telegram);
                    TelegramProcessor.ProcessReceivedTelegram(telegram);
                }
                else
                {
                    //If life telegrams are used, the connection needs to be restarted always
                    //If no life telegrams are used timeout can not be used
                    if (TelegramProcessor.GetUseLifeTelegrams() || telegram != SocketBase.TimeoutTelegram)
                    {
                        if (telegram == SocketBase.TimeoutTelegram)
                        {
                            Logger.LogError("No life telegram received");
                        }
                        Logger.LogDebug("Trigger restart");
                        RestartMe = true;
                        SetRun = false;
                        SendRestartRequest("Failed to receive telegram");
                    }
                }
            }

            if (!TelegramProcessor.ShouldReceiveTelegram())
            {
                // Send acknowledgement
                if (TelegramProcessor.GetNextTelegramToSend(out string acknowledgement))
                {
                    Logger.LogDebug("Sending acknowledgement {Acknowledgement}", acknowledgement);

                    if (!Socket.TrySend(acknowledgement, TelegramProcessor.GetEncoding()))
                    {
                        Logger.LogError("Failed sending acknowledgement {Acknowledgement}", acknowledgement);
                        SetRun = false;
                        RestartMe = true;
                        SendRestartRequest("Failed to send acknowledgement");
                    }
                    else
                    {
                        Logger.LogDebug("Acknowledgement {Acknowledgement} sent", acknowledgement);
                    }
                }
            }

            if (TelegramProcessor.ShouldRestart())
            {
                SetRun = false;
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