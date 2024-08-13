using System.Collections.Concurrent;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.Triggers;
using Gebhardt.Acc.Common.TriggerDefinition;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.TriggerDecisionPoint1;

/// <summary>
/// Represents a trigger at decision point 1.
/// </summary>
[UsedImplicitly]
public class DecisionPoint1Trigger(IBus bus) : IFfcTrigger
{
    private const string PropertyName = "StationCount";
    private const string PropertyDescription = "Anzahl der Stationen, die als Ziel für APL 1060 verwendet werden können.";
    private int _propertyValue = 3;

    /// <inheritdoc cref="IFfcTrigger"/>>
    public ConcurrentDictionary<string, ConfigurationParameter> GetConfigurationParameters()
    {
        var config = new ConcurrentDictionary<string, ConfigurationParameter>();
        config.TryAdd(PropertyName, new ConfigurationParameter(PropertyName, PropertyDescription, _propertyValue.ToString(), @"^\d+$"));
        return config;
    }

    /// <inheritdoc cref="IFfcTrigger"/>>
    public void UpdateConfigurationParameter(string name, string value)
    {
        _propertyValue = int.Parse(value);
    }

    /// <inheritdoc cref="IFfcTrigger"/>>
    public TriggerProperties GetProperties()
    {
        const string name = "DP1";
        return new TriggerProperties(urn: "www.gebhardt-group.com:kaeser.DP1", displayName: name, description: "Entscheidungspunkt DP1: Ziel APL 1060", version: "0.0.1", instancesMax: 1);
    }

    /// <inheritdoc cref="IFfcTrigger"/>>
    public async Task Invoke(AgvData agvData, AgvStatus agvStatus, ActionGroup? actionGroup, ICollection<Cargo> cargo, ICollection<AgvError> errors)
    {
        int stationCount = int.Parse(GetConfigurationParameters().GetValueOrDefault(PropertyName)!.Value);

        await bus.Publish(new AgvPassedDecisionPoint1(agvData, agvStatus, actionGroup, cargo, errors, stationCount));
    }
}