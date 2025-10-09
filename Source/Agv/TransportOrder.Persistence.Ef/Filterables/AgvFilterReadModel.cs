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
    /// <summary>
    /// Correlation id of the agv.
    /// </summary>
    [Filter.FilterProperty]
    [Filter.KeyProperty]
    public new Guid CorrelationId { get; init; }

    /// <summary>
    /// Serial number of the agv.
    /// </summary>
    [Filter.FilterProperty]
    public new string SerialNumber { get; init; } = null!;

    /// <summary>
    /// The name of the agv.
    /// </summary>
    [Filter.FilterProperty]
    public new string Name { get; private set; } = null!;

    /// <summary>
    /// Last received agv status.
    /// </summary>
    [Filter.NavigationProperty]
    public new AgvStatus? AgvStatus { get; set; }

    /// <summary>
    /// The mode of the agv.
    /// </summary>
    [Filter.NavigationProperty]

    public new AgvMode Mode { get; init; } = null!;
}