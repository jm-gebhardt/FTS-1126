using System.Diagnostics.CodeAnalysis;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using JetBrains.Annotations;

namespace Gebhardt.Acc.OrderTranslator.Domain.Models.HostOrder;

/// <summary>
/// Represents an incoming host order
/// </summary>
[PublicAPI]
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
    /// The order id as used internally
    /// </summary>
    public string TransportOrderId => GetTransportOrderId(Hu);

    /// <summary>
    /// The drop of the order.
    /// </summary>
    public Action? Drop => Actions.FirstOrDefault(a => a.ActionType == ActionType.Drop);

    /// <summary>
    /// The pick of the order.
    /// </summary>
    public Action? Pick => Actions.FirstOrDefault(a => a.ActionType == ActionType.Pick);

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
    public HostOrder(string orderId, string hu, DateTime createdDate) : this(Guid.NewGuid(), orderId, hu, createdDate)
    {
    }

    /// <summary>
    /// Initializes a new <see cref="HostOrder"/> instance.
    /// </summary>
    public HostOrder(Guid correlationId, string orderId, string hu, DateTime createdDate)
    {
        OrderId = orderId;
        Hu = hu;
        CreatedDate = createdDate;
        CorrelationId = correlationId;
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
    /// Create an action group based on the host order.
    /// </summary>
    /// <param name="pickStations">The pick stations connected to the source conveyor lane.</param>
    /// <param name="dropStations">The drop stations connected to the target conveyor lane</param>
    /// <returns>An action group</returns>
    public ActionGroup? CreateActionGroup(ICollection<Station.Station> pickStations, ICollection<Station.Station> dropStations)
    {
        if (pickStations.Count == 1 && dropStations.Count == 1)
        {
            return CreatePickDropActionGroup(pickStations.First(), dropStations.First());
        }

        if (pickStations.Count == 1 && dropStations.Count > 1)
        {
            return CreatePickMoveDropActionGroup(pickStations.First(), dropStations);
        }

        if (pickStations.Count >= 1 && dropStations.Count == 1)
        {
            return CreateMovePickDropActionGroup(pickStations, dropStations.First());
        }
        return null;
    }

    private ActionGroup CreatePickDropActionGroup(Station.Station pickStation, Station.Station dropStation)
    {
        bool canRunDoppelspiel = dropStation.DropDoppelspielMode is not (DoppelspielMode.None or DoppelspielMode.OrderOrQueueHead);
        DateTime? consideredForDoppelspielDate = canRunDoppelspiel ? DateTime.UtcNow : null;
        return new ActionGroup(
            CorrelationId,
            TransportOrderId,
            [
                Actions.Where(a => a.ActionType == ActionType.Pick).Select(a => new AgvAction(a.CorrelationId, a.ActionType, new ActionTarget(pickStation.Name.ToGraphNode(), null, [], pickStation.PickDoppelspielMode), CorrelationId, a.InfoToAgv) { Sequence = 0 }).First(),
                Actions.Where(a => a.ActionType == ActionType.Drop).Select(a => new AgvAction(a.CorrelationId, a.ActionType, new ActionTarget(dropStation.Name.ToGraphNode(), null, [], dropStation.DropDoppelspielMode), CorrelationId, a.InfoToAgv) { Sequence = 1 }).First()
            ],
            ActionGroupCreator.Wms,
            Priority.Default,
            false,
            CreatedDate)
        { HandlingUnit = Hu, CanRunDoppelspiel = canRunDoppelspiel, ConsideredForDoppelspielDate = consideredForDoppelspielDate};
    }

    private ActionGroup CreatePickMoveDropActionGroup(Station.Station pickStation, ICollection<Station.Station> dropStations)
    {
        var dropOption = dropStations.First();
        string moveTarget = dropOption.DropRouting.First(r => r.Station == pickStation.Name).MoveTarget;
        bool canRunDoppelspiel = dropOption.DropDoppelspielMode is not (DoppelspielMode.None or DoppelspielMode.OrderOrQueueHead);
        DateTime? consideredForDoppelspielDate = canRunDoppelspiel ? DateTime.UtcNow : null;
        return new ActionGroup(
                CorrelationId,
                TransportOrderId,
                [
                    Actions.Where(a => a.ActionType == ActionType.Pick).Select(a => new AgvAction(a.CorrelationId, a.ActionType, new ActionTarget(pickStation.Name.ToGraphNode(), null, [], pickStation.PickDoppelspielMode), CorrelationId, a.InfoToAgv) { Sequence = 0 }).First(),
                    new AgvAction(Guid.NewGuid(), ActionType.Move, moveTarget.ToGraphNode(), CorrelationId) { Sequence = 1 },
                    Actions.Where(a => a.ActionType == ActionType.Drop).Select(a => new AgvAction(a.CorrelationId, a.ActionType, new ActionTarget(null, null, dropStations.Select(s => s.Name).ToList(), dropOption.DropDoppelspielMode), CorrelationId, a.InfoToAgv) { Sequence = 2 }).First()
                ],
                ActionGroupCreator.Wms,
                Priority.Default,
                false,
                CreatedDate)
        { HandlingUnit = Hu, CanRunDoppelspiel = canRunDoppelspiel, ConsideredForDoppelspielDate = consideredForDoppelspielDate };
    }

    private ActionGroup CreateMovePickDropActionGroup(ICollection<Station.Station> pickStations, Station.Station dropStation)
    {
        var pickOption = pickStations.First();
        string moveTarget = pickOption.DropRouting.First(r => r.Station == dropStation.Name).MoveTarget;
        bool canRunDoppelspiel = dropStation.DropDoppelspielMode is not (DoppelspielMode.None or DoppelspielMode.OrderOrQueueHead);
        DateTime? consideredForDoppelspielDate = canRunDoppelspiel ? DateTime.UtcNow : null;
        return new ActionGroup(
                CorrelationId,
                TransportOrderId,
                [
                    new AgvAction(Guid.NewGuid(), ActionType.Move, moveTarget.ToGraphNode(), CorrelationId) { Sequence = 0 },
                    Actions.Where(a => a.ActionType == ActionType.Pick).Select(a => new AgvAction(a.CorrelationId, a.ActionType, new ActionTarget(null, null, pickStations.Select(s => s.Name).ToList(), pickOption.PickDoppelspielMode), CorrelationId, a.InfoToAgv) { Sequence = 1 }).First(),
                    Actions.Where(a => a.ActionType == ActionType.Drop).Select(a => new AgvAction(a.CorrelationId, a.ActionType, new ActionTarget(dropStation.Name.ToGraphNode(), null, [], dropStation.DropDoppelspielMode), CorrelationId, a.InfoToAgv) { Sequence = 2 }).First()
                ],
                ActionGroupCreator.Wms,
                Priority.Default,
                false,
                CreatedDate)
            { HandlingUnit = Hu, CanRunDoppelspiel = canRunDoppelspiel, ConsideredForDoppelspielDate = consideredForDoppelspielDate };
    }
}