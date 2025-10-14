using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.Messages.Stations.Commands;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Messages.Mqtt;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.DeviceCommunication.KarisAgv.Services;

/// <summary>
/// Handles station related commands.
/// </summary>
[UsedImplicitly]
public class AgvStationConsumer(ILogger<AgvStationConsumer> logger) : IConsumer<RemoveAgvFromStation>
{
    /// <summary>
    /// Handles the <see cref="RemoveAgvFromStation"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<RemoveAgvFromStation> context)
    {
        (string stationId, string agvToRemove, string actingAgv) = context.Message;

        logger.LogInformation("Sending request to remove agv {AgvToRemove} from station {StationId} to agv {ActingAgv}", agvToRemove, stationId, actingAgv);

        await context.PublishMqtt(new MqttRequestStationRemoveAgv(actingAgv, Guid.NewGuid(), agvToRemove, stationId), context.CancellationToken);
    }
}