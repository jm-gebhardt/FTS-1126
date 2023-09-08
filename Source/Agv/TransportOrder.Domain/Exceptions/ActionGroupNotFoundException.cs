using Gebhardt.StoreWare.Agv.Common.Exceptions;
using ActionGroup = TransportOrder.Domain.Models.ActionGroup.ActionGroup;

namespace TransportOrder.Domain.Exceptions;

/// <summary>
/// ActionGroup was not found
/// </summary>
public class ActionGroupNotFoundException : NotFoundException
{
    /// <inheritdoc cref="ActionGroupNotFoundException"/>
    /// <param name="actionGroupCorrelationId"></param>
    public ActionGroupNotFoundException(Guid actionGroupCorrelationId) 
        : base(nameof(ActionGroup), "ActionGroupCorrelationId", actionGroupCorrelationId)
    {
    }
}