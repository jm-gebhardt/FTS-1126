using MassTransit;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Requests.Lif;

/// <summary>
/// Provides a request to download a stored lif file.
/// </summary>
/// <param name="CorrelationId">The id of the lif file.</param>
public record DownloadLifFile(Guid CorrelationId) : CorrelatedBy<Guid>;
