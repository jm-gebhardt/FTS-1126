using Microsoft.Extensions.Logging;
using MassTransit;
using Gebhardt.StoreWare.Agv.Common.Messages.HostOrder.Events;

namespace Gebhardt.StoreWare.Agv.WCSCommunication.Services;

/// <summary>
/// Simulate finishing of host orders
/// </summary>
public class SimulationConfirmHostOrderConsumer :
    IConsumer<HostOrderHandlingSucceeded>,
    IConsumer<HostOrderHandlingFailed>
{
    private readonly ILogger<SimulationConfirmHostOrderConsumer> _logger;

    /// <summary>
    /// Create consumer with message handler delegate for simulation.
    /// </summary>
    /// <param name="logger">Mandatory logger facility.</param>
    [JetBrains.Annotations.PublicAPI]
    public SimulationConfirmHostOrderConsumer(ILogger<SimulationConfirmHostOrderConsumer> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Simulation: Handle message <see cref="HostOrderHandlingSucceeded"/>.
    /// <para>Logs received message and then discards it.</para>
    /// </summary>
    /// <param name="context">Context of the consumer wrapping details surrounding the inbound message.</param>
    public Task Consume(ConsumeContext<HostOrderHandlingSucceeded> context)
    {
        _logger.LogInformation("{MessageType} for order {OrderId} / {CorrelationId}",
            nameof(HostOrderHandlingSucceeded), context.Message.OrderId, context.Message.CorrelationId
        );

        return Task.CompletedTask;
    }

    /// <summary>
    /// Simulation: Handle message <see cref="HostOrderHandlingFailed"/>.
    /// <para>Logs received message and then discards it.</para>
    /// </summary>
    /// <param name="context">Context of the consumer wrapping details surrounding the inbound message.</param>
    public Task Consume(ConsumeContext<HostOrderHandlingFailed> context)
    {
        _logger.LogInformation("{MessageType} for order {OrderId} / {CorrelationId}",
            nameof(HostOrderHandlingFailed), context.Message.OrderId, context.Message.CorrelationId
        );

        return Task.CompletedTask;
    }
}