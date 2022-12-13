using Gebhardt.StoreWare.Agv.Common.Messages.Actions.Events;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.Common.Messages.Actions.Requests;

/// <summary>
/// Sent once the action can be assigned to an Agv. The ActionStateMachine will respond with the <see cref="ActionAssignmentSucceededEvent"/>
/// </summary>
public record AssignActionRequest : CorrelatedBy<Guid>
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
    /// The CorrelationId of the assigned Agv
    /// </summary>
    public Guid Agv { get; }

    /// <summary>
    /// The AgvId of the assigned Agv
    /// </summary>
    public string AgvId { get; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="action">Action that was assigned</param>
    /// <param name="agv">CorrelationId of the assigned Agv</param>
    /// <param name="agvId">AgvId of the assigned Agv</param>
    public AssignActionRequest(AgvAction action, Guid agv, string agvId)
    {
        Action = action;
        CorrelationId = action.CorrelationId;
        Agv = agv;
        AgvId = agvId;
    }
}