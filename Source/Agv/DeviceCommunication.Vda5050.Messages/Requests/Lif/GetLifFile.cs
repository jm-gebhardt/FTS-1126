using MassTransit;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Requests.Lif;

/// <summary>
/// Provides a request to get all stored lif files.
/// </summary>
/// <param name="CorrelationId">The id of the lif file.</param>
public record GetLifFile(Guid CorrelationId): CorrelatedBy<Guid>;