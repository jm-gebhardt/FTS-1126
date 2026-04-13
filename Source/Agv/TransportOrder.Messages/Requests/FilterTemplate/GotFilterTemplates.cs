using Gebhardt.Acc.TransportOrder.Domain.Models.FilterTemplate;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.FilterTemplate;

/// <summary>
/// Represents the response to the <see cref="GetFilterTemplates"/> request.
/// </summary>
/// <param name="FilterTemplates">The filter templates.</param>
public record GotFilterTemplates(ICollection<OwnedFilterTemplate> FilterTemplates);