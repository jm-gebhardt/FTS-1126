using MassTransit;


namespace Gebhardt.StoreWare.Agv.DeviceCommunication.States;

/// <summary>
/// Saga instance for <see cref="Services.AgvGraphBuilderStateMachine"/>
/// </summary>
public class AgvGraphBuilderSaga : SagaStateMachineInstance, CorrelatedBy<Guid>
{
    /// <summary>
    /// Correlation ID taken from the initial request "<see cref="Common.Messages.AgvManagement.Commands.RequestAgvGraph"/>".
    /// <para>
    /// See <see href="https://masstransit-project.com//usage/correlation.html" />
    /// </para></summary>
    public Guid CorrelationId { get; set; }

    /// <summary>
    /// Current state of this state machine instance.
    /// </summary>
    [JetBrains.Annotations.UsedImplicitly]
    public State CurrentState { get; set; } = default!;

    /// <summary>
    /// Request ID of a pending MQTT request.
    /// <para>Used to correlate MQTT response.</para>
    /// </summary>
    public Guid CurrentMqttRequestId { get; set; }

    /// <summary>
    /// Station list.
    /// </summary>
    public MqttMessages.MqttStationList? MqttStationList { get; set; }

    /// <summary>
    /// Graph.
    /// </summary>
    public MqttMessages.MqttGraph? MqttGraph { get; set; }

    /// <summary>
    /// Merged graph.
    /// </summary>
    public Common.Messages.AgvManagement.AgvGraph? AgvGraph { get; private set; }


    /// <summary>
    /// Create new saga instance.
    /// </summary>
    /// <param name="correlationId">Mandatory correlation ID.</param>
    public AgvGraphBuilderSaga(Guid correlationId)
    {
        if (correlationId == Guid.Empty)
        {
            throw new InvalidOperationException($"{GetType()} requires correlation ID.");
        }
        CorrelationId = correlationId;
    }


    /// <summary>
    /// Merge graph and station list into an <see cref="Common.Messages.AgvManagement.AgvGraph"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">Station list/graph missing or validation issue..</exception>
    public void MergeGraph()
    {
        if (MqttStationList == null)
        {
            throw new InvalidOperationException("Station list missing.");
        }
        if (MqttGraph == null)
        {
            throw new InvalidOperationException("Graph is missing.");
        }

        var parser = new MessageParsers.StationListParser(MqttStationList, MqttGraph);
        AgvGraph = parser.Graph;
    }
}