using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Dtos
{
    /// <summary>
    /// AgvAction Group model
    /// </summary>
    /// <param name="CorrelationId"></param>
    /// <param name="State">State of the actionGroup</param>
    /// <param name="Created">Creation date</param>
    /// <param name="Started">Start date</param>
    /// <param name="Ended">End date</param>
    /// /// <param name="AssignedAgv">Assigned Agv</param>
    /// <param name="Actions">List of actions</param>
    public record AgvActionGroupDto(
        Guid CorrelationId,
        ActionGroupState State,
        DateTime Created,
        DateTime? Started,
        DateTime? Ended,
        Guid? AssignedAgv, 
        IEnumerable<AgvActionDto> Actions
    ) : CorrelatedBy<Guid>;
}
