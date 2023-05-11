using Gebhardt.StoreWare.Agv.Common.Messages.Actions;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Dtos
{
    /// <summary>
    /// Count of AgvGroup in specific state
    /// </summary>
    /// <param name="State"></param>
    /// <param name="Count">Number of actions groups in state</param>
    public record AgvGroupCountByStateDto(ActionGroupState State, int Count);
}