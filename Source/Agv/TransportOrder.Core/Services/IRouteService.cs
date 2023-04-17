using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services;

/// <summary>
/// Service for computation of Route for Agv
/// </summary>
public interface IRouteService
{
    /// <summary>
    /// Computes if Agv has route for source to destination to check if Agv can handle route
    /// </summary>
    /// <param name="agv">The agv to calculate the route for</param>
    /// <param name="destination">The destination node</param>
    /// <param name="costs">The costs for the found route. Null if the method returns false</param>
    public bool HasRoute(IAgv agv, string destination, out float? costs);
}