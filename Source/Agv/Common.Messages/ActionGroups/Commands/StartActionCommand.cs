using Gebhardt.StoreWare.Agv.Common.Messages.Contracts;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups.Commands;

/// <summary>
/// Triggers the action on the agv side.
/// </summary>
public record StartActionCommand : CorrelatedBy<Guid>, IAgv
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
    /// Constructor
    /// </summary>
    /// <param name="correlationId">The Agv correlation Id.</param>
    /// <param name="serialNumber">The serial number of the agv which executes the action.</param>
    /// <param name="action">Action to be executed.</param>
    public StartActionCommand(Guid correlationId, string serialNumber, AgvAction action)
    {
        CorrelationId = correlationId;
        Action = action;
        SerialNumber = serialNumber;
    }
}