using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Lif;

/// <summary>
/// The Lif meta information
/// </summary>
[PublicAPI]
public class MetaInformation
{
    /// <inheritdoc cref="MetaInformation"/>
    public MetaInformation(string creator, DateTimeOffset exportTimestamp, string lifVersion, string projectIdentification)
    {
        Creator = creator;
        ExportTimestamp = exportTimestamp;
        LifVersion = lifVersion;
        ProjectIdentification = projectIdentification;
    }

    /// <summary>
    /// Creator of the LIF file (e.g., name of company or person).
    /// </summary>
    public string Creator { get; set; }

    /// <summary>
    /// The timestamp at which this LIF file was created/updated/modified. Format is ISO8601 in UTC.
    /// </summary>
    public DateTimeOffset ExportTimestamp { get; set; }

    /// <summary>
    /// Version of the LIF file format. Follows semantic versioning (Major.Minor.Patch).
    /// </summary>
    public string LifVersion { get; set; }

    /// <summary>
    /// Human-readable name of the project (e.g., for display purposes).
    /// </summary>
    public string ProjectIdentification { get; set; }
}
