using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.FilterTemplate;

/// <summary>
/// Provides a request to delete a filter template.
/// </summary>
/// <param name="CorrelationId">The correlation id of the filter template.</param>
/// <param name="Owner">The owner of the filter.</param>
[PublicAPI]
public record DeleteFilterTemplate(Guid CorrelationId, string Owner): CorrelatedBy<Guid>;