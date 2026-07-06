using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Services;
using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.Common.Messages.Maps;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Extensions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Lif;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Repositories;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Services.Graphs;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Commands.Graph;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Services.CommandHandler;

/// <summary>
/// Handles agv graph related commands.
/// </summary>
[UsedImplicitly]
public class AgvGraphCommandHandler(IBus bus, ILifRepository lifRepository, TimeProvider timeProvider, ILogger<AgvGraphCommandHandler> logger, INurbsEdgeBuilder edgeBuilder) : 
    IConsumer<PublishAgvGraph>
{
    /// <inheritdoc />
    public async Task Consume(ConsumeContext<PublishAgvGraph> context)
    {
        var (agvCorrelationId, serialNumber, vehicleTypeId) = context.Message;

        logger.LogDebug("Generating graph for AGV {AgvSerialNumber}/{AgvCorrelationId} (vehicleTypeId: {VehicleTypeId})",
            serialNumber, agvCorrelationId, vehicleTypeId);

        ICollection<Layout> layouts = await lifRepository.GetLayoutsByVehicleTypeIdAsync(vehicleTypeId);

        if (layouts.Count == 0)
        {
            logger.LogDebug("No matching layout data found for AGV {AgvSerialNumber}/{AgvCorrelationId} (vehicleTypeId: {VehicleTypeId}), skipping",
                serialNumber, agvCorrelationId, vehicleTypeId);
            return;
        }

        var graph = layouts.ToAgvGraph(agvCorrelationId, vehicleTypeId, edgeBuilder);

        logger.LogInformation("Publishing graph for AGV {AgvSerialNumber}/{AgvCorrelationId}", serialNumber, agvCorrelationId);
        await context.Publish(new AgvGraphReceived(agvCorrelationId, serialNumber, graph), context.CancellationToken);

        await BuildMap(context, agvCorrelationId, serialNumber, graph);
    }

    private async Task BuildMap(ConsumeContext<PublishAgvGraph> context, Guid agvCorrelationId, string serialNumber, AgvGraph graph)
    {
        IRequestClient<GetDefaultMap> requestClient = context.CreateRequestClient<GetDefaultMap>(bus);
        Map map;
        try
        {
            Response<GotDefaultMap> response = await requestClient.GetResponse<GotDefaultMap>(new GetDefaultMap(), context.CancellationToken);
            map = response.Message.Map.Extend(graph, timeProvider);
        }
        catch (Exception ex) when (ex is RequestTimeoutException or RequestFaultException)
        {
            logger.LogWarning(ex, "Could not get default map; falling back to locally generated map for AGV {AgvCorrelationId}", agvCorrelationId);
            map = MapBuilder.BuildDummyMap("map", graph, timeProvider);
        }
        await bus.Publish(new AgvMapReceived(agvCorrelationId, serialNumber, map));
    }
}