using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.WebApi.Dtos.BackgroundEditor;

/// <summary>
/// Base representation of a background canvas object.
/// Concrete types are discriminated by the <c>objectType</c> JSON property.
/// <see cref="BackgroundObjectDtoConverter"/> handles deserialization.
/// </summary>
[PublicAPI]
[JsonConverter(typeof(BackgroundObjectDtoConverter))]
public abstract record BackgroundObjectDto
{
    /// <summary>Discriminator identifying the concrete object type.</summary>
    public abstract ObjectTypeDto ObjectType { get; }

    /// <summary>Stable numeric identifier, unique within the background document.</summary>
    public int Id { get; init; }

    /// <summary>Horizontal position on the canvas (world units).</summary>
    public double X { get; init; }

    /// <summary>Vertical position on the canvas (world units).</summary>
    public double Y { get; init; }

    /// <summary>Bounding-box width of the object before scaling.</summary>
    public double Width { get; init; }

    /// <summary>Bounding-box height of the object before scaling.</summary>
    public double Height { get; init; }

    /// <summary>Rotation angle in radians, applied around the object origin.</summary>
    public double Rotation { get; init; }

    /// <summary>Horizontal scale factor.</summary>
    public double ScaleX { get; init; }

    /// <summary>Vertical scale factor.</summary>
    public double ScaleY { get; init; }
}
