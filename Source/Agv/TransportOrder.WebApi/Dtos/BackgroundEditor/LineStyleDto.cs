using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.WebApi.Dtos.BackgroundEditor;

/// <summary>
/// Defines the stroke style of a line or polyline object.
/// </summary>
[PublicAPI]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum LineStyleDto
{
    /// <summary>A continuous, unbroken line.</summary>
    Solid,

    /// <summary>A line rendered as a series of dashes.</summary>
    Dashed
}
