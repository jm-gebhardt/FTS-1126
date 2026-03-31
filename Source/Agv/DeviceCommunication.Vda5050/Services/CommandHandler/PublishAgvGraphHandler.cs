using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.DeviceCommunication.Common.Messages;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Services.Graphs;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Commands.Graph;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Services.CommandHandler;

/// <summary>
/// Handles <see cref="PublishAgvGraph"/> commands.
/// Generates a domain AgvGraph on-the-fly from stored layout data and publishes it.
/// </summary>
[UsedImplicitly]
public class PublishAgvGraphHandler(
    IAgvGraphProvider agvGraphProvider,
    ILifGraphConverter lifGraphConverter,
    TimeProvider timeProvider,
    ILogger<PublishAgvGraphHandler> logger) : IConsumer<PublishAgvGraph>
{
    /// <inheritdoc />
    public async Task Consume(ConsumeContext<PublishAgvGraph> context)
    {
        var (agvCorrelationId, serialNumber, vehicleTypeId) = context.Message;

        logger.LogDebug("Generating graph for AGV {AgvSerialNumber}/{AgvCorrelationId} (vehicleTypeId: {VehicleTypeId})",
            serialNumber, agvCorrelationId, vehicleTypeId);

        var domainGraph = await agvGraphProvider.GetAgvGraphAsync(agvCorrelationId);
        if (domainGraph == null)
        {
            logger.LogDebug("No matching layout data found for AGV {AgvSerialNumber}/{AgvCorrelationId} (vehicleTypeId: {VehicleTypeId}), skipping",
                serialNumber, agvCorrelationId, vehicleTypeId);
            return;
        }

        var commonGraph = lifGraphConverter.ConvertToCommonAgvGraph(domainGraph);

        logger.LogInformation("Publishing graph for AGV {AgvSerialNumber}/{AgvCorrelationId}", serialNumber, agvCorrelationId);
        await context.Publish(new InternalAgvGraphReceived(agvCorrelationId, serialNumber, commonGraph));

        foreach (var map in MapBuilder.BuildDummyMaps(domainGraph, commonGraph, timeProvider))
        {
            await context.Publish(new AgvMapReceived(agvCorrelationId, serialNumber, map));
        }
    }
}