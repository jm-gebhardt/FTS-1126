using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.WebApi.Dtos;

/// <summary>
/// Lif file meta information.
/// </summary>
[PublicAPI]
public record FileMetaInformationDto
{
    /// <summary>
    /// The correlation id of the lif file.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The time the file was uploaded
    /// </summary>
    public DateTime UploadTimestamp { get; set; }

    /// <summary>
    /// The name of the file
    /// </summary>
    public string Filename { get; set; } = string.Empty;

    /// <summary>
    /// The file size (Mb).
    /// </summary>
    public double FileSizeMb { get; set; }

    /// <summary>
    /// The one who uploaded the file.
    /// </summary>
    public string UploadedBy { get; set; } = string.Empty;

    /// <summary>
    /// Vehicles types supported by the layouts.
    /// </summary>
    public ICollection<string> SupportedVehicleTypes { get; set; } = [];

    /// <summary>
    /// Creator of the LIF file (e.g., name of company or person).
    /// </summary>
    public string Creator { get; set; } = string.Empty;

    /// <summary>
    /// The timestamp at which this LIF file was created/updated/modified. Format is ISO8601 in UTC.
    /// </summary>
    public DateTimeOffset ExportTimestamp { get; set; }

    /// <summary>
    /// Version of the LIF file format. Follows semantic versioning (Major.Minor.Patch).
    /// </summary>
    public string LifVersion { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable name of the project (e.g., for display purposes).
    /// </summary>
    public string ProjectIdentification { get; set; } = string.Empty;
}