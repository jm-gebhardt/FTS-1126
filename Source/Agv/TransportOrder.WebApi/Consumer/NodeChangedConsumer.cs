using Gebhardt.Acc.Common.Domain.Constants;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.TransportOrder.Domain.Services.LogEntry;
using Gebhardt.Acc.TransportOrder.Messages.Events.Map;
using Gebhardt.Acc.TransportOrder.WebApi.Hubs.Map;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.TransportOrder.WebApi.Consumer;

/// <summary>
/// Represents the handler for node changes.
/// </summary>
[UsedImplicitly]
public class NodeChangedConsumer :
    IConsumer<NodeStateChanged>,
    IConsumer<NodeApproachingPropertiesUpdated>,
    IConsumer<JamStateChanged>
{
    private readonly ILogger<NodeChangedConsumer> _logger;
    private readonly IMapPublisher _mapPublisher;
    private readonly ILoggingService _loggingService;

    /// <summary>
    /// Represents the handler for node changes.
    /// </summary>
    public NodeChangedConsumer(IMapPublisher mapPublisher,
        ILogger<NodeChangedConsumer> logger, ILoggingService loggingService)
    {
        _logger = logger;
        _mapPublisher = mapPublisher;
        _loggingService = loggingService;
    }

    /// <summary>
    /// Handles the <see cref="NodeStateChanged"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<NodeStateChanged> context)
    {
        var (nodeLock, nodeName, stationId, modifyingUser, lockReason, lockDescription) = context.Message;

       if (stationId != null && !nodeLock.IsLocked)
        {
            await _loggingService.LogStation(_logger, LogLevel.Information,
                 LogCodes.StationStateChanged,
                stationId.Value,
                "State Changed",
                $"State of station changed to '{nodeLock.NodeState}'",
                new Dictionary<string, object?> { { LogLabels.NodeState, nodeLock.NodeState } });
        }

        await _mapPublisher.SendNodeStateChangedAsync(nodeLock, nodeName, stationId, modifyingUser, lockReason, lockDescription);
    }

    /// <summary>
    /// Handles the <see cref="NodeApproachingPropertiesUpdated"/> command.
    /// </summary>
    public Task Consume(ConsumeContext<NodeApproachingPropertiesUpdated> context)
    {
        (var mapId, var node, double? pickLiftHeight, double? dropLiftHeight, DockingDirection? dockingDirection) = context.Message;
        return _mapPublisher.SendNodeApproachingPropertiesUpdated(mapId, node.Name, pickLiftHeight, dropLiftHeight, dockingDirection);
    }

    /// <summary>
    /// Handles the <see cref="JamStateChanged"/> event.
    /// </summary>
    public Task Consume(ConsumeContext<JamStateChanged> context)
    {
        return _mapPublisher.SendJamStateUpdate(context.Message.CorrelationId, context.Message.HasPalletJam);
    }
}