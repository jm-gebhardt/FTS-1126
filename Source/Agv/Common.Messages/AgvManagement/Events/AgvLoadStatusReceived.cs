using Gebhardt.StoreWare.Agv.Common.Domain.Models;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;

/// <summary>
/// An event reporting that AgvLoadStatusReceived of an Agv has been received
/// </summary>
[PublicAPI]
public record AgvLoadStatusReceived : CorrelatedBy<Guid>
{
    /// <summary>
    /// The correlationId of the Agv
    /// </summary>
    public Guid CorrelationId { get; }

    /// <summary>
    /// Current load status of the Agv
    /// </summary>
    public IReadOnlyCollection<Cargo> Cargos { get; }

    /// <inheritdoc cref="AgvLoadStatusReceived" />
    /// <param name="correlationId">The correlationId of the Agv</param>
    /// <param name="cargos"></param>
    public AgvLoadStatusReceived(Guid correlationId, IReadOnlyCollection<Cargo> cargos)
    {
        CorrelationId = correlationId;
        Cargos = cargos;
    }
}