using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.Triggers;
using Gebhardt.Acc.Common.TriggerDefinition;
using MassTransit;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using Gebhardt.Acc.Common.Domain.Utilities;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TriggerShortLoopWaitingPoint;

/// <summary>
/// Represents a trigger that invokes handling of waiting actions in the short loop.
/// </summary>
[UsedImplicitly]
public class ShortLoopWaitingPointTrigger(IBus bus, ILogger<ShortLoopWaitingPointTrigger> logger) : IFfcTrigger
{
    private readonly ConfigurationParameter _secondaryTargetsParameter = new("SecondaryTargets", "Comma-separated list of stations to consider as a target, if the primary station is locked. Stations will be used in this order", "PS02,PS01", "^[a-zA-Z0-9/-]+(,\\s*[a-zA-Z0-9/-]+)*$");
    private readonly ConfigurationParameter _moveTargetParameter = new("MoveTarget", "The node agvs move to prior to target selection", "map/NODE-27", string.Empty);
    private readonly ConfigurationParameter _primaryTargetParameter = new("PrimaryTarget", "The primary target station after waiting", "PS03", string.Empty);
    
    /// <inheritdoc cref="IFfcTrigger"/>>
    public ConcurrentDictionary<string, ConfigurationParameter> GetDefaultConfigurationParameters()
    {
        var config = new ConcurrentDictionary<string, ConfigurationParameter>();
        config.TryAdd(_moveTargetParameter.Name, _moveTargetParameter);
        config.TryAdd(_primaryTargetParameter.Name, _primaryTargetParameter);
        config.TryAdd(_secondaryTargetsParameter.Name, _secondaryTargetsParameter);
        return config;
    }

    /// <inheritdoc cref="IFfcTrigger"/>>
    public void UpdateDefaultConfigurationParameter(string name, string value)
    {
        if (name == _moveTargetParameter.Name)
        {
            _moveTargetParameter.Value = value;
        }

        if (name == _moveTargetParameter.Name)
        {
            _secondaryTargetsParameter.Value = value;
        }
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
        if (actionGroup == null || actionGroup.Actions.All(a => a.ActionType != ActionType.Pick) || actionGroup.Actions.All(a => a.Target.Node != null))
        {
            logger.LogWarning("Skip triggering waiting order selection: Agv {AgvCorrelationId} passed short loop waiting handler without incomplete pick drop action group  ({ActionGroup})", agvStatus.CorrelationId, actionGroup?.CorrelationId);
            return;
        }
        var moveTarget = GetMoveTarget(parameters);
        if (actionGroup.Actions.All(a => a.Target.Node?.Id != moveTarget))
        {
            logger.LogWarning("Skip triggering waiting order selection: Agv {AgvCorrelationId} passed short loop waiting handler action group  ({ActionGroup}) registered for this waiting point {MoveTarget}", agvStatus.CorrelationId, actionGroup.CorrelationId, moveTarget);
            return;
        }
        var primaryTarget = GetPrimaryTarget(parameters);
        ICollection<Guid> secondaryTargets = GetSecondaryTargets(parameters);
        logger.LogInformation("Agv {AgvCorrelationId} passed short loop waiting trigger", agvData.AgvCorrelationId);
        await bus.Publish(new AgvPassedShortLoopWaitingHandler(agvData, agvStatus, actionGroup, cargo, errors, primaryTarget, secondaryTargets));
    }

    private Guid GetPrimaryTarget(Dictionary<string, ConfigurationParameter> parameters)
    {
        var parameter = parameters.GetValueOrDefault(_primaryTargetParameter.Name) ?? GetDefaultConfigurationParameters().GetValueOrDefault(_primaryTargetParameter.Name);
        logger.LogInformation("Primary target for waiting handling are {Stations}", parameter?.Value);
        return parameter?.Value.ToGuid() ?? Guid.Empty;
    }

    private Guid GetMoveTarget(Dictionary<string, ConfigurationParameter> parameters)
    {
        var parameter = parameters.GetValueOrDefault(_moveTargetParameter.Name) ?? GetDefaultConfigurationParameters().GetValueOrDefault(_moveTargetParameter.Name);
        logger.LogInformation("Move target for waiting handling are {Stations}", parameter?.Value);
        return parameter?.Value.ToGuid() ?? Guid.Empty;
    }

    private ICollection<Guid> GetSecondaryTargets(Dictionary<string, ConfigurationParameter> parameters)
    {
        var parameter = parameters.GetValueOrDefault(_secondaryTargetsParameter.Name) ?? GetDefaultConfigurationParameters().GetValueOrDefault(_secondaryTargetsParameter.Name);
        logger.LogInformation("Secondary targets for waiting handling are {Stations}", parameter?.Value);
        return parameter?.Value.Split(",").Select(p => p.Trim().ToGuid()).ToList() ?? [];
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
        logger.LogInformation("Agv {AgvCorrelationId} left short loop waiting trigger", agvData.AgvCorrelationId);
        await bus.Publish(new AgvLeftShortLoopWaitingHandler(agvData, agvStatus, actionGroup, cargo, errors));
    }
}