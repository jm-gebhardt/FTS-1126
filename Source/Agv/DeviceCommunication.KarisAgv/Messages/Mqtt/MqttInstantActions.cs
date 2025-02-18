using Gebhardt.Acc.Common.Extensions;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.DeviceCommunication.KarisAgv.Messages.Mqtt;

/// <summary>
/// Instant actions to be executed by the agv.
/// <para>See "instantActions.schema".</para>
/// </summary>
[PublicAPI]
[EntityName("amq.topic")] // Specifies the RabbitMq exchange where the message is published
[RoutingKey("agv.instantAction")] // Specifies the topic where subscribers can receive the message
public record MqttInstantActions
{
    /// <summary>
    /// Serial number of the AGV.
    /// </summary>
    public string SerialNumber { get; set; } = string.Empty;

    /// <summary>
    /// The instant actions to be run
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
    /// The type of the instant action.
    /// </summary>
    public string ActionType { get; set; } = string.Empty;

    /// <summary>
    /// Unique ID to identify the action and map them to the actionState in the state.
    /// </summary>
    public string ActionId { get; set; } = string.Empty;

    /// <summary>
    /// Parameter values of the instant action. If not defined, the action has no parameters.
    /// </summary>
    public Dictionary<string, object>? Parameters { get; set; }
}