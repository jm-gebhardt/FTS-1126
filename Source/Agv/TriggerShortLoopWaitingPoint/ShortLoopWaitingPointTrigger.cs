using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.Triggers;
using Gebhardt.Acc.Common.TriggerDefinition;
using MassTransit;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TriggerShortLoopWaitingPoint;

/// <summary>
/// Represents a trigger that invokes handling of waiting actions in the short loop.
/// </summary>
[UsedImplicitly]
public class ShortLoopWaitingPointTrigger(IBus bus, ILogger<ShortLoopWaitingPointTrigger> logger) : IFfcTrigger
{
    /// <inheritdoc cref="IFfcTrigger"/>>
    public ConcurrentDictionary<string, ConfigurationParameter> GetDefaultConfigurationParameters()
    {
        var config = new ConcurrentDictionary<string, ConfigurationParameter>();
        return config;
    }

    /// <inheritdoc cref="IFfcTrigger"/>>
    public void UpdateDefaultConfigurationParameter(string name, string value)
    {
    }

    /// <inheritdoc cref="IFfcTrigger"/>>
    public TriggerProperties GetProperties()
    {
        const string name = "SLW";
        return new TriggerProperties(urn: "www.gebhardt-group.com:kaeser.SLW", displayName: name, description: "Wartepunkttrigger Short Loop, wählt nächste wartende Order für PS03 aus.", version: "0.0.1", instancesMax: 120);
    }

    /// <inheritdoc cref="IFfcTrigger"/>>
    public async Task Invoke(Dictionary<string, ConfigurationParameter> parameters, AgvData agvData, AgvStatus agvStatus, ActionGroup? actionGroup, ICollection<Cargo> cargo, ICollection<AgvError> errors)
    {
        if (actionGroup == null || actionGroup.Actions.All(a => a.ActionType != ActionType.Pick) || actionGroup.Actions.All(a => a.Target.NodeId != string.Empty))
        {
            logger.LogWarning("Skip triggering waiting order selection: Agv {AgvCorrelationId} passed short loop waiting handler without incomplete pick drop action group  ({ActionGroup})", agvStatus.CorrelationId, actionGroup?.CorrelationId);
            return;
        }
        logger.LogInformation("Agv {AgvCorrelationId} passed short loop waiting trigger", agvData.CorrelationId);
        await bus.Publish(new AgvPassedShortLoopWaitingHandler(agvData, agvStatus, actionGroup, cargo, errors));
    }

    /// <inheritdoc cref="IFfcTrigger"/>>
    public Task InvokeActionGroupChanged(Dictionary<string, ConfigurationParameter> parameters, AgvData agvData, AgvStatus agvStatus, ActionGroup? actionGroup, ICollection<Cargo> cargo, ICollection<AgvError> errors)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc cref="IFfcTrigger"/>>
    public async Task Leave(Dictionary<string, ConfigurationParameter> parameters, AgvData agvData, AgvStatus agvStatus, ActionGroup? actionGroup, ICollection<Cargo> cargo, ICollection<AgvError> errors)
    {
        if (actionGroup == null || actionGroup.Actions.All(a => a.ActionType != ActionType.Pick))
        {
            logger.LogWarning("Skip triggering removal of waiting order : Agv {AgvCorrelationId} passed short loop waiting handler without  pick drop action group  ({ActionGroup})", agvStatus.CorrelationId, actionGroup?.CorrelationId);
            return;
        }
        logger.LogInformation("Agv {AgvCorrelationId} left short loop waiting trigger", agvData.CorrelationId);
        await bus.Publish(new AgvLeftShortLoopWaitingHandler(agvData, agvStatus, actionGroup, cargo, errors));
    }
}