using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.Messages.AgvManagement.Commands;
using Gebhardt.Acc.DeviceCommunication.Common.Messages;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Events;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Messages.Mqtt;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.States;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.DeviceCommunication.KarisAgv.Services;

/// <summary>
/// Execute processing of an agv graph request.
/// </summary>
/// <remarks>
/// Request station list and graph and merge both to an internal graph passed to the service AgvManagement.
/// </remarks>
[UsedImplicitly]
public sealed class AgvGraphBuilderStateMachine : MassTransitStateMachine<AgvGraphBuilderSaga>
{
    private readonly ILogger<AgvGraphBuilderStateMachine> _logger;

    /// <summary>
    /// Initial request for AGV graph has been received.
    /// </summary>
    [UsedImplicitly]
    public State RequestAgvGraphReceived { get; private set; } = null!;

    /// <summary>
    /// Station list has been received.
    /// </summary>
    [UsedImplicitly]
    public State StationListReceived { get; private set; } = null!;

    /// <summary>
    /// Event fired on <see cref="RequestAgvGraph"/> message.
    /// </summary>
    [UsedImplicitly]
    public Event<RequestAgvGraph> RequestAgvGraphEvent { get; private set; } = null!;

    /// <summary>
    /// Event fired on <see cref="StationListReceived"/> message.
    /// </summary>
    [UsedImplicitly]
    public Event<InternalStationListReceived> StationListEvent { get; private set; } = null!;

    /// <summary>
    /// Event fired on <see cref="InternalGraphReceived"/> message.
    /// </summary>
    [UsedImplicitly]
    public Event<InternalGraphReceived> GraphEvent { get; private set; } = null!;

    /// <summary>
    /// Handle requesting AGV graph by master control.
    /// </summary>
    public AgvGraphBuilderStateMachine(ILogger<AgvGraphBuilderStateMachine> logger)
    {
        _logger = logger;

        // Initial creation
        Event(() => RequestAgvGraphEvent, e =>
            e.SetSagaFactory(context => new AgvGraphBuilderSaga(context.Message.CorrelationId))
        );
        Event(() => StationListEvent);
        Event(() => GraphEvent);

        Initially(RequestAgvGraphHandler());
        DuringAny(RequestAgvGraphHandler());

        During(RequestAgvGraphReceived, StationListHandler());
        During(StationListReceived, GraphHandler());

        SetCompletedWhenFinalized();
    }

    #region State transitions
    private EventActivityBinder<AgvGraphBuilderSaga, RequestAgvGraph> RequestAgvGraphHandler()
    {
        return When(RequestAgvGraphEvent)
            .Then(c => _logger.LogInformation("{MessageType} {AgvId} / {CorrelationId} received", nameof(RequestAgvGraph), c.Message.SerialNumber, c.Message.CorrelationId))
            .TransitionTo(RequestAgvGraphReceived)
            // Create a new request ID and store in saga instance.
            .Then(c => c.Saga.CurrentMqttRequestId = Guid.NewGuid())
            // Request the station list.
            .Then(c => c.PublishMqtt(new MqttRequestStationList(c.Message.SerialNumber, c.Saga.CurrentMqttRequestId), null));
    }

    private EventActivityBinder<AgvGraphBuilderSaga, InternalStationListReceived> StationListHandler()
    {
        return When(StationListEvent)
            .Then(c => _logger.LogInformation("{MessageType} {AgvId} / {CorrelationId} received", nameof(InternalStationListReceived), c.Message.StationList.SerialNumber, c.Saga.CorrelationId))
            .IfElse(c => c.Message.StationList.RequestId == c.Saga.CurrentMqttRequestId,
                // The response (station list) correlates to the request
                t => t.Then(_ => _logger.LogDebug("{MessageType} does match the request", nameof(InternalStationListReceived)))
                    .TransitionTo(StationListReceived)
                    // Store the station list in the saga instance.
                    .Then(c => c.Saga.MqttStationList = c.Message.StationList)
                    // Create a new request ID and store in saga instance.
                    .Then(c => c.Saga.CurrentMqttRequestId = Guid.NewGuid())
                    // Request the graph.
                    .Then(c => c.PublishMqtt(new MqttRequestGraph(c.Message.StationList.SerialNumber, c.Saga.CurrentMqttRequestId), null)),
                // The response does not correlate to the request
                e => e.Then(_ => _logger.LogWarning("{MessageType} does not match the request", nameof(InternalStationListReceived))));
    }

    private EventActivityBinder<AgvGraphBuilderSaga, InternalGraphReceived> GraphHandler()
    {
        return When(GraphEvent)
            .Then(c => _logger.LogInformation("{MessageType} {AgvId} / {CorrelationId} received", nameof(InternalGraphReceived), c.Message.Graph.SerialNumber, c.Saga.CorrelationId))
            .IfElse(c => c.Message.Graph.RequestId == c.Saga.CurrentMqttRequestId,
                // The response (station list) correlates to the request
                t => t.Then(_ => _logger.LogDebug("{MessageType} does match the request", nameof(InternalGraphReceived)))
                    // Store the graph in the saga instance.
                    .Then(c => c.Saga.MqttGraph = c.Message.Graph)
                    // Build the internal graph and store in the saga instance.
                    .Then(c => c.Saga.MergeGraph())
                    // Respond to the initial request with the graph
                    .Then(c => c.Publish(new InternalAgvGraphReceived(c.Saga.CorrelationId, c.Message.Graph.SerialNumber, c.Saga.AgvGraph!)))
                    .Then(c => c.Publish(new RequestAgvMap(c.Saga.CorrelationId, c.Message.Graph.SerialNumber, c.Saga.RelatedMapId, c.Saga.RelatedMapName!)))
                    .Finalize(),
                // The response does not correlate to the request
                e => e.Then(_ => _logger.LogWarning("{MessageType} does not match the request", nameof(InternalGraphReceived))))
            .Finalize();
    }
    #endregion
}