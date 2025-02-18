using Gebhardt.Acc.Common.Messages.AgvManagement.Commands;
using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Messages.AgvManagement.Events;

/// <summary>
/// Definition for an agv instant action.
/// </summary>
[PublicAPI]
public class AgvInstantAction
{
    /// <summary>
    /// Description of the action.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Description of the actions result.
    /// </summary>
    public string ResultDescription { get; set; } = string.Empty;

    /// <summary>
    /// Unique type of action.
    /// </summary>
    public string ActionType { get; set; } = string.Empty;

    /// <summary>
    /// Array of possible blocking types for defined action.
    /// </summary>
    public List<ActionBlockingType>? BlockingTypes { get; set; }

    /// <summary>
    /// Array of parameters an action has. If not defined, the action has no parameters.
    /// </summary>
    public List<AgvInstantActionParameterDefinition>? Parameters { get; set; } = [];
}

/// <summary>
/// Definition of a parameter for an instant agv action.
/// </summary>
[PublicAPI]
public record AgvInstantActionParameterDefinition
{
    /// <summary>
    /// Key string for parameter.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// /Data type of value.
    /// </summary>
    public string ValueType { get; set; } = string.Empty;

    /// <summary>
    /// Description of the parameter.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// If true, parameter is optional.
    /// </summary>
    public bool IsOptional { get; set; }
}