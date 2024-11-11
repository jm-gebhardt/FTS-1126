using Gebhardt.Acc.Common.Messages.Contracts;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.Common.Messages.ActionGroups.Commands;

/// <summary>
/// Updates the action on the agv side.
/// </summary>
[PublicAPI]
public record UpdateActionCommand : CorrelatedBy<Guid>, IAgv
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

    /// <inheritdoc cref="UpdateActionCommand"/>>
    public UpdateActionCommand(Guid correlationId, string serialNumber, AgvAction action)
    {
        CorrelationId = correlationId;
        Action = action;
        SerialNumber = serialNumber;
    }
}