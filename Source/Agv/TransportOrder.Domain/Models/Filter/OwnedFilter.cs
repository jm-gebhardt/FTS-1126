using System.ComponentModel.DataAnnotations;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Filter.Api.Dtos;
using Gebhardt.Acc.Common.Filter.FilterBuilder;

namespace Gebhardt.Acc.TransportOrder.Domain.Models.Filter;

/// <summary>
/// Represents an owned <see cref="IFilter"/>.
/// </summary>
public class OwnedFilter : IOwnedResource<FilterDto>
{
    /// <summary>
    /// The correlation id of the filter.
    /// </summary>
    public Guid ResourceCorrelationId { get; set; }

    /// <summary>
    /// The owner of the filter.
    /// </summary>
    public string Owner { get; set; }

    /// <summary>
    /// The filter.
    /// </summary>
    [Required]
    public FilterDto Resource { get; set; }

    /// <summary>
    /// The name of this filter.
    /// </summary>
    public string ResourceName { get; set; }

    /// <inheritdoc cref="OwnedFilter"/>
    public OwnedFilter()
    {
        Owner = "";
        ResourceCorrelationId = Guid.Empty;
        Resource = null!;
        ResourceName = "";
    }

    /// <inheritdoc cref="OwnedFilter"/>
    public OwnedFilter(Guid correlationId, string owner, FilterDto filter, string name)
    {
        ResourceCorrelationId = correlationId;
        Owner = owner;
        Resource = filter;
        ResourceName = name;
    }
}