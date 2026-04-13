using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.TransportOrder.Domain.Models.Filter;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.Models.FilterTemplate;

/// <summary>
/// Represents an owned filter template
/// </summary>
[PublicAPI]
public class OwnedFilterTemplate: IOwnedResource<FilterTemplate>
{
    /// <summary>
    /// The owner of the template.
    /// </summary>
    public string Owner { get; set; }

    /// <summary>
    /// The unique id of the filter.
    /// </summary>
    public Guid ResourceCorrelationId { get; set; }

    /// <summary>
    /// The filter template resource.
    /// </summary>
    public FilterTemplate Resource { get; set; }

    /// <summary>
    /// The name of the resource
    /// </summary>
    public string ResourceName { get; set; }

    /// <inheritdoc cref="OwnedFilterTemplate"/>
    public OwnedFilterTemplate()
    {
        Owner = "";
        ResourceCorrelationId = Guid.Empty;
        Resource = null!;
        ResourceName = "";
    }

    /// <inheritdoc cref="OwnedFilter"/>
    public OwnedFilterTemplate(Guid correlationId, string owner, FilterTemplate filter, string name)
    {
        ResourceCorrelationId = correlationId;
        Owner = owner;
        Resource = filter;
        ResourceName = name;
    }
}