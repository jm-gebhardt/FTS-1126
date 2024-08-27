using System.Diagnostics.CodeAnalysis;
using Gebhardt.Acc.Common.Messages;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.PrismatCommunication.Domain.Exceptions;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.KaeserGraph;
using JetBrains.Annotations;

namespace Gebhardt.Acc.PrismatCommunication.Domain.Models.HostOrder;

/// <summary>
/// Represents an incoming host order
/// </summary>
public class HostOrder
{
    private readonly List<Action> _actions = new();

    /// <summary>
    /// The correlation id order the host order /action group
    /// </summary>
    public Guid CorrelationId { get; init; }

    /// <summary>
    /// The order id as given from the host.
    /// </summary>
    public string OrderId { get; init; }

    /// <summary>
    /// The handling unit as given by the host.
    /// </summary>
    public string Hu { get; private set; }

    /// <summary>
    /// The actions of the order.
    /// </summary>
    public IReadOnlyCollection<Action> Actions => _actions;

    /// <summary>
    /// The time when the order was started.
    /// </summary>
    public DateTime CreatedDate { get; init; }

    /// <summary>
    /// The time when the order was started.
    /// </summary>
    public DateTime? StartedDate { get; private set; }

    /// <summary>
    /// The time when the order was canceled.
    /// </summary>
    public DateTime? CanceledDate { get; private set; }

    /// <summary>
    /// The time when the order was finished.
    /// </summary>
    public DateTime? FinishedDate { get; private set; }

    /// <summary>
    /// The time when the order failed.
    /// </summary>
    public DateTime? FailedDate { get; private set; }

    /// <summary>
    /// True, if the host order passed a trigger point.
    /// </summary>
    public bool PassedTriggerPoint { get; private set; }

    /// <summary>
    /// The assigned agv.
    /// </summary>
    public Guid? AssignedAgv { get; private set; }

    /// <summary>
    /// Initializes a new <see cref="HostOrder"/> instance.
    /// </summary>
    public HostOrder(Guid correlationId, string orderId, string hu, DateTime createdDate)
    {
        CorrelationId = correlationId;
        OrderId = orderId;
        Hu = hu;
        CreatedDate = createdDate;
    }

    [UsedImplicitly]
    [ExcludeFromCodeCoverage]
    private HostOrder()
    {
        CorrelationId = Guid.NewGuid();
        OrderId = string.Empty;
        Hu = string.Empty;
    }

    /// <summary>
    /// Add an action to the order.
    /// </summary>
    /// <param name="node">The node of the action.</param>
    /// <param name="actionType">The action type.</param>
    /// <param name="infoToAgv">Information that will be sent to the agv.</param>
    public void AddAction(string node, ActionType actionType, Dictionary<InfoToAgvKey, string>? infoToAgv = null)
    {
        var action = new Action(Guid.NewGuid(), CorrelationId, node, actionType, _actions.Count, infoToAgv);
        _actions.Add(action);
    }

    /// <summary>
    /// Start the order.
    /// </summary>
    [PublicAPI]
    public void Start(DateTime timestamp, Guid agv)
    {
        if (timestamp < StartedDate || timestamp < FinishedDate || timestamp < CanceledDate)
        {
            throw new HostOrderInformationOutdatedException($"Order start information from {timestamp} outdated, already started {StartedDate} / finished {FinishedDate}/ canceled {CanceledDate}.");
        }
        StartedDate = timestamp;
        AssignedAgv = agv;
    }

    /// <summary>
    /// Cancel the order.
    /// </summary>
    [PublicAPI]
    public void Cancel(DateTime timestamp)
    {
        if (timestamp < StartedDate || timestamp < FinishedDate || timestamp < CanceledDate)
        {
            throw new HostOrderInformationOutdatedException($"Order cancellation information from {timestamp} outdated, already started {StartedDate} / finished {FinishedDate}/ canceled {CanceledDate}.");
        }
        CanceledDate = timestamp;
    }

    /// <summary>
    /// Finish the order.
    /// </summary>
    [PublicAPI]
    public void Finish(DateTime timestamp)
    {
        if (timestamp < StartedDate || timestamp < FinishedDate || timestamp < CanceledDate)
        {
            throw new HostOrderInformationOutdatedException($"Order completion information from {timestamp} outdated, already started {StartedDate} / finished {FinishedDate}/ canceled {CanceledDate}.");
        }
        FinishedDate = timestamp;
    }

    /// <summary>
    /// Handle order failure.
    /// </summary>
    [PublicAPI]
    public void HandleFailure(DateTime timestamp)
    {
        if (timestamp < FinishedDate || timestamp < CanceledDate || timestamp < FailedDate)
        {
            throw new HostOrderInformationOutdatedException($"Order completion information from {timestamp} outdated, already finished {FinishedDate}/ canceled {CanceledDate} / failed {FailedDate}.");
        }
        FailedDate = timestamp;
    }

    /// <summary>
    /// Pass trigger point.
    /// </summary>
    public void PassTriggerPoint()
    {
        PassedTriggerPoint = true;
    }

    /// <summary>
    /// Try to create an action group with a pick and a drop action from the host order.
    /// </summary>
    /// <param name="configuration">The graph configuration.</param>
    /// <param name="orderDetails">The details of the host order.</param>
    /// <returns>Action group with pick and drop actions.</returns>
    public ActionGroup? TryCreatePickDropActionGroup(KaeserGraphConfiguration configuration, OrderDetails orderDetails)
    {
        bool canCreatePickDropActionGroup = Actions.All(a => configuration.PrismatKnownTargets.Contains(a.Node));
        if (canCreatePickDropActionGroup)
        {
            return new ActionGroup(CorrelationId,
                OrderId,
                Actions.Select(a => new AgvAction(a.CorrelationId, a.ActionType, configuration.StationMapping[a.Node], CorrelationId, a.InfoToAgv)
                    { Sequence = a.SequenceNumber }).ToList(),
                orderDetails.Creator.ConvertToActionGroupCreator(),
                orderDetails.CreationDate);
        }
        return null;
    }

    /// <summary>
    /// Try to create an action group with a move, pick, and a drop action from the host order.
    /// </summary>
    /// <param name="configuration">The graph configuration.</param>
    /// <param name="orderDetails">The details of the host order.</param>
    /// <returns>Action group with move, pick, drop actions, whereby the pick action has no target.</returns>
    public ActionGroup CreateMovePickDropActionGroup(KaeserGraphConfiguration configuration, OrderDetails orderDetails)
    {
        return new ActionGroup(
                CorrelationId, 
                OrderId,
                [
                    new AgvAction(Guid.NewGuid(), ActionType.Move, configuration.TriggerPointMapping.ConcurrentDict[configuration.DecisionPoint1TriggerName], CorrelationId),
                    Actions.Where(a => a.ActionType == ActionType.Pick).Select( a => new AgvAction(a.CorrelationId, a.ActionType, "", CorrelationId, a.InfoToAgv){Sequence = 1} ).First(),
                    Actions.Where(a => a.ActionType == ActionType.Drop).Select( a => new AgvAction(a.CorrelationId, a.ActionType, configuration.StationMapping[a.Node] , CorrelationId, a.InfoToAgv){Sequence = 2} ).First()
                ], 
                orderDetails.Creator.ConvertToActionGroupCreator(), 
                orderDetails.CreationDate);
    }

    /// <summary>
    /// Try to create an action group with a move, pick, and a drop action from the host order.
    /// </summary>
    /// <param name="configuration">The graph configuration.</param>
    /// <param name="assignedAgv">The assigned agv.</param>
    /// <returns>Action group with move, pick, drop actions, whereby the drop action has no target.</returns>
    public ActionGroup CreatePickMoveDropActionGroup(KaeserGraphConfiguration configuration, Guid? assignedAgv = null)
    {
        if (assignedAgv != null)
        {
            AssignedAgv = assignedAgv;
            return new ActionGroup(
                CorrelationId,
                OrderId,
                [
                    Actions.Where(a => a.ActionType == ActionType.Pick).Select( a => new AgvAction(a.CorrelationId, a.ActionType, configuration.StationMapping[a.Node] , CorrelationId, a.InfoToAgv){Sequence = 0} ).First(),
                    new AgvAction(Guid.NewGuid(), ActionType.Move, configuration.TriggerPointMapping.ConcurrentDict[configuration.DecisionPoint1TriggerName], CorrelationId){Sequence = 1},
                    Actions.Where(a => a.ActionType == ActionType.Drop).Select( a => new AgvAction(a.CorrelationId, a.ActionType, "", CorrelationId, a.InfoToAgv){Sequence = 2} ).First()
                ],
                ActionGroupCreator.Wms,
                CreatedDate){AssignedAgv = assignedAgv};
        }
        return new ActionGroup(
            CorrelationId,
            OrderId,
            [
                Actions.Where(a => a.ActionType == ActionType.Pick).Select( a => new AgvAction(a.CorrelationId, a.ActionType, configuration.StationMapping[a.Node] , CorrelationId, a.InfoToAgv){Sequence = 0} ).First(),
                new AgvAction(Guid.NewGuid(), ActionType.Move, configuration.TriggerPointMapping.ConcurrentDict[configuration.DecisionPoint1TriggerName], CorrelationId){Sequence = 1},
                Actions.Where(a => a.ActionType == ActionType.Drop).Select( a => new AgvAction(a.CorrelationId, a.ActionType, "", CorrelationId, a.InfoToAgv){Sequence = 2} ).First()
            ],
            ActionGroupCreator.Wms,
            CreatedDate);
    }

    /// <summary>
    /// Creates an action group from the given pick node to the target of the order for
    /// the assigned agv.
    /// </summary>
    /// <param name="pickNode">The pick node.</param>
    /// <param name="agv">Optional correlation id of the agv.</param>
    /// <param name="configuration">The graph configuration</param>
    /// <returns>ActionGroup with pick and drop actions, assigned to the given agv.</returns>
    public ActionGroup CreatePickDropActionGroupForAgv(string pickNode, Guid? agv, KaeserGraphConfiguration configuration)
    {
        AssignedAgv = agv;
        return new ActionGroup(CorrelationId,
            OrderId,
            [
                Actions.Where(a => a.ActionType == ActionType.Pick).Select( a => new AgvAction(a.CorrelationId, a.ActionType,  pickNode, CorrelationId, a.InfoToAgv){Sequence = 0} ).First(),
                Actions.Where(a => a.ActionType == ActionType.Drop).Select( a => new AgvAction(a.CorrelationId, a.ActionType, configuration.StationMapping[a.Node], CorrelationId, a.InfoToAgv){Sequence = 1} ).First()
            ],
            ActionGroupCreator.Wms,
            DateTime.UtcNow){AssignedAgv = agv};
    }

    /// <summary>
    /// Checks if the host order has an unknown drop target.
    /// </summary>
    /// <param name="configuration">The Kaeser graph configuration.</param>
    public bool HasUnknownDropTarget(KaeserGraphConfiguration configuration)
    {
        return Actions.All(a => configuration.PrismatIncompleteDropTargets.Contains(a.Node));
    }

    /// <summary>
    /// Checks if the host order has an unknown pick target.
    /// </summary>
    /// <param name="configuration">The Kaeser graph configuration.</param>
    public bool HasUnknownPickTarget(KaeserGraphConfiguration configuration)
    {
        return Actions.All(a => configuration.PrismatIncompletePickTargets.Contains(a.Node));
    }
}