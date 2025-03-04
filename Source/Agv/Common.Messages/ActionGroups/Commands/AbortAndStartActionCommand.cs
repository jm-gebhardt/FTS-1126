using Gebhardt.Acc.Common.Messages.Contracts;
using MassTransit;

namespace Gebhardt.Acc.Common.Messages.ActionGroups.Commands;

/// <summary>
/// Triggers abortion of an action on the agv side and start of a new action.
/// </summary>
public record AbortAndStartActionCommand : CorrelatedBy<Guid>, IAgv
{
    /// <summary>
    /// The Agv CorrelationId.
    /// </summary>
    public Guid CorrelationId { get; }

    /// <summary>
    /// The serial number of the agv which executes the action.
    /// </summary>
    public string SerialNumber { get; }

    /// <summary>
    /// Action to be executed.
    /// </summary>
    public AgvAction Action { get; }

    /// <summary>
    /// The correlation id of the aborted action
    /// </summary>
    public Guid? AbortedActionCorrelationId { get;  }

    /// <inheritdoc cref="AbortAndStartActionCommand"/>>
    public AbortAndStartActionCommand(Guid correlationId, string serialNumber, AgvAction action, Guid? abortedActionCorrelationId)
    {
        CorrelationId = correlationId;
        Action = action;
        SerialNumber = serialNumber;
        AbortedActionCorrelationId = abortedActionCorrelationId;
    }
}