using MassTransit;

namespace Gebhardt.StoreWare.Agv.Common.Messages.Actions.Commands;

/// <summary>
/// Triggers the action on the Agv side
/// </summary>
public record StartActionCommand : CorrelatedBy<Guid>
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="correlationId">CorrelationId</param>
    /// <param name="agvId">Id of the assigned Agv</param>
    /// <param name="action">Action to be executed</param>
    public StartActionCommand(Guid correlationId, string agvId, AgvAction action)
    {
        CorrelationId = correlationId;
        AgvId = agvId;
        Action = action;
    }

    /// <inheritdoc />
    public Guid CorrelationId { get; }

    /// <summary>
    /// The Id of the assigned Agv
    /// </summary>
    public string AgvId { get; }

    /// <summary>
    /// Action to be executed
    /// </summary>
    public AgvAction Action { get; }

}