using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.DeviceCommunication.Messages.Enums;

/// <summary>
/// The type of transport an order describes
/// </summary>
[PublicAPI]
public enum OrderType
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
    /// Simply drive to the ToStation
    /// </summary>
    Move,

    /// <summary>
    /// Drive to the ToStation, which is a charger and start charging
    /// </summary>
    Charge
}