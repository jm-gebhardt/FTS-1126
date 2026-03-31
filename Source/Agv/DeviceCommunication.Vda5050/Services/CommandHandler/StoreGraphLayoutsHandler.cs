using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Graphs;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Repositories;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Extensions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Commands.Graph;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Events;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Services.CommandHandler;

/// <summary>
/// Handles <see cref="StoreGraphLayouts"/> commands.
/// Persists graph layouts and emits <see cref="GraphLayoutStored"/> events.
/// </summary>
[UsedImplicitly]
public class StoreGraphLayoutsHandler(
    IGraphLayoutRepository graphLayoutRepository,
    ILogger<StoreGraphLayoutsHandler> logger) : IConsumer<StoreGraphLayouts>
{
    /// <inheritdoc />
    public async Task Consume(ConsumeContext<StoreGraphLayouts> context)
    {
        ICollection<GraphLayout> layouts = context.Message.Layouts;

        logger.LogInformation("Storing {LayoutCount} graph layouts", layouts.Count);
        await graphLayoutRepository.StoreGraphLayoutsAsync(layouts);

        HashSet<string> vehicleTypeIds = layouts.GetVehicleTypeIds();
        ICollection<string> layoutIds = layouts.Select(l => l.LayoutId).Distinct().ToList();

        logger.LogInformation("Graph layouts stored. LayoutIds: [{LayoutIds}], VehicleTypeIds: [{VehicleTypeIds}]",
            string.Join(", ", layoutIds), string.Join(", ", vehicleTypeIds));

        var graphLayoutStored = new GraphLayoutStored(layoutIds, vehicleTypeIds.ToList());

        await context.Publish(graphLayoutStored);
        await context.RespondAsync(graphLayoutStored);
    }
}