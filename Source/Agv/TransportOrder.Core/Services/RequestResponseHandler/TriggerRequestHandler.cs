using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Models.Polygon;
using Gebhardt.Acc.TransportOrder.Domain.Models.Trigger;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Trigger;
using Gebhardt.Acc.TransportOrder.Messages.Events.Trigger;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Trigger;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;

/// <summary>
/// Handles trigger-related requests.
/// </summary>
[UsedImplicitly]
public class TriggerRequestHandler(ITriggerRepository triggerRepository, IGraphRepository graphRepository, IMapRepository mapRepository, IBus bus, ILogger<TriggerRequestHandler> logger):
    IConsumer<GetTriggers>,
    IConsumer<AssignTriggerToLocation>,
    IConsumer<TryRemoveTriggerLocation>,
    IConsumer<GetTrigger>,
    IConsumer<TryUpdateConfigurationParameter>
{
    /// <summary>
    /// Handles the <see cref="GetTriggers"/> request.
    /// </summary>
    public async Task Consume(ConsumeContext<GetTriggers> context)
    {
        ICollection<Trigger> triggers = await triggerRepository.GetTriggers();
        await context.RespondAsync(new GotTriggers(triggers));
    }

    /// <summary>
    /// Handles the <see cref="AssignTriggerToLocation"/> request.
    /// </summary>
    public async Task Consume(ConsumeContext<AssignTriggerToLocation> context)
    {
        (var triggerId, Guid? edgeId, string? nodeId, ICollection<PolygonPoint> polygon, Guid? mapId) = context.Message;

        await ValidateMessageContent(triggerId, edgeId, nodeId, polygon, mapId);

        IRequestClient<AddTriggerLocation>? requestClient = context.CreateRequestClient<AddTriggerLocation>(bus);
        
        Response<TriggerLocationAdded> response =  await requestClient.GetResponse<TriggerLocationAdded>(new AddTriggerLocation(triggerId, edgeId, nodeId, polygon, mapId));

        if (response.Message.Exception != null)
        {
            logger.LogError(response.Message.Exception);
            throw new TriggerAssignmentFailedException(response.Message.Exception);
        }

        await context.RespondAsync(new TriggerLocationAssigned(response.Message.Location!));
    }

    private async Task ValidateMessageContent(Guid triggerId, Guid? edgeId, string? nodeId, ICollection<PolygonPoint> polygon, Guid? mapId)
    {
        await triggerRepository.GetById(triggerId);

        if (!OnlyOneLocationSet(edgeId, nodeId, polygon))
        {
            throw new TriggerAssignmentFailedException("More then one location set: only one of edge id, node id, or polygon must be provided");
        }

        if (nodeId != null && !await graphRepository.NodeExistsAsync(nodeId))
        {
            throw new NodeNotFoundException(nodeId);
        }

        if (edgeId != null && !await graphRepository.EdgeExistsAsync(edgeId.Value))
        {
            throw new TriggerAssignmentFailedException($"Edge {edgeId} does not exist");
        }

        if (mapId != null && !mapRepository.Exists(mapId.Value))
        {
            throw new TriggerAssignmentFailedException($"Map {mapId} does not exist");
        }
    }
    
    private static bool OnlyOneLocationSet(Guid? edgeId, string? nodeId, ICollection<PolygonPoint> polygon)
    {
        int count = 0;

        if (edgeId != null)
        {
            count++;
        }

        if (!string.IsNullOrEmpty(nodeId))
        {
            count++;
        }

        if (polygon.Any())
        {
            count++;
        }

        return count == 1;
    }

    /// <summary>
    /// Handles the <see cref="TryRemoveTriggerLocation"/> request.
    /// </summary>
    public async Task Consume(ConsumeContext<TryRemoveTriggerLocation> context)
    {
        var triggerId = context.Message.TriggerId;
        var locationId = context.Message.LocationId;

        var trigger = await triggerRepository.GetById(triggerId);

        if (trigger.Locations.All(l => l.Id != locationId))
        {
            throw new TriggerLocationNotFoundException(locationId);
        }

        await bus.Publish(new RemoveTriggerLocation(triggerId, locationId));
        await context.RespondAsync(new Success());
    }

    /// <summary>
    /// Handles the <see cref="GetTrigger"/> request.
    /// </summary>
    public async Task Consume(ConsumeContext<GetTrigger> context)
    {
        var triggerId = context.Message.TriggerId;
        var trigger = await triggerRepository.GetById(triggerId);
        await context.RespondAsync(new GotTrigger(trigger));
    }

    /// <summary>
    /// Handles the <see cref="TryUpdateConfigurationParameter"/> request.
    /// </summary>
    public async Task Consume(ConsumeContext<TryUpdateConfigurationParameter> context)
    {
        (var triggerId, string parameterName, string newValue) = context.Message;
       
        var trigger = await triggerRepository.GetById(triggerId);
        if (!trigger.ValidateConfigurationParameterValue(parameterName, newValue))
        {
            throw new TriggerConfigurationUpdateFailedException($"Value {newValue} is not valid");
        }

        await bus.Publish(new UpdateConfigurationParameter(triggerId, parameterName, newValue));
        await context.RespondAsync(new Success());
    }
}