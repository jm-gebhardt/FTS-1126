using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv.Queries;

/// <summary>
/// Represents the request which returns a list of all AgvErrors (both active and resolved) back until the provided date
/// </summary>
public record GetAgvErrors : CorrelatedBy<Guid>
{
    /// <inheritdoc cref="GetAgvErrors" />
    /// <param name="agvCorrelationId">Agv correlationId</param>
    /// <param name="dateFrom">only consider errors newer than this parameter (if set)</param>
    /// <param name="dateTo">only consider errors older than this parameter (if set)</param>
    public GetAgvErrors(Guid agvCorrelationId, DateTime dateFrom, DateTime dateTo)
    {
        CorrelationId = agvCorrelationId;
        DateFrom = dateFrom;
        DateTo = dateTo;
    }

    /// <summary>
    /// serialization ctor
    /// </summary>
    [UsedImplicitly]
    public GetAgvErrors()
    {
    }

    /// <summary>
    /// Correlation Id of Agv
    /// </summary>
    [UsedImplicitly]
    public Guid CorrelationId { get; init; }

    /// <summary>
    /// Only consider errors newer than this parameter (if set)
    /// </summary>
    [UsedImplicitly]
    public DateTime DateFrom { get; init; }

    /// <summary>
    /// Only consider errors older than this parameter (if set)
    /// </summary>
    [UsedImplicitly]
    public DateTime DateTo { get; init; }
}