using System.Diagnostics.CodeAnalysis;
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
    /// The correlation id of the host order
    /// </summary>
    public Guid CorrelationId { get; init; }

    /// <summary>
    /// The order id as given from the host.
    /// </summary>
    public string OrderId { get; init; }

    /// <summary>
    /// The handling unit as given by the host.
    /// </summary>
    public string Hu { get; init; }

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
    /// True, if the host order has been queued.
    /// </summary>
    public bool IsQueued { get; set; }

    /// <summary>
    /// The assigned agv.
    /// </summary>
    public Guid? AssignedAgv { get; private set; }

    /// <summary>
    /// The order id as used internally
    /// </summary>
    public string TransportOrderId => GetTransportOrderId(Hu);

    /// <summary>
    /// The host order correlation id that belongs to the Doppelspiel (either pick or drop partner).
    /// </summary>
    public Guid? DoppelspielPartner { get; private set; }

    /// <summary>
    /// True, if the order is registered for a Doppelspiel
    /// </summary>
    public bool IsRegisteredForDoppelspiel { get; private set; }

    /// <summary>
    /// True, if the orders Doppelspiel partner has been queued yet.
    /// </summary>
    public bool HasQueuedDoppelspielpartner => IsRegisteredForDoppelspiel && DoppelspielPartner != null;

    /// <summary>
    /// The source of the host order.
    /// </summary>
    public string Source => Actions.First(a => a.ActionType == ActionType.Pick).Node;

    /// <summary>
    /// The target of the host order.
    /// </summary>
    public string Target => Actions.First(a => a.ActionType == ActionType.Drop).Node;

    /// <summary>
    /// Initializes a new <see cref="HostOrder"/> instance.
    /// </summary>
    public HostOrder(string orderId, string hu, DateTime createdDate)
    {
        OrderId = orderId;
        Hu = hu;
        CreatedDate = createdDate;
        CorrelationId = Guid.NewGuid();
    }

    [UsedImplicitly]
    [ExcludeFromCodeCoverage]
    private HostOrder()
    {
        CreatedDate = DateTime.UtcNow;
        Hu = string.Empty;
        OrderId = string.Empty;
        CorrelationId = Guid.NewGuid();
    }

    /// <summary>
    /// Get the id of a transport order corresponding to the host order.
    /// </summary>
    /// <param name="hu">The handling unit.</param>
    public static string GetTransportOrderId(string hu)
    {
        return $"HU({hu})";
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
    /// Register order for Doppelspiel.
    /// </summary>
    public void RegisterForDoppelspiel()
    {
        IsRegisteredForDoppelspiel = true;
    }

    /// <summary>
    /// Unregister order from Doppelspiel.
    /// </summary>
    public void UnRegisterFromDoppelspiel()
    {
        IsRegisteredForDoppelspiel = false;
    }

    /// <summary>
    /// Register order as Doppelspiel partner.
    /// </summary>
    public void SetDoppelspielpartner(Guid partnerOrder)
    {
        DoppelspielPartner = partnerOrder;
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
        FinishAction(timestamp, ActionType.Drop);
    }

    /// <summary>
    /// Finish action of given type.
    /// </summary>
    /// <param name="timestamp">The time the action finished.</param>
    /// <param name="type">The type of the action.</param>
    public void FinishAction(DateTime timestamp, ActionType type)
    {
        var action = Actions.Single(a => a.ActionType == type);
        action.Finish(timestamp);
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
    /// True, if a host order runs on the short circuit.
    /// </summary>
    /// <param name="configuration">The Kaeser graph configuration.</param>
    public bool IsShortCircuitOrder(KaeserGraphConfiguration configuration)
    {
        return Actions.All(a => configuration.PrismatKnownTargets.Contains(a.Node));
    }

    /// <summary>
    /// Create an action group from the host order.
    /// </summary>
    /// <param name="configuration">The graph configuration.</param>
    /// <param name="assignedAgv">The assigned agv, if any.</param>
    /// <param name="pickNode">The pick node, if given after Doppelspiel.</param>
    /// <returns>The action group.</returns>
    public ActionGroup CreateActionGroup(KaeserGraphConfiguration configuration, Guid? assignedAgv = null, string? pickNode = null)
    {
        if (pickNode != null && HasUnknownPickTarget(configuration))
        {
            // Doppelspiel at APL
            return CreatePickDropActionGroupForAgv(pickNode, assignedAgv, configuration);
        }

        if (pickNode != null && IsShortCircuitOrder(configuration) && !configuration.IsPickAtAkl(Source))
        {
            // Doppelspiel at PSX for AKL drop
            return CreatePickDropActionGroupForAgv(pickNode, assignedAgv, configuration);
        }

        if (HasUnknownPickTarget(configuration))
        {
            // PSX-> APL
            return CreateMovePickDropActionGroup(configuration, configuration.LongLoopMoveTargetNode, assignedAgv: assignedAgv);
        }

        if (HasUnknownDropTarget(configuration))
        {
            // APL-> PSX
            return CreatePickMoveDropActionGroup(configuration, configuration.LongLoopMoveTargetNode, assignedAgv: assignedAgv);
        }

        if (IsShortCircuitOrder(configuration) && configuration.IsPickAtAkl(Source))
        {
            // AKL -> PSX
            return CreatePickMoveDropActionGroup(configuration, configuration.WaitingPositionByAklSource[Source], assignedAgv: assignedAgv);
        }

        // PSX-> AKL
        return CreateMovePickDropActionGroup(configuration, configuration.WaitingPositionByAklTarget[Target], assignedAgv: assignedAgv);
    }

    /// <summary>
    /// Try to create an action group with a move, pick, and a drop action from the host order.
    /// </summary>
    /// <param name="configuration">The graph configuration.</param>
    /// <param name="moveTarget">The target of the move action.</param>
    /// <param name="assignedAgv">The agv that should run the order.</param>
    /// <returns>Action group with move, pick, drop actions, whereby the pick action has no target.</returns>
    private ActionGroup CreateMovePickDropActionGroup(KaeserGraphConfiguration configuration, string moveTarget, Guid? assignedAgv = null)
    {
        return new ActionGroup(
            CorrelationId,
            TransportOrderId,
            [
                new AgvAction(Guid.NewGuid(), ActionType.Move, moveTarget, CorrelationId),
                Actions.Where(a => a.ActionType == ActionType.Pick).Select(a => new AgvAction(a.CorrelationId, a.ActionType, "", CorrelationId, a.InfoToAgv) { Sequence = 1 }).First(),
                Actions.Where(a => a.ActionType == ActionType.Drop).Select(a => new AgvAction(a.CorrelationId, a.ActionType, configuration.StationMapping[a.Node], CorrelationId, a.InfoToAgv) { Sequence = 2 }).First()
            ],
            ActionGroupCreator.Wms,
            CreatedDate,
            assignedAgv) { HandlingUnit = Hu };
    }

    /// <summary>
    /// Try to create an action group with a move, pick, and a drop action from the host order.
    /// </summary>
    /// <param name="configuration">The graph configuration.</param>
    /// <param name="assignedAgv">The assigned agv.</param>
    /// <param name="moveTarget">The target of the move action.</param>
    /// <returns>Action group with move, pick, drop actions, whereby the drop action has no target.</returns>
    private ActionGroup CreatePickMoveDropActionGroup(KaeserGraphConfiguration configuration, string moveTarget, Guid? assignedAgv = null)
    {
        return new ActionGroup(
            CorrelationId,
            TransportOrderId,
            [
                Actions.Where(a => a.ActionType == ActionType.Pick).Select(a => new AgvAction(a.CorrelationId, a.ActionType, configuration.StationMapping[a.Node], CorrelationId, a.InfoToAgv) { Sequence = 0 }).First(),
                new AgvAction(Guid.NewGuid(), ActionType.Move, moveTarget, CorrelationId) { Sequence = 1 },
                Actions.Where(a => a.ActionType == ActionType.Drop).Select(a => new AgvAction(a.CorrelationId, a.ActionType, "", CorrelationId, a.InfoToAgv) { Sequence = 2 }).First()
            ],
            ActionGroupCreator.Wms,
            CreatedDate) { AssignedAgv = assignedAgv, HandlingUnit = Hu };
    }

    /// <summary>
    /// Creates an action group from the given pick node to the target of the order for
    /// the assigned agv.
    /// </summary>
    /// <param name="pickNode">The pick node.</param>
    /// <param name="agv">Optional correlation id of the agv.</param>
    /// <param name="configuration">The graph configuration</param>
    /// <returns>ActionGroup with pick and drop actions, assigned to the given agv.</returns>
    private ActionGroup CreatePickDropActionGroupForAgv(string pickNode, Guid? agv, KaeserGraphConfiguration configuration)
    {
        AssignedAgv = agv;
        return new ActionGroup(CorrelationId,
            TransportOrderId,
            [
                Actions.Where(a => a.ActionType == ActionType.Pick).Select(a => new AgvAction(a.CorrelationId, a.ActionType, pickNode, CorrelationId, a.InfoToAgv) { Sequence = 0 }).First(),
                Actions.Where(a => a.ActionType == ActionType.Drop).Select(a => new AgvAction(a.CorrelationId, a.ActionType, configuration.StationMapping[a.Node], CorrelationId, a.InfoToAgv) { Sequence = 1 }).First()
            ],
            ActionGroupCreator.Wms,
            CreatedDate) { AssignedAgv = agv, HandlingUnit = Hu };
    }

    /// <summary>
    /// Checks if the host order has an unknown drop target.
    /// </summary>
    /// <param name="configuration">The Kaeser graph configuration.</param>
    public bool HasUnknownDropTarget(KaeserGraphConfiguration configuration)
    {
        bool isAplSource = Actions.Single(a => a.ActionType == ActionType.Pick).Node == configuration.PrismatAplSource;
        bool isUnknownDrop = configuration.PrismatOutputLanes.Contains(Actions.Single(a => a.ActionType == ActionType.Drop).Node);
        return isAplSource && isUnknownDrop;
    }

    /// <summary>
    /// Checks if the host order has an unknown pick target.
    /// </summary>
    /// <param name="configuration">The Kaeser graph configuration.</param>
    public bool HasUnknownPickTarget(KaeserGraphConfiguration configuration)
    {
        bool isAplTarget = Actions.Single(a => a.ActionType == ActionType.Drop).Node == configuration.PrismatAplTarget;
        bool isUnknownPick = configuration.PrismatInputLanes.Contains(Actions.Single(a => a.ActionType == ActionType.Pick).Node);
        return isUnknownPick && isAplTarget;
    }
}