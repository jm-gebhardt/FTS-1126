namespace Gebhardt.Acc.Common.Messages.ActionGroups.Requests;

/// <summary>
/// Represents the response to <see cref="TriggerDoppelspielPartnerQueueing"/>.
/// </summary>
/// <param name="CorrelationId">The correlation id of the Doppelspiel pick order, if any.</param>
public record DoppelspielPartnerQueued(Guid? CorrelationId);