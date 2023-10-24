using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.AgvActions;

/// <summary>
/// Represents the sorting direction
/// </summary>
public enum SortDirection
{
    /// <summary>
    /// Descending
    /// </summary>
    Desc,

    /// <summary>
    /// Ascending
    /// </summary>
    [UsedImplicitly]
    Asc
}