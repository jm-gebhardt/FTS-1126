using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Services;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Messages.Graphs.Requests;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Extensions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Lif;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Repositories;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Commands.Lif;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Events;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Requests.Lif;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Services.CommandHandler;

/// <summary>
/// Handles <see cref="StoreLayouts"/> commands.
/// Persists graph layouts and emits <see cref="LayoutStored"/> events.
/// </summary>
[UsedImplicitly]
public class LifCommandHandler(
    ILifRepository lifRepository,
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

        HashSet<string> vehicleTypeIds = layoutCollection.Layouts.SelectMany(l => l.GetSupportedVehicleTypes()).Distinct().ToHashSet();
        List<Layout> replaced = [];
        List<Layout> added = [];
        (ICollection<LifFile> uploadedFiles, ICollection<Layout> uploadedLayouts) = await lifRepository.GetLifsByVehicleTypeIdsAsync(vehicleTypeIds.ToArray());

        List<Graph> graphs = [];
        foreach (string vehicleTypeId in vehicleTypeIds)
        {
            List<Layout> newLayoutsForVehicle = layoutCollection.Layouts.Where(l => l.GetSupportedVehicleTypes().Contains(vehicleTypeId)).ToList();
            List<Layout> uploadedLayoutsForVehicle = uploadedLayouts.Where(l => l.GetSupportedVehicleTypes().Contains(vehicleTypeId)).ToList();
            List<LifFile> files = uploadedFiles.Where(f => f.SupportedVehicleTypes.Contains(vehicleTypeId)).ToList();
            if (files.Count == 0)
            {
                added.AddRange(newLayoutsForVehicle);
                graphs.AddRange(newLayoutsForVehicle.ToGraph(vehicleTypeId, edgeBuilder));
                replaced.AddRange(uploadedLayoutsForVehicle);
                continue;
            }
            foreach (var file in files)
            {
                if (file.MetaInformation.UploadTimestamp >= lifFile.MetaInformation.UploadTimestamp)
                {
                    continue;
                }
                added.AddRange(newLayoutsForVehicle);
                graphs.AddRange(newLayoutsForVehicle.ToGraph(vehicleTypeId, edgeBuilder));
                replaced.AddRange(uploadedLayoutsForVehicle);
            }
        }

        try
        {
            if (await lifRepository.AddFileAsync(lifFile, added, replaced))
            {
                logger.LogInformation("Lif file {Filename} stored", lifFile.MetaInformation.Filename);
                ICollection<string> layoutIds = layoutCollection.Layouts.Select(l => l.LayoutId).Distinct().ToList();

                logger.LogInformation("Lif layouts stored. LayoutIds: [{LayoutIds}], VehicleTypeIds: [{VehicleTypeIds}]",
                    string.Join(", ", layoutIds), string.Join(", ", vehicleTypeIds));

                var layoutStored = new LayoutStored(layoutIds, vehicleTypeIds.ToList());
                await context.Publish(layoutStored);

                await context.Publish(new AgvTypeGraphUpdated(graphs));
                await context.RespondAsync(new Success());
            }
            else
            {
                logger.LogWarning("Could not store lif {FileName}", lifFile.MetaInformation.Filename);
                await context.RespondAsync(new LifStoringFailed());
            }
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex.Message);
            await context.RespondAsync(new LifStoringFailed());
        }
    }
}