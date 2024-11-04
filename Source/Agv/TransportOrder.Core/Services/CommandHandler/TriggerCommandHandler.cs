using AutoMapper;
using Gebhardt.Acc.Common.Messages.Triggers;
using Gebhardt.Acc.Common.Messages.Triggers.Commands;
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
    IConsumer<ResetLocationConfigurationParameter>
{
    /// <summary>
    /// Handles the <see cref="CreateTrigger"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<CreateTrigger> context)
    {
        ICollection<ConfigurationParameter> configurationParameters = context.Message.ConfigurationParameters;
        var properties = context.Message.Properties;

        var trigger = new Trigger(properties.Id, properties.Urn, properties.DisplayName, properties.Version, properties.Description, properties.InstanceMaximum, configurationParameters);

        await triggerRepository.Add(trigger);

        logger.LogInformation("Trigger {Name} created", trigger.DisplayName);
    }

    /// <summary>
    /// Handles the <see cref="AddTriggerLocation"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<AddTriggerLocation> context)
    {
        Guid? edgeId = context.Message.EdgeId;
        string? nodeId = context.Message.NodeId;
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
                location = trigger.AddLocation(edgeId.Value, mapId, configurationParameters);
            }
            if (nodeId != null)
            {
                location = trigger.AddLocation(nodeId, mapId, configurationParameters);
            }
            if (polygon.Count > 0)
            {
                location = trigger.AddLocation(polygon, mapId, configurationParameters);
            }
            await triggerRepository.Update(trigger);
        }
        catch (Exception ex)
        {
            exception = ex;
        }
        await context.RespondAsync(new TriggerLocationAdded(location, exception?.Message));

        if (exception == null)
        {
            logger.LogInformation("Trigger {Name} assigned to location {Location}", trigger.DisplayName, location!.Id);

            if (nodeId != null)
            {
                await context.Publish(new TriggerAssignedToNode(triggerId, trigger.DisplayName, nodeId));
            }
            if (polygon.Count > 0)
            {
                await context.Publish(new TriggerAssignedToArea(triggerId, trigger.DisplayName, location.Id));
                logger.LogDebug("Location {Location} with polygon: {Polygon}", location.Id, location.Polygon.OrderBy(p => p.Index).Select(p => $"({p.X}, {p.Y})"));
            }
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
        await triggerRepository.Update(trigger);
        logger.LogDebug("Trigger {Name} removed from location {LocationId}", trigger.DisplayName, locationId);
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
            await triggerRepository.Update(trigger);
            await context.Publish(new DefaultTriggerConfigurationUpdated(triggerId, [updatedParameter]));
            logger.LogInformation("Trigger {Name} default parameter {ParameterName} updated to value {Value}", trigger.DisplayName, parameterName, newValue);
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
            await triggerRepository.Update(trigger);
            logger.LogInformation("Trigger {Name} parameter {ParameterName} at location {LocationId} updated to value {Value}", trigger.DisplayName, locationId, parameterName, newValue);
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
            await triggerRepository.Update(trigger);
            logger.LogInformation("Trigger {Name} parameter {ParameterName} at location {LocationId} reset to default value {Value}", trigger.DisplayName, locationId, parameterName, updatedParameter.Value);
        }
    }
}