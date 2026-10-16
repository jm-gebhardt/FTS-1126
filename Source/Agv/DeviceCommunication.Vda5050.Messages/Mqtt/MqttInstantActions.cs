using Gebhardt.Acc.Common.Domain.Models;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Mqtt;

/// <summary>
/// Instant actions to be executed by the agv.
/// <para>See "instantActions.schema".</para>
/// </summary>
[PublicAPI]
[EntityName("amq.topic")] // Specifies the RabbitMq exchange where the message is published
public record MqttInstantActions: MqttHeader
{
    /// <summary>
    /// The actions that the AGV can perform.
    /// </summary>
    public ICollection<MqttInstantAction> Actions { get; set; } = [];
}

/// <summary>
/// The instant action that is to be executed.
/// </summary>
[PublicAPI]
public record MqttInstantAction
{
    /// <summary>
    /// Name of action as described in the first column of \"Actions and Parameters\". Identifies the function of the action.
    /// </summary>
    public string ActionType { get; set; } = string.Empty;

    /// <summary>
    /// Unique ID to identify the action and map them to the actionState in the state.
    /// </summary>
    public string ActionId { get; set; } = string.Empty;

    /// <summary>
    /// Additional information on the action.
    /// </summary>
    public string? ActionDescription { get; set; }

    /// <summary>
    /// Parameter values of the instant action. If not defined, the action has no parameters.
    /// </summary>
    public ICollection<MqttActionParameter>? ActionParameters { get; set; }

    /// <summary>
    /// The blocking type to be used.
    /// </summary>
    public BlockingType BlockingType { get; set; }
}