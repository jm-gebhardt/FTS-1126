using Gebhardt.StoreWare.Agv.Common.Domain.Models;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.Agv;

/// <summary>
/// An event reporting that load state of an AGV has changed
/// </summary>
/// <param name="CorrelationId">The correlationId of the Agv</param>
/// <param name="Cargos">Agv with it's cargos</param>
[PublicAPI]
public record CargoChanged(Guid CorrelationId, IReadOnlyCollection<Cargo> Cargos) : CorrelatedBy<Guid>;