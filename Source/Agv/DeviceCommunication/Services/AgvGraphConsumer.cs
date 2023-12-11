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
    private readonly IAgvRepository _agvRepository;

    /// <summary>
    /// Create consumer with message handler delegate.
    /// </summary>
    /// <param name="logger">Mandatory logger facility.</param>
    /// <param name="agvRepository"></param>
    public AgvGraphConsumer(ILogger<AgvGraphConsumer> logger, IAgvRepository agvRepository)
    {
        _logger = logger;
        _agvRepository = agvRepository;
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

        var graphParser = new GraphParser(reversedYGraph);
        var agvGraph = graphParser.Graph;
        FeedAgvRepository(correlationId, agvGraph);

        await context.Publish(new InternalGraphReceived(
            correlationId,
            reversedYGraph
        ), context.CancellationToken);
    }

    private void FeedAgvRepository(Guid correlationId, AgvGraph agvGraph)
    {
        _agvRepository.AddOrUpdateAgvGraph(correlationId, agvGraph);
    }
}