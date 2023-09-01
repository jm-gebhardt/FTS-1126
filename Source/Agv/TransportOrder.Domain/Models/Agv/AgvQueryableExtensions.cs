using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;

namespace TransportOrder.Domain.Models.Agv;

/// <summary>
/// Extension methods for <see cref="IAgv"/>
/// </summary>
public static class AgvQueryableExtensions
{
    /// <summary>
    /// Get Idle Agvs
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static IQueryable<IAgv> IdleOnly(this IQueryable<IAgv> value) => value.Where(x =>  x.CurrentState == nameof(AgvState.Idle));
}