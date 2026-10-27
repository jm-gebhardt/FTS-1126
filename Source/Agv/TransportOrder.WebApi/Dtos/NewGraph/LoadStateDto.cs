using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.WebApi.Dtos.NewGraph;

/// <summary>
/// A load state of an AGV type.
/// </summary>
/// <param name="Id">The id of the load state.</param>
/// <param name="IsLoaded">Whether the AGV is loaded in this state.</param>
[PublicAPI]
public record LoadStateDto(Guid Id, bool IsLoaded);