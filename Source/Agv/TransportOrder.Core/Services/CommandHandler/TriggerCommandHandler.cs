using AutoMapper;
using Gebhardt.Acc.Common.Messages.Triggers;
using Gebhardt.Acc.Common.Messages.Triggers.Commands;
using Gebhardt.Acc.TransportOrder.Domain.Models.Trigger;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Trigger;
using Gebhardt.Acc.TransportOrder.Messages.Events.Trigger;
using JetBrains.Annotations;
using MassTransit;
using PolygonPoint = Gebhardt.Acc.TransportOrder.Domain.Models.Polygon.PolygonPoint;

namespace Gebhardt.Acc.TransportOrder.Core.Services.CommandHandler;

/// <summary>
/// Provides handlers for trigger-related commands.
/// </summary>
[UsedImplicitly]
public class TriggerCommandHandler(ITriggerRepository triggerRepository, IMapper mapper) :
    IConsumer<CreateTrigger>,
    IConsumer<AddTriggerLocation>,
    IConsumer<RemoveTriggerLocation>,
    IConsumer<UpdateConfigurationParameter>
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
      
        ICollection<PolygonPoint> polygon =  mapper.Map<List<PolygonPoint>>(context.Message.Polygon);

        Location? location = null;
        Exception? exception = null;
        Trigger trigger = null!;

        try
        {
            trigger = await triggerRepository.GetById(triggerId);
            if (edgeId != null)
            {
                location= trigger.AddLocation(edgeId.Value, mapId);
            }
            if (nodeId != null)
            {
                location= trigger.AddLocation(nodeId, mapId);
            }
            if (polygon.Count > 0)
            {
                location= trigger.AddLocation(polygon, mapId);
            }
            await triggerRepository.Update(trigger);
        }
        catch (Exception ex)
        {
            exception = ex;
        }
        await context.RespondAsync(new TriggerLocationAdded(location, exception?.Message));

        if (nodeId != null && exception == null)
        {
            await context.Publish(new TriggerAssignedToNode(triggerId, trigger.DisplayName, nodeId));
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
    }

    /// <summary>
    /// Handles the <see cref="UpdateConfigurationParameter"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<UpdateConfigurationParameter> context)
    {
        var triggerId = context.Message.TriggerId;
        string parameterName = context.Message.ParameterName;
        string newValue = context.Message.NewValue;

        var trigger = await triggerRepository.GetById(triggerId);
        var updatedParameter = trigger.UpdateConfigurationParameter(parameterName, newValue);
        if(updatedParameter!= null)
        {
            await triggerRepository.Update(trigger);
            await context.Publish(new TriggerConfigurationUpdated(triggerId, [updatedParameter]));
        }
    }
}