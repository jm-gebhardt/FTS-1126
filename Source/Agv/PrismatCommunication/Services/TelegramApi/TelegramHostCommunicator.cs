using Gebhardt.Acc.PrismatCommunication.Domain.Services;
using Gebhardt.Acc.PrismatCommunication.TelegramApi.Processors;
using Gebhardt.Acc.PrismatCommunication.TelegramApi.Telegrams;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.PrismatCommunication.Services.TelegramApi;

/// <summary>
/// Represents a communicator with prismat using telegrams
/// </summary>
public class TelegramHostCommunicator(ILogger<TelegramHostCommunicator> logger, ToPrismatTelegramProcessor telegramProcessor) : IHostCommunicator
{
    /// <inheritdoc cref="IHostCommunicator"/>>
    public async Task ReportOrderCompletion(string hu, string orderId, string destination)
    {
        logger.LogDebug("Reporting order completion to prismat");
        telegramProcessor.EnqueueTelegram(new Arrival(DateTime.UtcNow, destination, hu));
        await Task.CompletedTask;
    }

    /// <inheritdoc cref="IHostCommunicator"/>>
    public void ReportPickCompletion(string hu, string position, DateTime time)
    {
        logger.LogDebug("Reporting pick completion to prismat");
        telegramProcessor.EnqueueTelegram(new Pick(hu, position, time));
    }

    /// <inheritdoc cref="IHostCommunicator"/>>
    public void ReportOrderError(string hu, string source, string destination, int errorCode, string errorInfo, DateTime time)
    {
        logger.LogDebug("Reporting transport order error to prismat");
        telegramProcessor.EnqueueTelegram(new TordError(hu, source, destination, errorInfo, errorCode, time));
    }
    
    /// <inheritdoc cref="IHostCommunicator"/>>
    public void ReportSystemStatus(bool isReady, int errorCode, string errorInfo, DateTime time)
    {
        logger.LogDebug("Reporting system status to prismat");
        telegramProcessor.EnqueueTelegram(new Status(isReady? SystemState.AUTO: SystemState.AUS, time, errorCode, errorInfo));
    }
}