using Gebhardt.Acc.TransportOrder.Domain.Models.Station;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.WebApi.Dtos.NewGraph;

/// <summary>
/// The state of a station.
/// </summary>
/// <param name="Lock">The lock set by an operator. Null if the station is not locked.</param>
/// <param name="LockedOrderHandling">How orders to the station are handled while it is locked. Null if the station is not locked.</param>
/// <param name="HasPalletJam">Whether the station has a pallet jam.</param>
/// <param name="IsOnline">Whether the connection to the station is healthy. Null if the station is not monitored.</param>
/// <param name="ModifiedDate">The time of the last change. Null if the state never changed.</param>
[PublicAPI]
public record StationStatusDto(LockDto? Lock, LockedOrderHandlingType? LockedOrderHandling, bool HasPalletJam, bool? IsOnline, DateTime? ModifiedDate);