using Gebhardt.Acc.Common.Filter.ReadModelParser;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.Filterables;

/// <summary>
/// Read model of an agv filter.
/// </summary>
[PublicAPI]
public class AgvFilterReadModel : Agv, IFilterReadModel<Agv, AgvFilterReadModel>
{
    /// <inheritdoc cref="Agv.CorrelationId"/>
    [FilterReadModelBinderBase.FilterProperty]
    [FilterReadModelBinderBase.KeyProperty]
    public new Guid CorrelationId { get; init; }

    /// <inheritdoc cref="Agv.SerialNumber"/>
    [FilterReadModelBinderBase.FilterProperty]
    public new string SerialNumber { get; init; } = null!;

    /// <inheritdoc cref="Agv.Name"/>
    [FilterReadModelBinderBase.FilterProperty]
    public new string Name { get; init; } = null!;

    /// <inheritdoc cref="Agv.AgvStatus"/>
    [FilterReadModelBinderBase.NavigationProperty]
    public new AgvStatus? AgvStatus { get; init; }

    /// <inheritdoc cref="Agv.Mode"/>
    [FilterReadModelBinderBase.NavigationProperty]
    public new AgvMode Mode { get; init; } = null!;
}