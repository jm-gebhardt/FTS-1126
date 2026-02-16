using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.StationCommunication.Messages.Stations;

/// <summary>
/// Triggers a reload of the station configuration.
/// </summary>
[PublicAPI]
public record ReloadStationConfiguration : CorrelatedBy<Guid>
{
    /// <inheritdoc />
    public Guid CorrelationId { get; init; } = Guid.NewGuid();
}