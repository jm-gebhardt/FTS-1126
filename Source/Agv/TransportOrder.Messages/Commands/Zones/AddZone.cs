using System.Text.Json.Serialization;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Utilities;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Commands.Zones;

/// <summary>
/// Provides a command to add a <see cref="AgvZone"/>.
/// </summary>
public record AddZone: CorrelatedBy<Guid>
{
    /// <inheritdoc cref="CorrelatedBy{TKey}"/>>
    public Guid CorrelationId => Zone.ZoneId.ToGuid();

    /// <summary>
    /// The agv zone.
    /// </summary>
    public AgvZone Zone { get; }

    /// <inheritdoc cref="AddZone"/>>
    [JsonConstructor]
    public AddZone(AgvZone zone)
    {
        Zone = zone;
    }
}