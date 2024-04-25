using Gebhardt.Acc.Common.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup;

namespace Gebhardt.Acc.TransportOrder.Domain.Exceptions;

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