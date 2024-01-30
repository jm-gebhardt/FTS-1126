using Gebhardt.StoreWare.Agv.WCSCommunication.Domain.Services;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.WCSCommunication.WcsApi.SimulationMode;

/// <summary>
///  Simulate finishing of host orders
/// </summary>
[UsedImplicitly]
public class SimulationHostCommunicator : IHostCommunicator
{
    private readonly ILogger<SimulationHostCommunicator> _logger;

    /// <inheritdoc cref="SimulationHostCommunicator"/>>
    public SimulationHostCommunicator(ILogger<SimulationHostCommunicator> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc cref="IHostCommunicator"/>>
    public Task<bool> TransmitHostOrderHandlingSucceeded(string orderId)
    {
        _logger.LogInformation("{Method} for order {OrderId}",
            nameof(TransmitHostOrderHandlingSucceeded), orderId);
        return Task.FromResult(true);
    }

    /// <inheritdoc cref="IHostCommunicator"/>>
    public Task<bool> TransmitHostOrderStarted(string orderId, string assignedAgv)
    {
        _logger.LogInformation("{Method} for order {OrderId} with {Agv}",
            nameof(TransmitHostOrderStarted), orderId, assignedAgv);
        return Task.FromResult(true);
    }

    /// <inheritdoc cref="IHostCommunicator"/>>
    public Task<bool> TransmitHostOrderHandlingFailure(string orderId, string error)
    {
        _logger.LogInformation("{Method} for order {OrderId} with {Error}",
            nameof(TransmitHostOrderHandlingFailure), orderId, error);
        return Task.FromResult(true);
    }

    /// <inheritdoc cref="IHostCommunicator"/>>
    public Task<bool> TransmitHostOrderCancellationFailure(string orderId, string error)
    {
        _logger.LogInformation("{Method} for order {OrderId} with {Error}",
            nameof(TransmitHostOrderCancellationFailure), orderId, error);
        return Task.FromResult(true);
    }

    /// <inheritdoc cref="IHostCommunicator"/>>
    public Task<bool> TransmitHostOrderCancellationSucceeded(string orderId)
    {
        _logger.LogInformation("{Method} for order {OrderId}",
            nameof(TransmitHostOrderCancellationSucceeded), orderId);
        return Task.FromResult(true);
    }
}