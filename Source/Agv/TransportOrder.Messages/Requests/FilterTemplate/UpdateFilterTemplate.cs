namespace Gebhardt.Acc.TransportOrder.Messages.Requests.FilterTemplate;

/// <summary>
/// Represents a request to update an owned filter template.
/// </summary>
/// <param name="Owner">The owner.</param>
/// <param name="UpdatedContent">The new content.</param>
/// <param name="OwnedFilterCorrelationId">The correlation id of the owned filter template.</param>
/// <param name="Name">The new name of the owned filter.</param>
public record UpdateFilterTemplate(string Owner, Domain.Models.FilterTemplate.FilterTemplate UpdatedContent, Guid OwnedFilterCorrelationId, string Name);