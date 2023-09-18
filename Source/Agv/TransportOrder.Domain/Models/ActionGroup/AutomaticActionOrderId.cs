namespace Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.ActionGroup;

/// <summary>
/// Represents the different types of order ids
/// that are generated automatically by transport order.
/// </summary>
public enum AutomaticActionOrderId
{
    /// <summary>
    /// Automatic park action.
    /// </summary>
    AutomaticPark,

    /// <summary>
    /// Automatic charge action.
    /// </summary>
    AutomaticCharge,

    /// <summary>
    /// Automatic maintenance action.
    /// </summary>
    AutomaticMaintenance
}