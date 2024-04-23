using Gebhardt.Acc.Common.Domain.Models.Graphs;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.Services.ActionGroup;

/// <summary>
/// Providing information about maintenance requests for Agvs
/// </summary>
public interface IMaintenanceService
{
    /// <summary>
    /// Determines if the Agv needs maintenance
    /// </summary>
    /// <param name="agv"></param>
    /// <returns>True in case it needs maintenance</returns>
    [PublicAPI]
    public bool MaintenanceRequested(Models.Agv.Agv agv);

    /// <summary>
    /// Gets Maintenance Node for Agv
    /// </summary>
    /// <param name="agv"></param>
    /// <returns></returns>
    [PublicAPI]
    public Task<GraphNode> GetMaintenanceNode(Models.Agv.Agv agv);
}