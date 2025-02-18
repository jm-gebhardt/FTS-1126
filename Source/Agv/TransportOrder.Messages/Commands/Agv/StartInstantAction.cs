using Gebhardt.Acc.Common.Messages.AgvManagement.Commands;
using System.Text.Json.Serialization;
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
    public ActionBlockingType? BlockingType { get; set; } 

    /// <inheritdoc cref="StartInstantAction"/>>
    public StartInstantAction(Guid agvCorrelationId, string agvInstantActionType, Dictionary<string, object>? parameters, ActionBlockingType? blockingType)
    {
        AgvInstantActionType = agvInstantActionType;
        AgvCorrelationId = agvCorrelationId;
        Parameters = parameters;
        BlockingType = blockingType;
    }

    /// <summary>
    /// Deconstruct the message.
    /// </summary>
    public void Deconstruct(out Guid agvCorrelationId, out string agvInstantActionType, out Dictionary<string, object>? parameters, out ActionBlockingType? blockingType)
    {
        agvCorrelationId = AgvCorrelationId;
        agvInstantActionType = AgvInstantActionType;
        parameters = Parameters;
        blockingType = BlockingType;
    }
}
