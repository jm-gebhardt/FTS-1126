using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Dtos
{
    /// <summary>
    /// Action Group model
    /// </summary>
    /// <param name="CorrelationId"></param>
    /// <param name="OrderId"></param>
    /// <param name="AssignedAgv">Assigned Agv</param>
    /// <param name="CreatedDate">Creation date</param>
    /// <param name="StartedDate">Start date</param>
    /// <param name="CompletedDate">Completion date</param>
    /// <param name="State">State of the actionGroup</param>
    /// <param name="Actions">List of actions</param>
    public record ActionGroupDto(
        Guid CorrelationId,
        string OrderId,
        Guid? AssignedAgv,
        DateTime CreatedDate,
        DateTime? StartedDate,
        DateTime? CompletedDate,
        ActionGroupState State,
        ICollection<AgvActionDto> Actions
    ) : CorrelatedBy<Guid>;
}
