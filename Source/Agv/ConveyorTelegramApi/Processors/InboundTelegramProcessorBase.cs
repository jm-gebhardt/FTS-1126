using System.Text;
using Gebhardt.Acc.Common.TelegramCommunication.Configuration;
using Gebhardt.Acc.Common.TelegramCommunication.Telegram;
using Gebhardt.Acc.StationCommunication.Domain.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Gebhardt.Acc.ConveyorTelegramApi.Processors;

/// <summary>
/// Processor base for inbound telegrams
/// </summary>
/// <typeparam name="TTelegramType"></typeparam>
public class InboundTelegramProcessorBase<TTelegramType>([FromKeyedServices(ServiceKeyProvider.FromWeStation)] TelegramCommunicationConfiguration configuration) : TelegramProcessorBase<TTelegramType>
    where TTelegramType : TelegramBase
{
    /// <summary>
    ///If true, should receive telegrams
    /// </summary>
    protected bool ShouldReceive { get; set; } = true;

    /// <summary>
    /// Outbound telegram sequence counter
    /// </summary>
    protected SeqCounter SequenceCounter { get; } = new(1, 999, 3);

    /// <inheritdoc cref="TelegramProcessorBase{TTelegramType}"/>>
    public override bool EnqueueTelegram(TTelegramType telegram)
    {
        // not applicable for inbound telegrams
        return true;
    }

    /// <inheritdoc cref="TelegramProcessorBase{TTelegramType}"/>>
    public override bool GetNextTelegramToSend(out string telegram)
    {
        // implemented by parent
        throw new NotImplementedException();
    }

    /// <inheritdoc cref="TelegramProcessorBase{TTelegramType}"/>>
    public override bool ShouldReceiveTelegram()
    {
        return ShouldReceive;
    }

    /// <inheritdoc cref="TelegramProcessorBase{TTelegramType}"/>>
    public override void ProcessReceivedTelegram(string telegram)
    {
        // implemented by parent
        throw new NotImplementedException();
    }

    /// <inheritdoc cref="TelegramProcessorBase{TTelegramType}"/>>
    public override int GetTelegramLength()
    {
        // Enforce usage of length independent receive method
        return 0;
    }

    /// <inheritdoc cref="TelegramProcessorBase{TTelegramType}"/>>
    public override int GetWaitingTime()
    {
        return configuration.TelegramWaitingTime;
    }

    /// <inheritdoc />
    public override void Cancel()
    {
    }

    /// <inheritdoc cref="TelegramProcessorBase{TTelegramType}"/>>
    public override void ResetCounter()
    {
        // not applicable for inbound telegrams
    }

    /// <inheritdoc cref="TelegramProcessorBase{TTelegramType}"/>>
    public override bool GetUseLifeTelegrams()
    {
        return configuration.UseLifeTelegrams;
    }

    /// <inheritdoc cref="TelegramProcessorBase{TTelegramType}"/>>
    public override bool HasSendDemand()
    {
        // not applicable for inbound telegrams
        return false;
    }

    /// <inheritdoc cref="TelegramProcessorBase{TTelegramType}"/>>
    public override Encoding GetEncoding()
    {
        return Encoding.UTF8;
    }
}