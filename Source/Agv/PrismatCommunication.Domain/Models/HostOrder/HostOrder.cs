using System.Diagnostics.CodeAnalysis;
using Gebhardt.Acc.Common.Domain.Exceptions;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using JetBrains.Annotations;

namespace Gebhardt.Acc.PrismatCommunication.Domain.Models.HostOrder;

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
    /// The assigned agv.
    /// </summary>
    public Guid? AssignedAgv { get; private set; }

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
    public HostOrder(string orderId, string hu, DateTime createdDate)
    {
        OrderId = orderId;
        Hu = hu;
        CreatedDate = createdDate;
        CorrelationId = Guid.NewGuid();
    }

    /// <inheritdoc cref="HostOrder"/>>
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
}