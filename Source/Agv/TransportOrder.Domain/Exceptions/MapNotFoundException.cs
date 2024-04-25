using Gebhardt.Acc.Common.Domain.Exceptions;

namespace Gebhardt.Acc.TransportOrder.Domain.Exceptions;

/// <summary>
/// Represents errors that occur when the map is not found in the e.g. repository
/// </summary>
public class MapNotFoundException : NotFoundException
{
    /// <summary>
    /// <inheritdoc cref="MapNotFoundException"/>
    /// </summary>
    public MapNotFoundException() : base("Map")
    {
    }

    /// <summary>
    /// <inheritdoc cref="MapNotFoundException"/>
    /// </summary>
    /// <param name="paramIdentifierName">Name of map identifier</param>
    /// <param name="paramIdentifierValue">Value of map identifier</param>
    public MapNotFoundException(string paramIdentifierName, object paramIdentifierValue)
        : base("Map", paramIdentifierName, paramIdentifierValue)
    {
    }
}