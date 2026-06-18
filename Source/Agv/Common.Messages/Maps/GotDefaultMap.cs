namespace Gebhardt.Acc.Common.Messages.Maps;

/// <summary>
/// Provides a response to the <see cref="GetDefaultMap"/> request.
/// </summary>
/// <param name="MapName">The name of the map</param>
/// <param name="MinX">The minimum x of the map</param>
/// <param name="MaxX">The maximum x of the map</param>
/// <param name="MinY">The minimum y of the map</param>
/// <param name="MaxY">The maximum y of the map</param>
public record GotDefaultMap(string MapName, float MinY, float MaxY, float MinX, float MaxX);

/// <summary>
/// Provides a response to the <see cref="GetDefaultMap"/> request.
/// </summary>
public record NoDefaultMap;
