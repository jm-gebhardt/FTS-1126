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
        logger.LogInformation("Reporting order completion to host: HU {Hu} OId {OrderId} Dest {Dest}", hu, orderId, destination);
        await Task.CompletedTask;
    }

    /// <inheritdoc cref="IHostCommunicator"/>>
    public void ReportPickCompletion(string hu, string position, DateTime time)
    {
        logger.LogInformation("Reporting pick completion to host: HU {Hu} Pos {Position}", hu, position);
    }

    /// <inheritdoc cref="IHostCommunicator"/>>
    public void ReportOrderError(string hu, string source, string destination, int errorCode, string errorInfo, DateTime time)
    {
        logger.LogInformation("Reporting transport order error to host: HU {Hu}, Src {Src}, Dest {Dest}, Err {ErrorCode}, Info {Info}", hu, source, destination, errorCode, errorInfo);
    }
}