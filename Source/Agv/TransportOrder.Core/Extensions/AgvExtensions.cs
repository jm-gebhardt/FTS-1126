using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Agvs;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Extensions;

/// <summary>
/// Extension methods for <see cref="IAgv"/>
/// </summary>
public static class AgvExtensions
{
    /// <summary>
    /// Get Idle Agvs
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static IQueryable<IAgv> IdleOnly(this IQueryable<IAgv> value) => value.Where(x =>  x.CurrentState == nameof(AgvStateMachine.Idle));
}