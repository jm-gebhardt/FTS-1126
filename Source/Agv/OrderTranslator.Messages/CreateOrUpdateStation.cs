using Gebhardt.Acc.OrderTranslator.Domain.Models.Station;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.OrderTranslator.Messages;

/// <summary>
/// Provides a command to create or update an existing station.
/// </summary>
[PublicAPI]
public record CreateOrUpdateStation : CorrelatedBy<Guid>
{
    /// <summary>
    /// The station.
    /// </summary>
    public Station Station { get; init; }

    /// <summary>
    /// The message correlation id.
    /// </summary>
    public Guid CorrelationId => Station.Id;

    /// <inheritdoc cref="CreateOrUpdateStation"/>>
    public CreateOrUpdateStation(Station station)
    {
        Station = station;
    }
}