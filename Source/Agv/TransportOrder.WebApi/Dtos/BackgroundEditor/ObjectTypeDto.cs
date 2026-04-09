using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.WebApi.Dtos.BackgroundEditor;

/// <summary>
/// Discriminates the concrete type of <see cref="BackgroundObjectDto"/>.
/// Values are matched case-insensitively during deserialization.
/// </summary>
[PublicAPI]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ObjectTypeDto
{
    /// <summary>
    /// Rectangle object.
    /// </summary>
    Rectangle,

    /// <summary>
    /// Triangle object.
    /// </summary>
    Triangle,

    /// <summary>
    /// Circle object.
    /// </summary>
    Circle,

    /// <summary>
    /// Line object.
    /// </summary>
    Line,

    /// <summary>
    /// Polyline object.
    /// </summary>
    Polyline,

    /// <summary>
    /// Text object.
    /// </summary>
    Text
}
