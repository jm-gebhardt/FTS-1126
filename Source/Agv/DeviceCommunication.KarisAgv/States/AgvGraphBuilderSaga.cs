using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Messages.AgvManagement.Commands;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.MessageParsers;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Messages.Mqtt;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Services;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.DeviceCommunication.KarisAgv.States;

/// <summary>
/// Saga instance for <see cref="AgvGraphBuilderStateMachine"/>
/// </summary>
public class AgvGraphBuilderSaga : SagaStateMachineInstance, CorrelatedBy<Guid>
{
    /// <summary>
    /// Correlation ID taken from the initial request "<see cref="RequestAgvGraph"/>".
    /// <para>
    /// See <see href="https://masstransit-project.com//usage/correlation.html" />
    /// </para></summary>
    public Guid CorrelationId { get; set; }

    /// <summary>
    /// Current state of this state machine instance.
    /// </summary>
    [UsedImplicitly]
    public State CurrentState { get; set; } = default!;

    /// <summary>
    /// Request ID of a pending MQTT request.
    /// <para>Used to correlate MQTT response.</para>
    /// </summary>
    public Guid CurrentMqttRequestId { get; set; }

    /// <summary>
    /// Station list.
    /// </summary>
    public MqttStationList? MqttStationList { get; set; }

    /// <summary>
    /// Graph.
    /// </summary>
    public MqttGraph? MqttGraph { get; set; }

    /// <summary>
    /// Merged graph.
    /// </summary>
    public AgvGraph? AgvGraph { get; private set; }

    /// <summary>
    /// The name of the related map.
    /// </summary>
    public string? RelatedMapName { get; private set; }

    /// <summary>
    /// The Id of the related map.
    /// </summary>
    public Guid RelatedMapId { get; private set; }

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
    /// Merge graph and station list into an <see cref="Acc.Common.Domain.Models.Graphs.AgvGraph"/>.
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

        var parser = new StationListParser(MqttStationList, MqttGraph);
        AgvGraph = parser.Graph;
        RelatedMapId = parser.RelatedMapId;
        RelatedMapName = parser.RelatedMapName;
    }
}