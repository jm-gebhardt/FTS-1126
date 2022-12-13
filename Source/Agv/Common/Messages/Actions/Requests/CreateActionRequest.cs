using MassTransit;

namespace Gebhardt.StoreWare.Agv.Common.Messages.Actions.Requests;

/// <summary>
/// Sent as soon as the action has been assigned to an Agv
/// </summary>
public record CreateActionRequest : CorrelatedBy<Guid>
{
    /// <summary>
    /// The action that was assigned
    /// </summary>
    public AgvAction Action { get; }

    /// <summary>
    /// The CorrelationId
    /// </summary>
    public Guid CorrelationId { get; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="action">Action that was assigned</param>
    public CreateActionRequest(AgvAction action)
    {
        Action = action;
        CorrelationId = action.CorrelationId;
    }
}