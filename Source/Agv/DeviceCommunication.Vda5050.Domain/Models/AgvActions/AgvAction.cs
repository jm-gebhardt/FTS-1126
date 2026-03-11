using Gebhardt.Acc.Common.Domain.Models;
using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.AgvActions;

/// <summary>
/// An action type supported by the AGV, including its parameters and applicable scopes.
/// </summary>
[PublicAPI]
public class AgvAction
{
    /// <summary>
    /// The serial number of the agv.
    /// </summary>
    public string AgvSerialNumber { get; set; } = null!;

    /// <summary>
    /// Unique actionType corresponding to action.actionType.
    /// </summary>
    public string ActionType { get; set; } = string.Empty;

    /// <summary>
    /// Free text description of the action.
    /// </summary>
    public string? ActionDescription { get; set; }

    /// <summary>
    /// List of allowed scopes for using this action type.
    /// </summary>
    public ICollection<ActionScope> ActionScopes { get; set; } = [];

    /// <summary>
    /// List of parameters for this action. Omit or leave empty if the action has no parameters.
    /// </summary>
    public ICollection<AgvActionParameter>? ActionParameters { get; set; }

    /// <summary>
    /// Free text description of the result description.
    /// </summary>
    public string? ResultDescription { get; set; }

    /// <summary>
    /// Array of possible blocking types for this action.
    /// </summary>
    public ICollection<BlockingType>? BlockingTypes { get; set; }

    /// <summary>
    /// Resolves the blocking type to use for an order action, preferring the requested type if supported.
    /// </summary>
    /// <param name="preferred">The preferred blocking type, typically from the order action context.</param>
    /// <returns>The preferred type if supported, the first supported type, or <see cref="BlockingType.HARD"/> as a fallback.</returns>
    public BlockingType ResolveBlockingType(BlockingType? preferred = null)
    {
        if (BlockingTypes is { Count: > 0 })
        {
            if (preferred != null && BlockingTypes.Contains(preferred.Value))
            {
                return preferred.Value;
            }
            return BlockingTypes.First();
        }
        return BlockingType.HARD;
    }

    /// <summary>
    /// True, of the given parameter is required to execute this agv action.
    /// </summary>
    /// <param name="parameterKey">The parameter key.</param>
    public bool IsParameterRequired(string parameterKey)
    {
        if (ActionParameters == null || ActionParameters.Count == 0)
        {
            return false;
        }

        var parameter = ActionParameters.SingleOrDefault(p => p.Key == parameterKey);
        if (parameter == null)
        {
            return false;
        }
        return !parameter.IsOptional;
    }
}