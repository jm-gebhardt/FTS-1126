using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Services;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Messages.Graphs.Requests;
using Gebhardt.Acc.Common.Messages.NewGraph;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Extensions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Lif;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.LIF.LifToGraphConversion;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Repositories;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Commands.Lif;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Events;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Requests.Lif;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Services.CommandHandler;

/// <summary>
/// Handles <see cref="StoreLayouts"/> commands.
/// Persists graph layouts and emits <see cref="LayoutStored"/> events.
/// </summary>
[UsedImplicitly]
public class LifCommandHandler(
    ILifRepository lifRepository,
    IAgvTypeRepository agvTypeRepository,
    ILogger<LifCommandHandler> logger,
    INurbsEdgeBuilder edgeBuilder) :
    IConsumer<StoreLayouts>,
    IConsumer<StoreLif>

{
    /// <inheritdoc />
    public async Task Consume(ConsumeContext<StoreLayouts> context)
    {
        ICollection<Layout> layouts = context.Message.Layouts;

        logger.LogInformation("Storing {LayoutCount} lif layouts", layouts.Count);

        HashSet<string> vehicleTypeIds = layouts.SelectMany(l => l.GetSupportedVehicleTypes()).Distinct().ToHashSet();
        List<Layout> replaced = [];
        List<Layout> added = [];
        List<Graph> graphs = [];
        (ICollection<LifFile> uploadedFiles, ICollection<Layout> uploadedLayouts) = await lifRepository.GetLifsByVehicleTypeIdsAsync(vehicleTypeIds.ToArray());
        foreach (string vehicleTypeId in vehicleTypeIds)
        {
            List<Layout> newLayoutsForVehicle = layouts.Where(l => l.GetSupportedVehicleTypes().Contains(vehicleTypeId)).ToList();
            List<LifFile> files = uploadedFiles.Where(f => f.SupportedVehicleTypes.Contains(vehicleTypeId)).ToList();
            if (files.Count == 0)
            {
                added.AddRange(newLayoutsForVehicle);
                graphs.AddRange(newLayoutsForVehicle.ToGraph(vehicleTypeId, edgeBuilder));
                replaced.AddRange(uploadedLayouts.Where(l => l.GetSupportedVehicleTypes().Contains(vehicleTypeId)));
            }
        }
        await lifRepository.UpdateLayouts(added, replaced);
        ICollection<string> layoutIds = layouts.Select(l => l.LayoutId).Distinct().ToList();

        logger.LogInformation("Lif layouts stored. LayoutIds: [{LayoutIds}], VehicleTypeIds: [{VehicleTypeIds}]",
            string.Join(", ", layoutIds), string.Join(", ", vehicleTypeIds));

        var layoutStored = new LayoutStored(layoutIds, vehicleTypeIds.ToList());
        await context.Publish(new AgvTypeGraphUpdated(graphs));
        await context.Publish(layoutStored);
        await context.RespondAsync(layoutStored);
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<StoreLif> context)
    {
        var (lifFile, layoutCollection) = context.Message;

        logger.LogInformation("Storing new layout collection");

        try
        {
            await lifRepository.AddFileAsync(lifFile, layoutCollection.Layouts, []);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not store lif {FileName}", lifFile.MetaInformation.Filename);
            await context.RespondAsync(new LifStoringFailed());

            return;
        }

        logger.LogInformation("Lif file {Filename} stored", lifFile.MetaInformation.Filename);
        ICollection<string> layoutIds = layoutCollection.Layouts.Select(l => l.LayoutId).Distinct().ToList();
        ICollection<string> vehicleTypeIds = layoutCollection.Layouts.SelectMany(l => l.GetSupportedVehicleTypes()).Distinct().ToList();

        logger.LogInformation("Lif layouts stored. LayoutIds: [{LayoutIds}], VehicleTypeIds: [{VehicleTypeIds}]", string.Join(", ", layoutIds), string.Join(", ", vehicleTypeIds));

        Dictionary<string, Guid> agvTypeIdentifiers = await agvTypeRepository.GetAgvTypeIdentifiersAsync();

        var graph = LifToGraphConverter.ToGraph(layoutCollection, agvTypeIdentifiers, logger);

        var layoutStored = new LayoutStored(layoutIds, vehicleTypeIds.ToList());
        await context.Publish(layoutStored);

        await context.Publish(new NewGraphReceived(context.Message.CorrelationId, graph));

        await context.RespondAsync(new Success());
    }
}