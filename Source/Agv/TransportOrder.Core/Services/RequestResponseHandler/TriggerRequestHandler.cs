using Gebhardt.Acc.Common.Domain.Exceptions;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Messages.Triggers;
using Gebhardt.Acc.Common.Messages.Triggers.Commands;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Models.Trigger;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Trigger;
using Gebhardt.Acc.TransportOrder.Messages.Events.Trigger;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Trigger;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;
using Trigger = Gebhardt.Acc.TransportOrder.Domain.Models.Trigger.Trigger;

namespace Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;

/// <summary>
/// Handles trigger-related requests.
/// </summary>
[UsedImplicitly]
public class TriggerRequestHandler(ITriggerRepository triggerRepository, IGraphRepository graphRepository, IMapRepository mapRepository, IBus bus, ILogger<TriggerRequestHandler> logger) :
    IConsumer<GetTriggers>,
    IConsumer<GetDeletedTriggers>,
    IConsumer<AssignTriggerToLocation>,
    IConsumer<TryRemoveTriggerLocation>,
    IConsumer<GetTrigger>,
    IConsumer<TryUpdateDefaultConfigurationParameter>,
    IConsumer<TryUpdateLocationConfigurationParameter>,
    IConsumer<TryResetLocationConfigurationParameter>,
    IConsumer<TryMarkTriggerForDeletion>,
    IConsumer<TryRestoreTrigger>,
    IConsumer<DuplicateTriggerLocation>,
    IConsumer<DuplicateTriggerLocations>
{
    /// <inheritdoc/>>
    public async Task Consume(ConsumeContext<GetTriggers> context)
    {
        ICollection<Trigger> triggers = await triggerRepository.GetTriggers();
        await context.RespondAsync(new GotTriggers(triggers));
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<GetDeletedTriggers> context)
    {
        ICollection<Trigger> triggers = await triggerRepository.GetDeletedTriggers();
        await context.RespondAsync(new GotDeletedTriggers(triggers));
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<AssignTriggerToLocation> context)
    {
        (var triggerId, var node, Guid? edgeId, ICollection<PolygonPoint> polygon, Guid? mapId, ICollection<ConfigurationParameter>? configurationParameters) = context.Message;

        await ValidateMessageContent(triggerId, node?.Id, edgeId, polygon, mapId, configurationParameters);

        IRequestClient<AddTriggerLocation>? requestClient = context.CreateRequestClient<AddTriggerLocation>(bus);

        Response<TriggerLocationAdded> response = await requestClient.GetResponse<TriggerLocationAdded>(new AddTriggerLocation(triggerId, node, edgeId, polygon, mapId, configurationParameters));
        string? exceptionMessage = response.Message.Exception;

        if (exceptionMessage != null)
        {
            logger.LogError(exceptionMessage);
            throw new TriggerAssignmentFailedException(exceptionMessage);
        }

        await context.RespondAsync(new TriggerLocationAssigned(response.Message.Location!, node));
    }

    private async Task ValidateMessageContent(Guid triggerId, Guid? nodeId, Guid? edgeId, ICollection<PolygonPoint> polygon, Guid? mapId, ICollection<ConfigurationParameter>? configurationParameters)
    {
        var trigger = await triggerRepository.GetById(triggerId);

        if (!configurationParameters.IsNullOrEmpty())
        {
            foreach (var parameter in configurationParameters!)
            {
                if (!trigger.ValidateConfigurationParameterValue(parameter.Name, parameter.Value))
                {
                    throw new TriggerAssignmentFailedException($"Parameter {parameter.Name}:{parameter.Value} is not valid");
                }
            }
        }

        if (nodeId == null && edgeId is null && polygon.Count == 0)
        {
            throw new TriggerAssignmentFailedException("No location set: edge id, node id, and polygon are null");
        }
        if (!OnlyOneLocationSet(nodeId, edgeId, polygon))
        {
            throw new TriggerAssignmentFailedException("More then one location set: only one of edge id, node id, or polygon must be provided");
        }

        if (nodeId != null)
        {
            try
            {
                await graphRepository.ThrowIfNodeNotExistsAsync(nodeId);
            }
            catch (NodeNotFoundException ex)
            {
                throw new ValidationException(ex.Message);
            }
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

    private static bool OnlyOneLocationSet(Guid? nodeId, Guid? edgeId, ICollection<PolygonPoint> polygon)
    {
        int count = 0;

        if (edgeId != null)
        {
            count++;
        }

        if (nodeId != null)
        {
            count++;
        }

        if (polygon.Count != 0)
        {
            count++;
        }

        return count == 1;
    }

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<GetTrigger> context)
    {
        var triggerId = context.Message.TriggerId;
        var trigger = await triggerRepository.GetById(triggerId);
        await context.RespondAsync(new GotTrigger(trigger));
    }

   /// <inheritdoc/>
    public async Task Consume(ConsumeContext<TryMarkTriggerForDeletion> context)
    {
        var triggerId = context.Message.TriggerId;
        await triggerRepository.GetById(triggerId);
        await bus.Publish(new MarkTriggerForDeletion(triggerId));
        await context.RespondAsync(new Success());
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<TryRestoreTrigger> context)
    {
        var triggerId = context.Message.TriggerId;
        var trigger = await triggerRepository.GetById(triggerId, includeDeleted: true);
        if (!trigger.IsDeleted)
        {
            throw new TriggerNotDeletedException(triggerId);
        }
        await bus.Publish(new RestoreTrigger(triggerId));
        await context.RespondAsync(new Success());
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<TryUpdateDefaultConfigurationParameter> context)
    {
        (var triggerId, string parameterName, string newValue) = context.Message;

        var trigger = await triggerRepository.GetById(triggerId);
        if (!trigger.ValidateConfigurationParameterValue(parameterName, newValue))
        {
            throw new TriggerConfigurationUpdateFailedException($"Value {newValue} is not valid");
        }

        await bus.Publish(new UpdateDefaultConfigurationParameter(triggerId, parameterName, newValue));
        await context.RespondAsync(new Success());
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<TryUpdateLocationConfigurationParameter> context)
    {
        (var triggerId, var locationId, string parameterName, string newValue) = context.Message;
        var trigger = await triggerRepository.GetById(triggerId);
        if (!trigger.ValidateConfigurationParameterValue(parameterName, newValue))
        {
            throw new TriggerConfigurationUpdateFailedException($"Value {newValue} is not valid");
        }
        if (trigger.Locations.All(l => l.Id != locationId))
        {
            throw new TriggerLocationNotFoundException(locationId);
        }
        await bus.Publish(new UpdateLocationConfigurationParameter(triggerId, locationId, parameterName, newValue));
        await context.RespondAsync(new Success());
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<TryResetLocationConfigurationParameter> context)
    {
        (var triggerId, var locationId, string parameterName) = context.Message;
        var trigger = await triggerRepository.GetById(triggerId);
        if (trigger.Locations.All(l => l.Id != locationId))
        {
            throw new TriggerLocationNotFoundException(locationId);
        }
        await bus.Publish(new ResetLocationConfigurationParameter(triggerId, locationId, parameterName));
        await context.RespondAsync(new Success());
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<DuplicateTriggerLocation> context)
    {
        var (triggerId, targetTriggerId, locationId) = context.Message;

        var trigger = await triggerRepository.GetById(triggerId);
        var location = trigger.Locations.Single(l => l.Id == locationId);
        await RequestAddingLocation(context, targetTriggerId, location);
        await context.RespondAsync(new Success());
    }

    private async Task RequestAddingLocation<T>(ConsumeContext<T> context, Guid targetTriggerId, Location location)
    where T: class
    {
        IRequestClient<AddTriggerLocation>? requestClient = context.CreateRequestClient<AddTriggerLocation>(bus);

        Response<TriggerLocationAdded> response = await requestClient.GetResponse<TriggerLocationAdded>(new AddTriggerLocation(targetTriggerId, location.Node, location.EdgeId, location.Polygon.OrderBy(p => p.Index).Select(p => new PolygonPoint(p.X, p.Y)).ToList(), location.MapId, null));
        string? exceptionMessage = response.Message.Exception;

        if (exceptionMessage != null)
        {
            logger.LogError(exceptionMessage);
            throw new TriggerAssignmentFailedException(exceptionMessage);
        }
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<DuplicateTriggerLocations> context)
    {
        var (triggerId, targetTriggerId) = context.Message;

        var trigger = await triggerRepository.GetById(triggerId);
        IEnumerable<Task> addingTasks = trigger.Locations.Select(l => RequestAddingLocation(context, targetTriggerId, l));
        await Task.WhenAll(addingTasks);
        await context.RespondAsync(new Success());
    }
}