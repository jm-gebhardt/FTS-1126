using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.WebApi.Dtos.FilterResults;

/// <summary>
/// 
/// </summary>
/// <param name="Position"></param>
/// <param name="Node"></param>
[PublicAPI]
public record FilteredActionTargetDto(FilteredAgvPositionDto? Position, FilteredGraphNodeDto? Node);

/// <summary>
/// 
/// </summary>
/// <param name="X"></param>
/// <param name="Y"></param>
/// <param name="Z"></param>
/// <param name="Theta"></param>
[PublicAPI]
public record FilteredAgvPositionDto(float X, float Y, float Z, float Theta);

/// <summary>
/// 
/// </summary>
/// <param name="Name"></param>
[PublicAPI]
public record FilteredGraphNodeDto(string Name);