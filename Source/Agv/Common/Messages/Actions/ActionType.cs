using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.Common.Messages.Actions;

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
    /// Do pick at FromStation and drop at ToStation
    /// </summary>
    Transport,
    /// <summary>
    /// Simply drive anywhere
    /// </summary>
    Move,
    /// <summary>
    /// Drive to the ToStation, which is a charger and start charging
    /// </summary>
    Charge
}