using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.Queue;
using Gebhardt.Acc.Common.Messages.Triggers;
using Gebhardt.Acc.Common.TriggerDefinition;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Gebhardt.Acc.TriggerEvacuateOrAssign;

/// <summary>
/// Represents a trigger that if enabled triggers evacuation of a queue head when an agv running a pick drop action group passes the trigger.
/// If a drop to the configured station passes the trigger, evacuate head or assign to next pick at station.
/// </summary>
[PublicAPI]
public class HeadEvacuationOrAssignmentTrigger(IBus bus, ILogger<HeadEvacuationOrAssignmentTrigger> logger) : IFfcTrigger
{
    private readonly ConfigurationParameter _enableParameter = new("Enable", "Flag for switching on queue head evacuation", false.ToString().ToLower(), "^(true|false)$");
    private readonly ConfigurationParameter _queueNameParameter = new("QueueName", "The name of the queue to evacuate the head of", string.Empty, string.Empty);
    private readonly ConfigurationParameter _evacuationNodeParameter = new("EvacuationNode", "The node to evacuate the queue to", string.Empty, string.Empty);
    private readonly ConfigurationParameter _doppelspielStationParameter = new("DoppelspielStation", "The station to check for pick", string.Empty, string.Empty);

    /// <inheritdoc cref="IFfcTrigger"/>>
    public ConcurrentDictionary<string, ConfigurationParameter> GetDefaultConfigurationParameters()
    {
        var config = new ConcurrentDictionary<string, ConfigurationParameter>();
        config.TryAdd(_enableParameter.Name, _enableParameter);
        config.TryAdd(_evacuationNodeParameter.Name, _evacuationNodeParameter);
        config.TryAdd(_queueNameParameter.Name, _queueNameParameter);
        config.TryAdd(_doppelspielStationParameter.Name, _doppelspielStationParameter);
        return config;
    }

    /// <inheritdoc cref="IFfcTrigger"/>>
    public void UpdateDefaultConfigurationParameter(string name, string value)
    {
        if (name == _enableParameter.Name)
        {
            _enableParameter.Value = value;
        }

        if (name == _queueNameParameter.Name)
        {
            _queueNameParameter.Value = value;
        }

        if (name == _evacuationNodeParameter.Name)
        {
            _evacuationNodeParameter.Value = value;
        }

        if (name == _evacuationNodeParameter.Name)
        {
            _doppelspielStationParameter.Value = value;
        }
    }

    /// <inheritdoc cref="IFfcTrigger"/>>
    public TriggerProperties GetProperties()
    {
        const string name = "QHEA";
        return new TriggerProperties(urn: "www.gebhardt-group.com:kaeser.QHEA", displayName: name, description: "Warteschlangen Kopf Evakuierung oder Auftragszuweisung", version: "0.0.1", instancesMax: 120);
    }

    /// <inheritdoc cref="IFfcTrigger"/>>
    public async Task Invoke(Dictionary<string, ConfigurationParameter> parameters, AgvData agvData, AgvStatus agvStatus, ActionGroup? actionGroup, ICollection<Cargo> cargo, ICollection<AgvError> errors)
    {
        if (!ShouldEvacuate(parameters))
        {
            logger.LogInformation("Queue evacuation switched off");
            return;
        }
        if (actionGroup == null || !actionGroup.Actions.Any(a => a.ActionType is ActionType.Pick or ActionType.Drop))
        {
            logger.LogInformation("Agv {AgvCorrelationId} has no pick-drop action group {ActionGroupCorrelationId}, not triggering queue evacuation or assignment", agvStatus.CorrelationId, actionGroup?.CorrelationId);
            return;
        }
        string stationName = GetStationName(parameters);
        string queueName = GetQueueName(parameters);
        var evacuationNode = GetEvacuationNode(parameters);
        if (actionGroup.Actions.Any(a => a.ActionType == ActionType.Drop && a.Target.Node?.Name == stationName))
        {
            logger.LogInformation("Trigger evacuation or assignment of head of queue {QueueName} to node {EvacuationNode} for agv {AgvCorrelationId}", queueName, evacuationNode, agvStatus.CorrelationId);
            await bus.Publish(new EvacuateOrAssignQueueHead(queueName, evacuationNode, agvStatus.CorrelationId, stationName));
            return;
        } 
       
        logger.LogInformation("Trigger evacuation of head of queue {QueueName} to node {EvacuationNode} for agv {AgvCorrelationId}", queueName, evacuationNode, agvStatus.CorrelationId);
        await bus.Publish(new EvacuateQueueHead(queueName, evacuationNode));
    }

    /// <inheritdoc cref="IFfcTrigger"/>>
    public Task InvokeActionGroupChanged(Dictionary<string, ConfigurationParameter> parameters, AgvData agvData, AgvStatus agvStatus, ActionGroup? actionGroup, ICollection<Cargo> cargo, ICollection<AgvError> errors)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc cref="IFfcTrigger"/>>
    public Task Leave(Dictionary<string, ConfigurationParameter> parameters, AgvData agvData, AgvStatus agvStatus, ActionGroup? actionGroup, ICollection<Cargo> cargo, ICollection<AgvError> errors)
    {
        throw new NotImplementedException();
    }

    private bool ShouldEvacuate(Dictionary<string, ConfigurationParameter> parameters)
    {
        var enable = parameters.GetValueOrDefault(_enableParameter.Name) ??
                     GetDefaultConfigurationParameters().GetValueOrDefault(_enableParameter.Name);
        return bool.Parse(enable!.Value);
    }

    private string GetQueueName(Dictionary<string, ConfigurationParameter> parameters)
    {
        var queueName = parameters.GetValueOrDefault(_queueNameParameter.Name) ??
                        GetDefaultConfigurationParameters().GetValueOrDefault(_queueNameParameter.Name);
        return queueName?.Value ?? string.Empty;
    }

    private Guid GetEvacuationNode(Dictionary<string, ConfigurationParameter> parameters)
    {
        var node = parameters.GetValueOrDefault(_evacuationNodeParameter.Name) ??
                   GetDefaultConfigurationParameters().GetValueOrDefault(_evacuationNodeParameter.Name);
        return !string.IsNullOrEmpty(node?.Value) ? node.Value.ToGuid() : Guid.Empty;
    }

    private string GetStationName(Dictionary<string, ConfigurationParameter> parameters)
    {
        var node = parameters.GetValueOrDefault(_doppelspielStationParameter.Name) ??
                   GetDefaultConfigurationParameters().GetValueOrDefault(_doppelspielStationParameter.Name);
        return !string.IsNullOrEmpty(node?.Value) ? node.Value : string.Empty;
    }
}