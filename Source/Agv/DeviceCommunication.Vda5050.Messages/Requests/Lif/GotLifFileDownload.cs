using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Lif;
using MassTransit;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Requests.Lif;

/// <summary>
/// Represents the response to the <see cref="DownloadLifFile"/> request.
/// </summary>
/// <param name="LifFile">The lif file.</param>
public record GotLifFileDownload(LifFile LifFile) : CorrelatedBy<Guid>
{
    /// <inheritdoc cref="CorrelatedBy{TKey}"/>
    public Guid CorrelationId => LifFile.Id;
}
