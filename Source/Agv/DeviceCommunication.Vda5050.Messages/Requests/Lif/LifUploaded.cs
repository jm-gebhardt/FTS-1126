using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Lif;
using MassTransit;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Requests.Lif;

/// <summary>
/// Event raise, when a new lif file has been successfully uploaded.
/// </summary>
/// <param name="LifFile">The lif file.</param>
public record LifUploaded(LifFile LifFile) : CorrelatedBy<Guid>
{
    /// <inheritdoc cref="CorrelatedBy{TKey}"/>
    public Guid CorrelationId => LifFile.Id;
}