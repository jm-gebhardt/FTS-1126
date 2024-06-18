using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Messages.ActionGroups;

/// <summary>
/// The type of action to be performed
/// </summary>
[PublicAPI]
public enum ActionType
{
    /// <summary>
    /// Only pick at FromStation
    /// </summary>
    Pick,

    /// <summary>
    /// Only drop at ToStation
    /// </summary>
    Drop,

    /// <summary>
    /// Simply drive anywhere
    /// </summary>
    Move,

    /// <summary>
    /// Charge at ToStation
    /// </summary>
    Charge
}