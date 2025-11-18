using System.Collections.Concurrent;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.Triggers;
using Gebhardt.Acc.Common.TriggerDefinition;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.TriggerDecisionPoint1;

/// <summary>
/// Represents a trigger at decision point 1.
/// </summary>
[UsedImplicitly]
public class DecisionPoint1Trigger(IBus bus, ILogger<DecisionPoint1Trigger> logger) : IFfcTrigger
{
    private readonly ConfigurationParameter _useJamStateParameter = new("UseJamState", "Flag for switching on jam state consideration", false.ToString().ToLower(), "^(true|false)$");

    private readonly ConfigurationParameter _consideredStationsParameter = new("ConsideredStations", "Comma-separated list of stations to consider for target decision", "PS01,PS02", "^[a-zA-Z0-9/-]+(,\\s*[a-zA-Z0-9/-]+)*$");

    /// <inheritdoc cref="IFfcTrigger"/>>
    public ConcurrentDictionary<string, ConfigurationParameter> GetDefaultConfigurationParameters()
    {
        var config = new ConcurrentDictionary<string, ConfigurationParameter>();
        
        config.TryAdd(_consideredStationsParameter.Name, _consideredStationsParameter);
        config.TryAdd(_useJamStateParameter.Name, _useJamStateParameter);
        return config;
    }

    /// <inheritdoc cref="IFfcTrigger"/>>
    public void UpdateDefaultConfigurationParameter(string name, string value)
    {
        if (name == _consideredStationsParameter.Name)
        {
            _consideredStationsParameter.Value = value;
        }
        if (name == _useJamStateParameter.Name)
        {
            _useJamStateParameter.Value = value;
        }
    }

    /// <inheritdoc cref="IFfcTrigger"/>>
    public TriggerProperties GetProperties()
    {
        const string name = "DP1";
        return new TriggerProperties(urn: "www.gebhardt-group.com:kaeser.DP1", displayName: name, description: "Entscheidungspunkt DP1: Ziel APL 1060", version: "0.0.1", instancesMax: 1);
    }

    /// <inheritdoc cref="IFfcTrigger"/>>
    public async Task Invoke(Dictionary<string, ConfigurationParameter> parameters, AgvData agvData, AgvStatus agvStatus, ActionGroup? actionGroup, ICollection<Cargo> cargo, ICollection<AgvError> errors)
    {
        ICollection<Guid> consideredStations = GetConsideredStations(parameters);
        await bus.Publish(new AgvPassedDecisionPoint1(agvData, agvStatus, actionGroup, cargo, errors, consideredStations, ShouldUseJamState(parameters)));
    }

    /// <inheritdoc cref="IFfcTrigger"/>>
    public async Task InvokeActionGroupChanged(Dictionary<string, ConfigurationParameter> parameters, AgvData agvData, AgvStatus agvStatus, ActionGroup? actionGroup, ICollection<Cargo> cargo, ICollection<AgvError> errors)
    {
        logger.LogInformation("Action group changed invoked for {Name}; redirecting to default trigger invocation", nameof(DecisionPoint1Trigger));

        await Invoke(parameters, agvData, agvStatus, actionGroup, cargo, errors);
    }

    /// <inheritdoc cref="IFfcTrigger"/>>
    public Task Leave(Dictionary<string, ConfigurationParameter> parameters, AgvData agvData, AgvStatus agvStatus, ActionGroup? actionGroup, ICollection<Cargo> cargo, ICollection<AgvError> errors)
    {
        return Task.CompletedTask;
    }
    
    private bool ShouldUseJamState(Dictionary<string, ConfigurationParameter> parameters)
    {
        var enable = parameters.GetValueOrDefault(_useJamStateParameter.Name) ??
                     GetDefaultConfigurationParameters().GetValueOrDefault(_useJamStateParameter.Name);
        return bool.Parse(enable!.Value);
    }

    private ICollection<Guid> GetConsideredStations(Dictionary<string, ConfigurationParameter> parameters)
    {
        var parameter = parameters.GetValueOrDefault(_consideredStationsParameter.Name) ?? GetDefaultConfigurationParameters().GetValueOrDefault(_consideredStationsParameter.Name);
        logger.LogInformation("Considered stations for DP1 are {Stations}", parameter?.Value);
        return parameter?.Value.Split(",").Select(p => p.Trim().ToGuid()).ToList() ?? [];
    }
}