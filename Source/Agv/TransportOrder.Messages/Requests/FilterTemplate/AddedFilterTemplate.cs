namespace Gebhardt.Acc.TransportOrder.Messages.Requests.FilterTemplate;

/// <summary>
/// Represents a response to <see cref="AddFilterTemplate"/>.
/// </summary>
/// <param name="FilterTemplateCorrelationId">The correlation id of the added filter template.</param>
public record AddedFilterTemplate(Guid FilterTemplateCorrelationId);