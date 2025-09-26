using Gebhardt.Acc.Common.Filter.FilterableParser;
using Gebhardt.Acc.Common.Filter.FilterableParser.Attributes;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.Filterables;

/// <summary>
/// Read model of an agv filter.
/// </summary>
[PublicAPI]
public class AgvFilterable : Agv, IFilterable<Agv, AgvFilterable>
{
    /// <inheritdoc cref="Agv.CorrelationId"/>
    [FilterProperty]
    [KeyProperty]
    public new Guid CorrelationId { get; init; }

    /// <inheritdoc cref="Agv.SerialNumber"/>
    [FilterProperty]
    public new string SerialNumber { get; init; } = null!;

    /// <inheritdoc cref="Agv.Name"/>
    [FilterProperty]
    public new string Name { get; init; } = null!;

    /// <inheritdoc cref="Agv.AgvStatus"/>
    [NavigationProperty]
    public new AgvStatus? AgvStatus { get; init; }

    /// <inheritdoc cref="Agv.Mode"/>
    [NavigationProperty]
    public new AgvMode Mode { get; init; } = null!;
}