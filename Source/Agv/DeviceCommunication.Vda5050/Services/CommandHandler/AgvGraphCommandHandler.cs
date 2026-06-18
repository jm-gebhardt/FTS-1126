using Gebhardt.Acc.Common.Domain.Services;
using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Repositories;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Extensions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Lif;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Services.Graphs;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Commands.Graph;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;
using Gebhardt.Acc.Common.Messages.Maps;
using Gebhardt.Acc.Common.Domain.Models.Graphs;

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
            Response<GotDefaultMap, NoDefaultMap> response = await requestClient.GetResponse<GotDefaultMap, NoDefaultMap>(new GetDefaultMap(), context.CancellationToken);
            if (response.Is(out Response<NoDefaultMap>? _))
            {
                map = MapBuilder.BuildDummyMap("map", graph, timeProvider);
                await context.Publish(new AgvMapReceived(agvCorrelationId, serialNumber, map));
                return;
            }
            if (response.Is(out Response<GotDefaultMap>? defaultMapResponse))
            {
                var defaultMap = defaultMapResponse.Message;
                map = MapBuilder.BuildDummyMap(defaultMap.MapName, graph, timeProvider, defaultMap.MinX, defaultMap.MaxX, defaultMap.MinY, defaultMap.MaxY);
                await context.Publish(new AgvMapReceived(agvCorrelationId, serialNumber, map));
            }
        }
        catch (Exception)
        {
            map = MapBuilder.BuildDummyMap("map", graph, timeProvider);
            await context.Publish(new AgvMapReceived(agvCorrelationId, serialNumber, map));
        }
    }
}