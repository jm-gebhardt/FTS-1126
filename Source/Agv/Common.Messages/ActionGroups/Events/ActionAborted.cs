using MassTransit;

namespace Gebhardt.Acc.Common.Messages.ActionGroups.Events;

/// <summary>
/// Represents the event triggered, when an action group
/// was aborted via the transport order WebApi or during a pause request.
/// </summary>
public record ActionAborted : CorrelatedBy<Guid>
{
    /// <summary>
    /// The Agv correlation ID.
    /// </summary>
    public Guid CorrelationId { get; }

    /// <summary>
    /// Action to be executed.
    /// </summary>
    public AgvAction Action { get; }

    /// <summary>
    /// If the abort is because of a paused action group.
    /// </summary>
    public bool IsTemporary { get; }

    /// <inheritdoc cref="ActionAborted"/>
    public ActionAborted(Guid correlationId, AgvAction action, bool isTemporary = false)
    {
        CorrelationId = correlationId;
        Action = action;
        IsTemporary = isTemporary;
    }

    /// <summary>
    /// Deconstructs <see cref="ActionAborted"/>.
    /// </summary>
    public void Deconstruct(out Guid correlationId, out AgvAction action, out bool isTemporary)
    {
        correlationId = CorrelationId;
        action = Action;
        isTemporary = IsTemporary;
    }
}