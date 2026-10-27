using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Gebhardt.Acc.TransportOrder.WebApi.Dtos.NewGraph;

/// <summary>
/// Form data for uploading or replacing the background image of a view.
/// </summary>
[UsedImplicitly]
public record SetViewBackgroundDto
{
    /// <summary>
    /// The resolution of the image in [m] per pixel.
    /// </summary>
    [FromForm]
    [Required]
    [Range(double.Epsilon, double.MaxValue, ErrorMessage = "The field {0} must be greater than 0.")]
    [UsedImplicitly]
    public double Resolution { get; init; }

    /// <summary>
    /// The x offset in [m] where the graph coordinate system starts. Origin is the left upper corner.
    /// </summary>
    [FromForm]
    [Required]
    [UsedImplicitly]
    public double XOffset { get; init; }

    /// <summary>
    /// The y offset in [m] where the graph coordinate system starts. Origin is the left upper corner.
    /// </summary>
    [FromForm]
    [Required]
    [UsedImplicitly]
    public double YOffset { get; init; }

    /// <summary>
    /// PNG file to upload. Other image formats are rejected.
    /// </summary>
    [FromForm(Name = "file")]
    [Required]
    [UsedImplicitly]
    public required IFormFile File { get; init; }
}