using MassTransit;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Commands.Lif;

/// <summary>
/// Provides a command to delete a lif file.
/// </summary>
/// <param name="CorrelationId">The id of the lif file (used as message correlation id.)</param>
public record DeleteLifFile(Guid CorrelationId) : CorrelatedBy<Guid>;