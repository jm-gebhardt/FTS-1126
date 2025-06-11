using Gebhardt.Acc.Common.Messages.Triggers;
using Gebhardt.Acc.Common.TriggerDefinition;
using MassTransit;
using System.Collections.Concurrent;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TriggerDoppelspiel;

/// <summary>
/// Represents a trigger that initiates the "Doppelspiel"
/// </summary>
/// <param name="bus"></param>
[PublicAPI]
public class DoppelspielTrigger(IBus bus) : IFfcTrigger
{
    private const string PropertyName = "TargetStationName";
    private const string PropertyDescription = "Name of the target station where Doppelspiel is done";
    private string _propertyValue = string.Empty;

    /// <inheritdoc cref="IFfcTrigger"/>>
    public ConcurrentDictionary<string, ConfigurationParameter> GetDefaultConfigurationParameters()
    {
        var config = new ConcurrentDictionary<string, ConfigurationParameter>();
        config.TryAdd(PropertyName, new ConfigurationParameter(PropertyName, PropertyDescription, _propertyValue, string.Empty));
        return config;
    }

    /// <inheritdoc cref="IFfcTrigger"/>>
    public void UpdateDefaultConfigurationParameter(string name, string value)
    {
        _propertyValue = value;
    }

    /// <inheritdoc cref="IFfcTrigger"/>>
    public TriggerProperties GetProperties()
    {
        const string name = "DP3";
        return new TriggerProperties(urn: "www.gebhardt-group.com:kaeser.DP3", displayName: name, description: "Doppelspiel-Trigger", version: "0.0.1", instancesMax: 20);
    }

    /// <inheritdoc cref="IFfcTrigger"/>>
    public async Task Invoke(Dictionary<string, ConfigurationParameter> parameters, AgvData agvData, AgvStatus agvStatus, ActionGroup? actionGroup, ICollection<Cargo> cargo, ICollection<AgvError> errors)
    {
        parameters.TryGetValue(PropertyName, out var targetStationName);
        if (actionGroup?.Actions.Any(a => a.ActionType == ActionType.Pick) ?? false)
        {
            await bus.Publish(new AgvPassedDoppelspielTriggerPoint(agvData, agvStatus, actionGroup, cargo, errors, targetStationName?.Value ?? _propertyValue));
        }
    }

    /// <inheritdoc cref="IFfcTrigger"/>>
    public Task InvokeActionGroupChanged(Dictionary<string, ConfigurationParameter> parameters, AgvData agvData, AgvStatus agvStatus, ActionGroup? actionGroup, ICollection<Cargo> cargo, ICollection<AgvError> errors)
    {
        return Task.CompletedTask;
    }
}