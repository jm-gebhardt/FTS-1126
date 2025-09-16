using AutoMapper;
using Gebhardt.Acc.Common.Messages.Graphs.Requests;
using Gebhardt.Acc.Common.Messages.Triggers;
using Gebhardt.Acc.Common.Messages.Triggers.Commands;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Models.Trigger;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Trigger;
using Gebhardt.Acc.TransportOrder.Messages.Events.Trigger;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;
using PolygonPoint = Gebhardt.Acc.TransportOrder.Domain.Models.Polygon.PolygonPoint;

namespace Gebhardt.Acc.TransportOrder.Core.Services.CommandHandler;

/// <summary>
/// Provides handlers for trigger-related commands.
/// </summary>
[UsedImplicitly]
public class TriggerCommandHandler(ITriggerRepository triggerRepository, IMapper mapper, ILogger<TriggerCommandHandler> logger) :
    IConsumer<CreateTrigger>,
    IConsumer<AddTriggerLocation>,
    IConsumer<RemoveTriggerLocation>,
    IConsumer<UpdateDefaultConfigurationParameter>,
    IConsumer<UpdateLocationConfigurationParameter>,
    IConsumer<ResetLocationConfigurationParameter>,
    IConsumer<GetTriggerMapping>
{
    /// <summary>
    /// Handles the <see cref="CreateTrigger"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<CreateTrigger> context)
    {
        ICollection<ConfigurationParameter> configurationParameters = context.Message.ConfigurationParameters;
        var properties = context.Message.Properties;
        Trigger? existingTrigger = null;
        try
        {
            existingTrigger = await triggerRepository.GetById(properties.Id);
        }
        catch (TriggerNotFoundException)
        {
            // pass
        }
        var trigger = new Trigger(properties.Id, properties.Urn, properties.DisplayName, properties.Version, properties.Description, properties.InstanceMaximum, configurationParameters);

        await triggerRepository.AddOrUpdate(trigger, false);

        logger.LogInformation("Trigger {Name} created or updated", trigger.DisplayName);

        if (!trigger.Equals(existingTrigger))
        {
            // get trigger with locations
            trigger = await triggerRepository.GetById(properties.Id);
            await context.Publish(new TriggerChanged(trigger));
        }
    }

    /// <summary>
    /// Handles the <see cref="AddTriggerLocation"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<AddTriggerLocation> context)
    {
        Guid? edgeId = context.Message.EdgeId;
        var node = context.Message.Node;
        var triggerId = context.Message.TriggerId;
        Guid? mapId = context.Message.MapId;
        ICollection<ConfigurationParameter>? configurationParameters = context.Message.ConfigurationParameters;

        ICollection<PolygonPoint> polygon = mapper.Map<List<PolygonPoint>>(context.Message.Polygon);

        Location? location = null;
        Exception? exception = null;
        Trigger trigger = null!;

        try
        {
            trigger = await triggerRepository.GetById(triggerId);
            if (edgeId != null)
            {
                location = trigger.AddLocation(null, edgeId, mapId, configurationParameters);
            }
            if (node != null)
            {
                location = trigger.AddLocation(node, null, mapId, configurationParameters);
            }
            if (polygon.Count > 0)
            {
                location = trigger.AddLocation(polygon, mapId, configurationParameters);
            }
            await triggerRepository.Update(trigger, true);
        }
        catch (Exception ex)
        {
            exception = ex;
        }
        await context.RespondAsync(new TriggerLocationAdded(location, exception?.Message));

        if (exception == null)
        {
            logger.LogInformation("Trigger {Name} assigned to location {Location}", trigger.DisplayName, location!.Id);

            if (node != null)
            {
                await context.Publish(new TriggerAssignedToNode(triggerId, trigger.DisplayName, node.Id));
            }
            if (polygon.Count > 0)
            {
                await context.Publish(new TriggerAssignedToArea(triggerId, trigger.DisplayName, location.Id));
                logger.LogDebug("Location {Location} with polygon: {Polygon}", location.Id, location.Polygon.OrderBy(p => p.Index).Select(p => $"({p.X}, {p.Y})"));
            }

            await context.Publish(new TriggerChanged(trigger));
        }
    }

    /// <summary>
    /// Handles the <see cref="RemoveTriggerLocation"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<RemoveTriggerLocation> context)
    {
        var triggerId = context.Message.TriggerId;
        var locationId = context.Message.LocationId;

        var trigger = await triggerRepository.GetById(triggerId);
        trigger.RemoveLocation(locationId);
        await triggerRepository.Update(trigger, true);
        logger.LogDebug("Trigger {Name} removed from location {LocationId}", trigger.DisplayName, locationId);
        await context.Publish(new TriggerChanged(trigger));
    }

    /// <summary>
    /// Handles the <see cref="UpdateDefaultConfigurationParameter"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<UpdateDefaultConfigurationParameter> context)
    {
        var triggerId = context.Message.TriggerId;
        string parameterName = context.Message.ParameterName;
        string newValue = context.Message.NewValue;

        var trigger = await triggerRepository.GetById(triggerId);
        var updatedParameter = trigger.UpdateDefaultConfigurationParameter(parameterName, newValue);
        if (updatedParameter != null)
        {
            await triggerRepository.Update(trigger, true);
            await context.Publish(new DefaultTriggerConfigurationUpdated(triggerId, [updatedParameter]));
            logger.LogInformation("Trigger {Name} default parameter {ParameterName} updated to value {Value}", trigger.DisplayName, parameterName, newValue);
            await context.Publish(new TriggerChanged(trigger));
        }
    }

    /// <summary>
    /// Handles the <see cref="UpdateLocationConfigurationParameter"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<UpdateLocationConfigurationParameter> context)
    {
        var triggerId = context.Message.TriggerId;
        var locationId = context.Message.LocationId;
        string parameterName = context.Message.ParameterName;
        string newValue = context.Message.NewValue;

        var trigger = await triggerRepository.GetById(triggerId);
        var updatedParameter = trigger.UpdateConfigurationParameterAtLocation(locationId, parameterName, newValue);
        if (updatedParameter != null)
        {
            await triggerRepository.Update(trigger, true);
            logger.LogInformation("Trigger {Name} parameter {ParameterName} at location {LocationId} updated to value {Value}", trigger.DisplayName, locationId, parameterName, newValue);
            await context.Publish(new TriggerChanged(trigger));
        }
    }

    /// <summary>
    /// Handles the <see cref="ResetLocationConfigurationParameter"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<ResetLocationConfigurationParameter> context)
    {
        var triggerId = context.Message.TriggerId;
        var locationId = context.Message.LocationId;
        string parameterName = context.Message.ParameterName;
        var trigger = await triggerRepository.GetById(triggerId);

        var updatedParameter = trigger.ResetConfigurationParameterAtLocation(locationId, parameterName);
        if (updatedParameter != null)
        {
            await triggerRepository.Update(trigger, true);
            logger.LogInformation("Trigger {Name} parameter {ParameterName} at location {LocationId} reset to default value {Value}", trigger.DisplayName, locationId, parameterName, updatedParameter.Value);
            await context.Publish(new TriggerChanged(trigger));
        }
    }

    /// <summary>
    /// Handles the <see cref="GetTriggerMapping"/> request.
    /// </summary>
    public async Task Consume(ConsumeContext<GetTriggerMapping> context)
    {
        ICollection<Trigger> triggers = await triggerRepository.GetTriggers();

        List<TriggerLocalization> localizations = triggers
            .SelectMany(trigger => trigger.Locations, (trigger, location) => new { trigger, location })
            .Where(tl => tl.location.Node != null || tl.location.Polygon.Count > 0)
            .Select(tl =>
                tl.location.Node != null ? new TriggerLocalization(tl.trigger.DisplayName, tl.location.Node.Id) : new TriggerLocalization(tl.trigger.DisplayName, tl.location.Node?.Id, tl.location.Id))
            .ToList();

        await context.RespondAsync(new GotTriggerMapping(localizations));
    }
}