using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.WebApi.Dtos.NewGraph;

/// <summary>
/// An AGV type in one load state.
/// </summary>
/// <param name="AgvTypeId">The id of the AGV type.</param>
/// <param name="LoadState">The load state of the AGV type.</param>
[PublicAPI]
public record AgvTypeStateDto(Guid AgvTypeId, LoadStateDto LoadState);