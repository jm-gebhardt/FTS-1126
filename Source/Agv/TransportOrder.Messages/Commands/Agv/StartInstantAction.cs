using System.Text.Json.Serialization;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages.Converter;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;

/// <summary>
/// Provides a command to start an instant action for an agv.
/// </summary>
[PublicAPI]
public class StartInstantAction
{
    /// <summary>
    /// The correlation id of the agv.
    /// </summary>
    public Guid AgvCorrelationId { get; set; }

    /// <summary>
    /// The type of the instant action.
    /// </summary>
    public string AgvInstantActionType { get; set; }

    /// <summary>
    /// The parameters set for the instant action.
    /// </summary>
    [JsonConverter(typeof(InstantActionParameterJsonConverter))]
    public Dictionary<string, object>? Parameters { get; set; } 

    /// <summary>
    /// The blocking type of the action.
    /// </summary>
    public BlockingType? BlockingType { get; set; }

    /// <summary>
    /// The user who started the instant action.
    /// </summary>
    public string User { get; set; }

    /// <inheritdoc cref="StartInstantAction"/>>
    public StartInstantAction(Guid agvCorrelationId, string agvInstantActionType, Dictionary<string, object>? parameters, BlockingType? blockingType, string user)
    {
        AgvInstantActionType = agvInstantActionType;
        AgvCorrelationId = agvCorrelationId;
        Parameters = parameters;
        BlockingType = blockingType;
        User = user;
    }

    /// <summary>
    /// Deconstruct the message.
    /// </summary>
    public void Deconstruct(out Guid agvCorrelationId, out string agvInstantActionType, out Dictionary<string, object>? parameters, out BlockingType? blockingType, out string user)
    {
        agvCorrelationId = AgvCorrelationId;
        agvInstantActionType = AgvInstantActionType;
        parameters = Parameters;
        blockingType = BlockingType;
        user = User;
    }
}
