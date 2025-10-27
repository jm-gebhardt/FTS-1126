using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.Queue;
using Gebhardt.Acc.Common.Messages.Triggers;
using Gebhardt.Acc.Common.TriggerDefinition;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Utilities;

namespace Gebhardt.Acc.TriggerQueueHeadEvacuation;

/// <summary>
/// Represents a trigger that if enabled triggers evacuation of a queue head when an agv running a pick drop action group passes the trigger.
/// </summary>
[PublicAPI]
public class QueueHeadEvacuationTrigger(IBus bus, ILogger<QueueHeadEvacuationTrigger> logger) : IFfcTrigger
{
    private readonly ConfigurationParameter _enableParameter = new("Enable", "Flag for switching on queue head evacuation", false.ToString().ToLower(), "^(true|false)$");
    private readonly ConfigurationParameter _queueNameParameter = new("QueueName", "The name of the queue to evacuate the head of", string.Empty, string.Empty);
    private readonly ConfigurationParameter _evacuationNodeParameter = new("EvacuationNode", "The node to evacuate the queue to", string.Empty, string.Empty);

    /// <inheritdoc cref="IFfcTrigger"/>>
    public ConcurrentDictionary<string, ConfigurationParameter> GetDefaultConfigurationParameters()
    {
        var config = new ConcurrentDictionary<string, ConfigurationParameter>();
        config.TryAdd(_enableParameter.Name, _enableParameter);
        config.TryAdd(_evacuationNodeParameter.Name, _evacuationNodeParameter);
        config.TryAdd(_queueNameParameter.Name, _queueNameParameter);
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
    }

    /// <inheritdoc cref="IFfcTrigger"/>>
    public TriggerProperties GetProperties()
    {
        const string name = "QHE";
        return new TriggerProperties(urn: "www.gebhardt-group.com:kaeser.QHE", displayName: name, description: "Warteschlangen Kopf Evakuierung", version: "0.0.1", instancesMax: 120);
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
            logger.LogInformation("Agv {AgvCorrelationId} has no pick-drop action group {ActionGroupCorrelationId}, not triggering queue evacuation", agvStatus.CorrelationId, actionGroup?.CorrelationId);
            return;
        }
        string queueName = GetQueueName(parameters);
        var evacuationNode = GetEvacuationNode(parameters);
        logger.LogInformation("Trigger evacuation of head of queue {QueueName} to node {EvacuationNode} for agv {AgvCorrelationId}", queueName, evacuationNode, agvStatus.CorrelationId);
        await bus.Publish(new EvacuateQueueHead(queueName, evacuationNode, agvStatus.CorrelationId));
    }

    /// <inheritdoc cref="IFfcTrigger"/>>
    public Task InvokeActionGroupChanged(Dictionary<string, ConfigurationParameter> parameters, AgvData agvData, AgvStatus agvStatus, ActionGroup? actionGroup, ICollection<Cargo> cargo, ICollection<AgvError> errors)
    {
        return Task.CompletedTask;
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

    /// <inheritdoc cref="IFfcTrigger"/>>
    public Task Leave(Dictionary<string, ConfigurationParameter> parameters, AgvData agvData, AgvStatus agvStatus, ActionGroup? actionGroup, ICollection<Cargo> cargo, ICollection<AgvError> errors)
    {
        return Task.CompletedTask;
    }
}