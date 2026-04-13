using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.FilterTemplate;

/// <summary>
/// Represents a request to add a filter template.
/// </summary>
/// <param name="Owner">The owner of the filter template.</param>
/// <param name="FilterTemplate">The filter template to add.</param>
/// <param name="CorrelationId">The name of the filter template.</param>
public record AddFilterTemplate(string Owner, Domain.Models.FilterTemplate.FilterTemplate FilterTemplate, string CorrelationId) : CorrelatedBy<string>;