using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Events;
using Gebhardt.StoreWare.Agv.DeviceCommunication.MessageParsers;
using Gebhardt.StoreWare.Agv.DeviceCommunication.MqttMessages;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.DeviceCommunication.Services;

/// <summary>
/// Transform graph information from AGV into MassTransit message.
/// </summary>
[UsedImplicitly]
public class AgvGraphConsumer : IConsumer<MqttGraph>
{
    private readonly ILogger<AgvGraphConsumer> _logger;
    private readonly IAgvRegistry _agvRegistry;


    /// <summary>
    /// Create consumer with message handler delegate.
    /// </summary>
    /// <param name="logger">Mandatory logger facility.</param>
    /// <param name="agvRegistry"></param>
    public AgvGraphConsumer(ILogger<AgvGraphConsumer> logger, IAgvRegistry agvRegistry)
    {
        _logger = logger;
        _agvRegistry = agvRegistry;
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

        await context.Publish(new InternalGraphReceived(
            correlationId: correlationId,
            graph: reversedYGraph
        ), context.CancellationToken);

        var graphParser = new GraphParser(reversedYGraph);
        var agvGraph = graphParser.Graph;
        FeedAgvRegistry(correlationId, agvGraph);
    }

    private void FeedAgvRegistry(Guid correlationId, AgvGraph agvGraph)
    {
        _agvRegistry.AddOrUpdateAgvGraph(correlationId, agvGraph);
    }
}