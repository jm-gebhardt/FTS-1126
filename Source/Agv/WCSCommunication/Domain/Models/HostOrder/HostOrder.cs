using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;
using JetBrains.Annotations;
using System.Diagnostics.CodeAnalysis;
using Gebhardt.StoreWare.Agv.WCSCommunication.Domain.Exceptions;

namespace Gebhardt.StoreWare.Agv.WCSCommunication.Domain.Models.HostOrder;

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
    public string OrderId { get; private set; }

    /// <summary>
    /// The actions of the order.
    /// </summary>
    public IReadOnlyCollection<Action> Actions => _actions;

    /// <summary>
    /// The time when the order was started.
    /// </summary>
    public DateTime? StartedDate { get; private set; }

    /// <summary>
    /// The time when the order was cancelled.
    /// </summary>
    public DateTime? CancelledDate { get; private set; }

    /// <summary>
    /// The time when the order was finished.
    /// </summary>
    public DateTime? FinishedDate { get; private set; }

    /// <summary>
    /// The time when the order failed.
    /// </summary>
    public DateTime? FailedDate { get; private set; }

    /// <summary>
    /// Initializes a new <see cref="HostOrder"/> instance.
    /// </summary>
    public HostOrder(Guid correlationId, string orderId)
    {
        CorrelationId = correlationId;
        OrderId = orderId;
    }

    [UsedImplicitly]
    [ExcludeFromCodeCoverage]
    private HostOrder()
    {
        CorrelationId = Guid.NewGuid();
        OrderId = string.Empty;
    }

    /// <summary>
    /// Add an action to the order.
    /// </summary>
    /// <param name="node">The node of the action.</param>
    /// <param name="actionType">The action type.</param>
    public void AddAction(string node, ActionType actionType)
    {
        var action = new Action(Guid.NewGuid(), CorrelationId, node, actionType, _actions.Count);
        _actions.Add(action);
    }

    /// <summary>
    /// Start the order.
    /// </summary>
    public void Start(DateTime timestamp)
    {
        if (timestamp < StartedDate || timestamp < FinishedDate || timestamp < CancelledDate)
        {
            throw new HostOrderInformationOutdatedException($"Order start information from {timestamp} outdated, already started {StartedDate} / finished {FinishedDate}/ cancelled {CancelledDate}.");
        }
        StartedDate = timestamp;
    }

    /// <summary>
    /// Cancel the order.
    /// </summary>
    public void Cancel(DateTime timestamp)
    {
        if (timestamp < StartedDate || timestamp < FinishedDate || timestamp < CancelledDate)
        {
            throw new HostOrderInformationOutdatedException($"Order cancellation information from {timestamp} outdated, already started {StartedDate} / finished {FinishedDate}/ cancelled {CancelledDate}.");
        }
        CancelledDate = timestamp;
    }

    /// <summary>
    /// Finish the order.
    /// </summary>
    public void Finish(DateTime timestamp)
    {
        if (timestamp < StartedDate || timestamp < FinishedDate || timestamp < CancelledDate)
        {
            throw new HostOrderInformationOutdatedException($"Order completion information from {timestamp} outdated, already started {StartedDate} / finished {FinishedDate}/ cancelled {CancelledDate}.");
        }
        FinishedDate = timestamp;
    }

    /// <summary>
    /// Handle order failure.
    /// </summary>
    public void HandleFailure(DateTime timestamp)
    {
        if (timestamp < FinishedDate || timestamp < CancelledDate || timestamp < FailedDate)
        {
            throw new HostOrderInformationOutdatedException($"Order completion information from {timestamp} outdated, already finished {FinishedDate}/ cancelled {CancelledDate} / failed {FailedDate}.");
        }
        FailedDate = timestamp;
    }
}