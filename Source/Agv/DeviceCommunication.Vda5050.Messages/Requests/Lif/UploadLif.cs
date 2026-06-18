using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Requests.Lif;

/// <summary>
/// Provides a command to upload a lif file
/// </summary>
[PublicAPI]
public record UploadLif
{
    /// <summary>
    /// The time the file was uploaded.
    /// </summary>
    public DateTime UploadTimestamp { get; set; }

    /// <summary>
    /// The LIF file
    /// </summary>
    public IFormFile LifFile { get; set; } = null!;

    /// <summary>
    /// The user that uploaded the Lif file
    /// </summary>
    public string UploadedBy { get; set; } = string.Empty;

    /// <inheritdoc cref="UploadLif" />
    public UploadLif(DateTime uploadTimestamp, IFormFile lifFile, string uploadedBy)
    {
        UploadTimestamp = uploadTimestamp;
        LifFile = lifFile;
        UploadedBy = uploadedBy;
    }

    /// <summary>
    /// Deconstruct the message.
    /// </summary>
    public void Deconstruct(out DateTime uploadTimestamp, out IFormFile lifFile, out string uploadedBy)
    {
        uploadTimestamp = UploadTimestamp;
        lifFile = LifFile;
        uploadedBy = UploadedBy;
    }
}
