using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Requests.Lif;

/// <summary>
/// Provides a command to upload a lif file.
/// </summary>
/// <param name="CorrelationId">The correlation id of the upload.</param>
/// <param name="UploadTimestamp">The time the file was uploaded.</param>
/// <param name="Content">The raw content of the lif file.</param>
/// <param name="FileName">The name of the lif file.</param>
/// <param name="ContentType">The content type of the lif file.</param>
/// <param name="UploadedBy">The user that uploaded the lif file.</param>
[PublicAPI]
public record UploadLif(Guid CorrelationId, DateTime UploadTimestamp, byte[] Content, string FileName, string ContentType, string UploadedBy) : CorrelatedBy<Guid>;