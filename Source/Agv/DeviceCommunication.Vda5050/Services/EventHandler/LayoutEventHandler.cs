using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Agvs;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Repositories;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Commands.Graph;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Events;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Services.EventHandler;

/// <summary>
/// Handles layout-related events.
/// </summary>
[UsedImplicitly]
public class LayoutEventHandler(IAgvRepository agvRepository, ILogger<LayoutEventHandler> logger) : 
    IConsumer<LayoutStored>
{
    /// <inheritdoc />
    public async Task Consume(ConsumeContext<LayoutStored> context)
    {
        IReadOnlyCollection<Agv> allAgvs = await agvRepository.GetAllAsync();
        var unmatchedVehicleTypeIds = new List<string>();
        ICollection<Task> publishTasks = [];
        foreach (string vehicleTypeId in context.Message.VehicleTypeIds)
        {
            List<Agv> agvs = allAgvs.Where(a => a.VehicleTypeId == vehicleTypeId).ToList();
            if (agvs.Count == 0)
            {
                unmatchedVehicleTypeIds.Add(vehicleTypeId);
                continue;
            }
            foreach (var agv in agvs)
            {
                logger.LogInformation("Publishing graph for AGV {AgvSerialNumber}/{AgvCorrelationId} (vehicleTypeId: {VehicleTypeId})",
                    agv.SerialNumber, agv.CorrelationId, vehicleTypeId);
                publishTasks.Add(context.Publish(new PublishAgvGraph(agv.CorrelationId, agv.SerialNumber, vehicleTypeId)));
            }
        }

        await Task.WhenAll(publishTasks);

        if (unmatchedVehicleTypeIds.Count > 0)
        {
            logger.LogInformation("Unmatched vehicle types: {VehicleTypeIds}. Agv graphs will be published when matching AGVs register.",
                string.Join(", ", unmatchedVehicleTypeIds));
        }
    }
}