using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Domain.Repositories;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Events;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.MessageParsers;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Messages.Mqtt;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;
using IAgvRepository = Gebhardt.Acc.DeviceCommunication.Common.Repositories.IAgvRepository;

namespace Gebhardt.Acc.DeviceCommunication.KarisAgv.Services;

/// <summary>
/// Transform graph information from AGV into MassTransit message.
/// </summary>
[UsedImplicitly]
public class MqttGraphConsumer : IConsumer<MqttGraph>
{
    private readonly ILogger<MqttGraphConsumer> _logger;
    private readonly IAgvRepository _agvRepository;
    private readonly IGraphRepository _graphRepository;

    /// <summary>
    /// Create consumer with message handler delegate.
    /// </summary>
    /// <param name="logger">Mandatory logger facility.</param>
    /// <param name="agvRepository">The agv repository with AgvInformation.</param>
    /// <param name="graphRepository">The graph repository of GraphNodes/Edges with matching MqttNodes/Edges.</param>
    public MqttGraphConsumer(ILogger<MqttGraphConsumer> logger, IAgvRepository agvRepository, IGraphRepository graphRepository)
    {
        _logger = logger;
        _agvRepository = agvRepository;
        _graphRepository = graphRepository;
    }

    /// <summary>
    /// Consume graph message from AGV and transform this into <see cref="InternalGraphReceived"/> message.
    /// </summary>
    /// <exception cref="InvalidOperationException">Message content is invalid and cannot be processed.</exception>
    public async Task Consume(ConsumeContext<MqttGraph> context)
    {
        _logger.LogInformation("{MessageType} from AGV {AgvId} received", nameof(MqttGraph), context.Message.SerialNumber);
        var reversedYGraph = context.Message;
        var correlationId = context.Message.SerialNumber.ToGuid();

        //Special treatment because we are in a different coordinate system than Karis AGV
        if (reversedYGraph.Nodes != null)
        {
            foreach (var t in reversedYGraph.Nodes)
            {
                t.Y *= -1;
            }
        }

        if (reversedYGraph.Edges != null)
        {
            foreach (var e in reversedYGraph.Edges)
            {
                if (e.ControlPoints != null)
                {
                    foreach (var controlPoint in e.ControlPoints)
                    {
                        controlPoint.Y *= -1;
                    }
                }
            }
        }

        var graphParser = new GraphParser(reversedYGraph);
        graphParser.Parse();
        await _graphRepository.Add(graphParser.NodesById, graphParser.EdgesById);

        var agvGraph = graphParser.Graph;
        FeedAgvRepository(correlationId, agvGraph);

        await context.Publish(new InternalGraphReceived(correlationId, reversedYGraph), context.CancellationToken);
    }

    private void FeedAgvRepository(Guid correlationId, AgvGraph agvGraph)
    {
        _agvRepository.AddOrUpdateAgvGraph(correlationId, agvGraph);
    }
}