using Gebhardt.Acc.PrismatCommunication.Domain.Services;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.PrismatCommunication.PrismatApi.TelegramApi;

/// <summary>
/// Represents a communicator with prismat using telegrams
/// </summary>
public class TelegramHostCommunicator(ILogger<TelegramHostCommunicator> logger) : IHostCommunicator
{
    /// <inheritdoc cref="IHostCommunicator"/>>
    public async Task ReportOrderCompletion(string hu, string orderId, string destination)
    {
       logger.LogInformation("Reporting order completion to prismat");
       await Task.CompletedTask;
    }
}