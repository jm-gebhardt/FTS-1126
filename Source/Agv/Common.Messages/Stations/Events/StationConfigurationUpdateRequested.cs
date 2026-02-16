using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.Common.Messages.Stations.Events;

/// <summary>
/// Represents an event raised when a station configuration update is requested for all services.
/// </summary>
public record StationConfigurationUpdateRequested : CorrelatedBy<Guid>
{
    /// <inheritdoc />
    public Guid CorrelationId
    {
        get;
        [UsedImplicitly]
        init;
    } = Guid.NewGuid();
}
