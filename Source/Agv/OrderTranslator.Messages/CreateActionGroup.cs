using Gebhardt.Acc.Common.Messages;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.OrderTranslator.Messages;

/// <summary>
/// Provides a command to create a host order.
/// </summary>
[UsedImplicitly]
public record CreateActionGroup : CorrelatedBy<Guid>
{
    /// <summary>
    /// The correlation id of the host order.
    /// </summary>
    public Guid CorrelationId { get; }

    /// <summary>
    /// Assigned AGV that needs to execute the ActionGroup.
    /// </summary>
    public Guid? AssignedAgv { get; }

    /// <summary>
    /// Order details.
    /// </summary>
    public OrderDetails OrderDetails { get; }

    /// <summary>
    /// Handling unit.
    /// </summary>
    public string Hu { get; }

    /// <inheritdoc cref="CreateActionGroup"/>
    public CreateActionGroup(string hu, OrderDetails orderDetails, Guid? assignedAgv = null)
    {
        CorrelationId = orderDetails.CorrelationId;
        OrderDetails = orderDetails;
        AssignedAgv = assignedAgv;
        Hu = hu;
    }
}