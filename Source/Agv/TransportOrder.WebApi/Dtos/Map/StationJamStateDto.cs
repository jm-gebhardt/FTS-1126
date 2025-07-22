using Gebhardt.Acc.Common.Messages;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.WebApi.Dtos.Map;

/// <summary>
/// The dto for jam state changes.
/// </summary>
/// <param name="StationId">The station id.</param>
/// <param name="HasPalletJam">True, if the station has a pallet jam</param>
[PublicAPI]
public record StationJamStateDto(Guid StationId, bool HasPalletJam): DatedMessage;