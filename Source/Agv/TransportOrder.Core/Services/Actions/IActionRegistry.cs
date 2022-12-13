using Gebhardt.StoreWare.Agv.Common.Messages.Actions;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Actions;

/// <summary>
/// 
/// </summary>
public interface IActionRegistry
{
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    IList<AgvAction> GetAllPendingActions();

}