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
        logger.LogInformation("Reporting order completion to prismat");
        telegramProcessor.EnqueueTelegram(new Arrival(DateTime.UtcNow, destination, hu));
        await Task.CompletedTask;
    }

    /// <inheritdoc cref="IHostCommunicator"/>>
    public void ReportPickCompletion(string hu, string position, DateTime time)
    {
        logger.LogInformation("Reporting pick completion to prismat");
        telegramProcessor.EnqueueTelegram(new Pick(hu, position, time));
    }
}