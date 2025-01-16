namespace Gebhardt.Acc.PrismatCommunication.Messages.Commands;

/// <summary>
/// Provides a command to handle passing the Doppelspiel trigger point
/// </summary>
/// <param name="AgvCorrelationId">The correlation id of the agv.</param>
/// <param name="DroppingHostOrderCorrelationId">The correlation id of the host order of the drop.</param>
/// <param name="NextPickHostOrderCorrelationId">The correlation id of the host order of the next pick.</param>
/// <param name="ShouldPickNext">If true the agv is going to pick next.</param>
public record PassTriggerPoint(Guid AgvCorrelationId, Guid DroppingHostOrderCorrelationId, Guid? NextPickHostOrderCorrelationId, bool ShouldPickNext);