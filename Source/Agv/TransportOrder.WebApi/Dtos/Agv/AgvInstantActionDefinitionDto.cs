using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages.AgvManagement.Commands;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.WebApi.Dtos.Agv;

/// <summary>
/// Definition for an agv instant action.
/// </summary>
[PublicAPI]
public record AgvInstantActionDefinitionDto
{
    /// <summary>
    /// Description of the action
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Description of the actions result
    /// </summary>
    public string ResultDescription { get; set; } = string.Empty;

    /// <summary>
    /// Unique type of action
    /// </summary>
    public string ActionType { get; set; } = string.Empty;

    /// <summary>
    /// Array of parameters an action has. If not defined, the action has no parameters.
    /// </summary>
    public ICollection<AgvInstantActionParameterDefinitionDto>? Parameters { get; set; } = [];

    /// <summary>
    /// Array of possible blocking types for defined action
    /// </summary>
    public ICollection<BlockingType> BlockingTypes { get; set; } = [];
}

/// <summary>
/// Definition of a parameter for an instant agv action.
/// </summary>
/// <param name="Key">Key string for parameter.</param>
/// <param name="ValueType">Data type of value .</param>
/// <param name="Description">Description of the parameter.</param>
/// <param name="IsOptional">If true, parameter is optional.</param>
[PublicAPI]
public record AgvInstantActionParameterDefinitionDto(string Key, InstantActionParameterType ValueType, string Description, bool IsOptional);

/// <summary>
/// Instant action parameter.
/// </summary>
/// <param name="Key"></param>
/// <param name="Value"></param>
[PublicAPI]
public record AgvInstantActionParameterDto(string Key, object Value);

/// <summary>
/// Instant action.
/// </summary>
/// <param name="ActionType">The type of the action.</param>
/// <param name="BlockingType">The blocking type</param>
/// <param name="Parameters">The parameters of the action.</param>
[PublicAPI]
public record AgvInstantActionDto(string ActionType, BlockingType? BlockingType, ICollection<AgvInstantActionParameterDto>? Parameters);