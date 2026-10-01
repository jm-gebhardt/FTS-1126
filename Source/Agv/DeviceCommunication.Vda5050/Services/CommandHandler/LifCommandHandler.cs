using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Messages.NewGraph;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.LIF.LifToGraphConversion;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Repositories;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Commands.Lif;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Requests.Lif;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Services.CommandHandler;

/// <summary>
/// Handles <see cref="StoreLif"/> commands.
/// Persists graph layouts and emits <see cref="StoreLif"/> events.
/// </summary>
[UsedImplicitly]
public class LifCommandHandler(
    ILifRepository lifRepository,
    IAgvTypeRepository agvTypeRepository,
    ILogger<LifCommandHandler> logger) :
    IConsumer<StoreLif>
{
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

        var graphSegment = LifToGraphConverter.ToGraphSegment(layoutCollection, agvTypeIdentifiers, logger);

        await context.Publish(new NewGraphSegmentReceived(context.Message.CorrelationId, graphSegment));

        await context.RespondAsync(new Success());
    }
}