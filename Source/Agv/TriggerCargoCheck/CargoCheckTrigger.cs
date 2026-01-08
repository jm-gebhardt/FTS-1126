using System.Collections.Concurrent;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.Triggers;
using Gebhardt.Acc.Common.TriggerDefinition;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.TriggerCargoCheck;

/// <summary>
/// Represents a trigger at cargo reporting point which checks if agv has cargo loaded.
/// </summary>
[UsedImplicitly]
public class CargoCheckTrigger(IBus bus) : IFfcTrigger
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
        const string name = "DP2";
        return new TriggerProperties(urn: "www.gebhardt-group.com:kaeser.DP2", displayName: name, description: "Meldepunkt DP2: Leeres Agv an APL", version: "0.0.1", instancesMax: 1);
    }

    /// <inheritdoc cref="IFfcTrigger"/>>
    public async Task Invoke(Dictionary<string, ConfigurationParameter> parameters, AgvData agvData, AgvStatus agvStatus, ActionGroup? actionGroup, ICollection<Cargo> cargo, ICollection<AgvError> errors)
    {
        await bus.Publish(new AgvPassedCargoTriggerPoint(agvStatus.CorrelationId, actionGroup?.CorrelationId, actionGroup?.Actions.Single(a => a.State == ActionState.Started), cargo.Count > 0));
    }

    /// <inheritdoc cref="IFfcTrigger"/>>
    public Task InvokeActionGroupChanged(Dictionary<string, ConfigurationParameter> parameters, AgvData agvData, AgvStatus agvStatus, ActionGroup? actionGroup, ICollection<Cargo> cargo, ICollection<AgvError> errors)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc cref="IFfcTrigger"/>>
    public Task Leave(Dictionary<string, ConfigurationParameter> parameters, AgvData agvData, AgvStatus agvStatus, ActionGroup? actionGroup, ICollection<Cargo> cargo, ICollection<AgvError> errors)
    {
        return Task.CompletedTask;
    }
}