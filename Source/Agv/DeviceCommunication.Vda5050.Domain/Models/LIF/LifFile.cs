
using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Lif;

/// <summary>
/// A lif file.
/// </summary>
[PublicAPI]
public class LifFile
{
    /// <summary>
    /// The correlation id of the lif file.
    /// </summary>
    public Guid Id { get; set; } = Guid.Empty;

    /// <summary>
    /// The Lif collection meta information
    /// </summary>
    public MetaInformation LifCollectionMetaInformation { get; set; } = null!;

    /// <summary>
    /// The ids of the lif layouts that are part of the collection.
    /// </summary>
    public ICollection<Guid> LayoutIds { get; set; } = [];

    /// <summary>
    /// Vehicles types supported by the layouts.
    /// </summary>
    public ICollection<string> SupportedVehicleTypes { get; set; } = [];

    /// <summary>
    /// The file meta information.
    /// </summary>
    public LifFileMetaInformation MetaInformation { get; set; } = null!;

    /// <summary>
    /// The raw JSON bytes of the originally uploaded file.
    /// </summary>
    public byte[]? RawJson { get; set; }

    /// <inheritdoc cref="LifFile" />
    public LifFile(Guid id, MetaInformation lifCollectionMetaInformation, ICollection<Guid> layoutIds,ICollection<string> supportedVehicleTypes, LifFileMetaInformation metaInformation)
    {
        Id = id;
        LifCollectionMetaInformation = lifCollectionMetaInformation;
        LayoutIds = layoutIds;
        MetaInformation = metaInformation;
        SupportedVehicleTypes = supportedVehicleTypes;
    }

    /// <inheritdoc cref="LifFile" />
    public LifFile()
    {
    }
}

/// <summary>
/// Meta information on the lif file.
/// </summary>
[PublicAPI]
public record LifFileMetaInformation
{
    /// <summary>
    /// The time the file was uploaded
    /// </summary>
    public DateTime UploadTimestamp { get; set; }

    /// <summary>
    /// The name of the file
    /// </summary>
    public string Filename { get; set; }

    /// <summary>
    /// The file size (Mb).
    /// </summary>
    public double FileSizeMb { get; set; }

    /// <summary>
    /// The one who uploaded the file.
    /// </summary>
    public string UploadedBy { get; set; }

    /// <summary>
    /// Validation results.
    /// </summary>
    public ICollection<LifValidationResult> ValidationResults { get; set; } = [];

    /// <inheritdoc cref="LifFileMetaInformation"/>
    public LifFileMetaInformation(DateTime uploadTimestamp, string filename, double fileSizeMb, string uploadedBy)
    {
        UploadTimestamp = uploadTimestamp;
        Filename = filename;
        FileSizeMb = fileSizeMb;
        UploadedBy = uploadedBy;
    }
}
