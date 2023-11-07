using MassTransit;

namespace Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups.Events;

/// <summary>
/// Represents the event triggered, when an action group
/// was aborted via the transport order WebApi.
/// </summary>
public record ActionAborted : CorrelatedBy<Guid>
{
    /// <inheritdoc cref="CorrelatedBy{Guid}"/>
    public Guid CorrelationId { get; }

    /// <summary>
    /// Action to be executed.
    /// </summary>
    public AgvAction Action { get; }

    /// <inheritdoc cref="ActionAborted"/>
    public ActionAborted(Guid correlationId,  AgvAction action)
    {
        CorrelationId = correlationId;
        Action = action;
    }

    /// <summary>
    /// Deconstructs <see cref="ActionAborted"/>.
    /// </summary>
    public void Deconstruct(out Guid correlationId, out AgvAction action)
    {
        correlationId = CorrelationId;
        action = Action;
    }
}