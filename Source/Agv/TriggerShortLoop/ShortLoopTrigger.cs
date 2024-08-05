using System.Collections.Concurrent;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.Triggers;
using Gebhardt.Acc.Common.TriggerDefinition;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.TriggerShortLoop;

/// <summary>
/// Represents a trigger at short loop decision point which reports if agv has cargo.
/// </summary>
[UsedImplicitly]
public class ShortLoopTrigger(IBus bus) : IFfcTrigger
{
    /// <inheritdoc cref="IFfcTrigger"/>>
    public ConcurrentDictionary<string, ConfigurationParameter> GetConfigurationParameters()
    {
        var config = new ConcurrentDictionary<string, ConfigurationParameter>();
        return config;
    }

    /// <inheritdoc cref="IFfcTrigger"/>>
    public void UpdateConfigurationParameter(string name, string value)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc cref="IFfcTrigger"/>>
    public TriggerProperties GetProperties()
    {
        const string name = "DP3";
        return new TriggerProperties(urn: "www.gebhardt-group.com:kaeser.DP3", displayName: name, description: "Entscheidungspunkt DP3: Ziel AKL 1050", version: "0.0.1", instancesMax: 1);
    }

    /// <inheritdoc cref="IFfcTrigger"/>>
    public async Task Invoke(AgvData agvData, AgvStatus agvStatus, ActionGroup? actionGroup, ICollection<Cargo> cargo, ICollection<AgvError> errors)
    {
        await bus.Publish(new AgvPassedShortLoopTriggerPoint(agvData, agvStatus, actionGroup, cargo, errors));
    }
}