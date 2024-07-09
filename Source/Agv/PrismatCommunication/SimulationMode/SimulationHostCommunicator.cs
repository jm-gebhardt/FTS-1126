using Gebhardt.Acc.PrismatCommunication.Domain.Services;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.PrismatCommunication.SimulationMode;

/// <summary>
/// Mimics host communication in the simulation mode.
/// </summary>
public class SimulationHostCommunicator(ILogger<SimulationHostCommunicator> logger): IHostCommunicator
{
    /// <inheritdoc cref="IHostCommunicator"/>>
    public async Task ReportOrderCompletion(string hu, string orderId, string destination)
    {
        logger.LogInformation("Reporting order completion to host");
        await Task.CompletedTask;
    }

    /// <inheritdoc cref="IHostCommunicator"/>>
    public void ReportPickCompletion(string hu, string position, DateTime time)
    {
        logger.LogInformation("Reporting pick completion to host");
    }
}