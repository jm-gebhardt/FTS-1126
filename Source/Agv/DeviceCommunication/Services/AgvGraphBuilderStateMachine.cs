using Microsoft.Extensions.Logging;
using MassTransit;
using Gebhardt.StoreWare.Agv.Common.Extensions;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Commands;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Events;
using Gebhardt.StoreWare.Agv.DeviceCommunication.States;


namespace Gebhardt.StoreWare.Agv.DeviceCommunication.Services
{
    /// <summary>
    /// Execute processing of a agv graph request.
    /// </summary>
    /// <remarks>
    /// Request station list and graph and merge both to an internal graph passed to the service AgvManagement.
    /// </remarks>
    [JetBrains.Annotations.UsedImplicitly]
    public sealed class AgvGraphBuilderStateMachine : MassTransitStateMachine<AgvGraphBuilderSaga>
    {
        private readonly ILogger<AgvGraphBuilderStateMachine> _logger;


        /// <summary>
        /// Initial request for AGV graph has been received.
        /// </summary>
        [JetBrains.Annotations.UsedImplicitly]
        public State RequestAgvGraphReceived { get; private set; } = default!;

        /// <summary>
        /// Station list has been received.
        /// </summary>
        [JetBrains.Annotations.UsedImplicitly]
        public State StationListReceived { get; private set; } = default!;


        /// <summary>
        /// Event fired on <see cref="RequestAgvGraph"/> message.
        /// </summary>
        [JetBrains.Annotations.UsedImplicitly]
        public Event<RequestAgvGraph> RequestAgvGraphEvent { get; private set; } = default!;

        /// <summary>
        /// Event fired on <see cref="StationListReceived"/> message.
        /// </summary>
        [JetBrains.Annotations.UsedImplicitly]
        public Event<MqttStationListReceived> StationListEvent { get; private set; } = default!;

        /// <summary>
        /// Event fired on <see cref="MqttGraphReceived"/> message.
        /// </summary>
        [JetBrains.Annotations.UsedImplicitly]
        public Event<MqttGraphReceived> GraphEvent { get; private set; } = default!;


        private EventActivityBinder<AgvGraphBuilderSaga, RequestAgvGraph> RequestAgvGraphHandler() =>
            When(RequestAgvGraphEvent)
                .Then(c => _logger.LogInformation("RequestAgvGraph {AgvId}/{CorrelationId} received", c.Message.AgvId, c.Message.CorrelationId))
                .TransitionTo(RequestAgvGraphReceived)
                // Create a new request ID and store in saga instance.
                .Then(c => c.Saga.CurrentMqttRequestId = Guid.NewGuid())
                // Request the station list.
                .Then(c => c.PublishMqtt(new MqttMessages.MqttRequestStationList(c.Message.AgvId, c.Saga.CurrentMqttRequestId), null));

        private EventActivityBinder<AgvGraphBuilderSaga, MqttStationListReceived> StationListHandler() =>
            When(StationListEvent)
                .Then(c => _logger.LogInformation("Station list {AgvId}/{CorrelationId} received", c.Message.StationList.SerialNumber, c.Saga.CorrelationId))
                .IfElse(c => c.Message.StationList.RequestId == c.Saga.CurrentMqttRequestId,
                    // The response (station list) correlates to the request
                    t => t.Then(_ => _logger.LogDebug("Station list received does match the request"))
                            .TransitionTo(StationListReceived)
                            // Store the station list in the saga instance.
                            .Then(c => c.Saga.MqttStationList = c.Message.StationList)
                            // Create a new request ID and store in saga instance.
                            .Then(c => c.Saga.CurrentMqttRequestId = Guid.NewGuid())
                            // Request the graph.
                            .Then(c => c.PublishMqtt(new MqttMessages.MqttRequestGraph(c.Message.StationList.SerialNumber, c.Saga.CurrentMqttRequestId), null)),
                    // The response does not correlate to the request
                    e => e.Then(_ => _logger.LogWarning("Station list received does not match the request")));

        private EventActivityBinder<AgvGraphBuilderSaga, MqttGraphReceived> GraphHandler() =>
            When(GraphEvent)
                .Then(c => _logger.LogInformation("Graph {AgvId}/{CorrelationId} received", c.Message.Graph.SerialNumber, c.Saga.CorrelationId))
                .IfElse(c => c.Message.Graph.RequestId == c.Saga.CurrentMqttRequestId,
                    // The response (station list) correlates to the request
                    t => t.Then(_ => _logger.LogDebug("Graph received does match the request"))
                            // Store the graph in the saga instance.
                            .Then(c => c.Saga.MqttGraph = c.Message.Graph)
                            // Build the internal graph and store in the saga instance.
                            .Then(c => c.Saga.MergeGraph())
                            // Respond to the initial request with the graph
                            .Then(c => c.Publish(new Common.Messages.AgvManagement.Events.AgvGraphReceived(c.Saga.CorrelationId, c.Message.Graph.SerialNumber, c.Saga.AgvGraph!)))
                            .Finalize(),
                    // The response does not correlate to the request
                    e => e.Then(_ => _logger.LogWarning("Graph received does not match the request")))
                            .Finalize();


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
    }
}
