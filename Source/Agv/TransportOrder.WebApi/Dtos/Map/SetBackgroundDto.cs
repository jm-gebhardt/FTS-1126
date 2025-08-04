using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Gebhardt.Acc.TransportOrder.WebApi.Dtos.Map;
//
// /// <summary>
// /// Form data for uploading or replacing a background image for a map.
// /// </summary>
// /// <param name="Resolution">The resolution of the maps image. Expressed as how many [m] per pixel are represented</param>
// /// <param name="XOffset">The x offset of the map in [m] where the mapped graph coordinate system starts. Origin is the left upper corner.</param>
// /// <param name="YOffset">The y offset of the map in [m] where the mapped graph coordinate system starts. Origin is the left upper corner.</param>
// /// <param name="File">PNG file uploaded</param>
// [UsedImplicitly]
// public record SetBackgroundDto([FromForm] [Required] double Resolution, [FromForm] [Required] double XOffset, [FromForm] [Required] double YOffset, [FromForm(Name = "file")] [Required] IFormFile File);

/// <summary>
/// Form data for uploading or replacing a background image for a map.
/// </summary>
[UsedImplicitly]
public record SetBackgroundDto
{
    /// <summary>
    /// The resolution of the map’s image. Expressed as how many [m] per pixel are represented.
    /// </summary>
    [FromForm]
    [Required]
    [UsedImplicitly]
    public double Resolution { get; init; }

    /// <summary>
    /// The x offset of the map in [m] where the mapped graph coordinate system starts.
    /// Origin is the left upper corner.
    /// </summary>
    [FromForm]
    [Required]
    [UsedImplicitly]
    public double XOffset { get; init; }

    /// <summary>
    /// The y offset of the map in [m] where the mapped graph coordinate system starts.
    /// Origin is the left upper corner.
    /// </summary>
    [FromForm]
    [Required]
    [UsedImplicitly]
    public double YOffset { get; init; }

    /// <summary>
    /// PNG file to upload.
    /// </summary>
    [FromForm(Name = "file")]
    [Required]
    public required IFormFile File { get; init; }
}