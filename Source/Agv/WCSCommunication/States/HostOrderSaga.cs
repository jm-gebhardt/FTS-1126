using System.Text.Json.Serialization;
using Gebhardt.StoreWare.Agv.Common.Messages;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;
using Gebhardt.StoreWare.Agv.Common.Messages.HostOrder.Events;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.WCSCommunication.States;

/// <summary>
/// Represents an host order as a state machine saga.
/// </summary>
public class HostOrderSaga : SagaStateMachineInstance
{
    /// <summary>
    /// Correlated by GUID of OrderId
    /// </summary>
    public Guid CorrelationId { get; set; }

    /// <summary>
    /// Redundant OrderId info
    /// </summary>
    public string OrderId { get; set; }

    /// <summary>
    /// Current state
    /// </summary>
    [UsedImplicitly]
    public string CurrentState { get; set; }

    /// <summary>
    /// Order details
    /// </summary>
    public OrderDetails OrderDetails { get; set; } = null!;

    /// <summary>
    /// Pick Action's GUID
    /// </summary>
    public Guid PickActionGuid { get; } = Guid.NewGuid();

    /// <summary>
    /// Drop Action's GUID
    /// </summary>
    public Guid DropActionGuid { get; } = Guid.NewGuid();

    /// <summary>
    /// The AGV assigned to the  action group of the saga.
    /// </summary>
    public string? AssignedAgv { get; private set; }

    /// <summary>
    /// The error that occurred during handling failure.
    /// </summary>
    public HostOrderHandlingFailedErrorCode HandlingError { get; set; }


    /// <summary>
    /// The error that occurred during cancellation failure.
    /// </summary>
    public HostOrderCancelFailedErrorCode CancellationError { get; set; }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="orderDetails">Order details</param>
    /// <param name="currentState">Order state</param>
    public HostOrderSaga(OrderDetails orderDetails,
        string? currentState = null)
    {
        CorrelationId = orderDetails.CorrelationId;
        OrderId = orderDetails.OrderId;
        CurrentState = currentState ?? default!;
        OrderDetails = orderDetails;
    }

    /// <summary>
    /// Private constructor for Entity Framework
    /// </summary>
    /// <param name="correlationId">Correlated by GUID of OrderId</param>
    /// <param name="orderId">Redundant OrderId info</param>
    /// <param name="currentState">Order state</param>
    /// <param name="pickActionGuid">Pick Action GUID</param>
    /// <param name="dropActionGuid">Drop Action GUID</param>
    private HostOrderSaga(Guid correlationId, string orderId, string currentState, Guid pickActionGuid, Guid dropActionGuid)
    {
        CorrelationId = correlationId;
        OrderId = orderId;
        CurrentState = currentState;
        PickActionGuid = pickActionGuid;
        DropActionGuid = dropActionGuid;
    }

    /// <summary>
    /// Private constructor for Entity Framework used to restore an instance from persistence
    /// </summary>
    /// <param name="correlationId">Correlated by GUID of OrderId</param>
    /// <param name="orderId">Redundant OrderId info</param>
    /// <param name="currentState">Order state</param>
    /// <param name="orderDetails">Order details</param>
    [JsonConstructor]
    private HostOrderSaga(Guid correlationId, string orderId, string currentState, OrderDetails orderDetails)
    {
        CorrelationId = correlationId;
        OrderId = orderId;
        CurrentState = currentState;
        OrderDetails = orderDetails;
    }

    /// <summary>
    /// Get a new pick drop action group.
    /// </summary>
    /// <returns></returns>
    public ActionGroup GetPickDropActionGroup()
    {
        var actionGroupId = CorrelationId;
        var actionGroupCreator = OrderDetails.Creator.ConvertToActionGroupCreator();

        var pickAction = new AgvAction(PickActionGuid, ActionType.Pick, OrderDetails.Source, actionGroupId)
        {
            Sequence = 0
        };
        var dropAction = new AgvAction(DropActionGuid, ActionType.Drop, OrderDetails.Destination, actionGroupId)
        {
            Sequence = 1
        };

        var actionGroup = new ActionGroup(CorrelationId, OrderId, new List<AgvAction> { pickAction, dropAction }, actionGroupCreator, OrderDetails.CreationDate);

        return actionGroup;
    }

    /// <summary>
    /// Assign an agv once started.
    /// </summary>
    /// <param name="agv"></param>
    public void AssignAgv(Guid? agv)
    {
        if (agv == null)
        {
            return;
        }
        AssignedAgv = agv.ToString();
    }

}