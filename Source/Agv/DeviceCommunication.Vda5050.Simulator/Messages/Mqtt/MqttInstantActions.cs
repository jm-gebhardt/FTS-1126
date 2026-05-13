using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Extensions;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Messages.Mqtt;

/// <summary>
/// Instant actions to be executed by the agv.
/// <para>See "instantActions.schema".</para>
/// </summary>
[PublicAPI]
[EntityName("amq.topic")] // Specifies the RabbitMq exchange where the message is published
[RoutingKey("*.*.*.*.instantActions")] // Specifies the topic where subscribers can receive the message
public record MqttInstantActions
{
    /// <summary>
    /// HeaderId of the message.
    /// The headerId is defined per topic and incremented by 1 with each sent (but not necessarily received) message.
    /// </summary>
    public uint HeaderId { get; set; }

    /// <summary>
    /// Timestamp in ISO8601 format (YYYY-MM-DDTHH:mm:ss.ffZ).
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>
    /// Version of the protocol [Major].[Minor].[Patch].
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Manufacturer of the AGV.
    /// </summary>
    public string Manufacturer { get; set; } = string.Empty;

    /// <summary>
    /// Serial number of the AGV.
    /// </summary>
    public string SerialNumber { get; set; } = string.Empty;

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

/// <summary>
/// Key-value parameter for an order action.
/// </summary>
[PublicAPI]
public record MqttActionParameter
{
    /// <summary>
    /// The key of the action parameter.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// The value of the action parameter.
    /// Can be a string, number, boolean, array, or object.
    /// </summary>
    public object Value { get; set; } = new();
}