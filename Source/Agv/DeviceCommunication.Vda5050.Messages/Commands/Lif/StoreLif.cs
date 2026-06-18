using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Lif;
using MassTransit;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Commands.Lif;

/// <summary>
/// Provides a command to store a lif file.
/// </summary>
/// <param name="LifFile">The lif file.</param>
/// <param name="LayoutCollection">The layout collection.</param>
public record StoreLif(LifFile LifFile, LifLayoutCollection LayoutCollection) : CorrelatedBy<Guid>
{
    /// <inheritdoc/>
    public Guid CorrelationId => LifFile.Id;

    /// <summary>
    /// Deconstruct the message.
    /// </summary>
    public void Deconstruct(out LifFile lifFile, out LifLayoutCollection layoutCollection)
    {
        lifFile = LifFile;
        layoutCollection = LayoutCollection;
    }
}