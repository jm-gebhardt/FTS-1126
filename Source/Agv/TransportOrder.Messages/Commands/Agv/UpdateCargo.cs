using Gebhardt.StoreWare.Agv.Common.Domain.Models;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Agv;

/// <summary>
/// Provides a command to update or add the cargos of a specific agv.
/// </summary>
/// <param name="CorrelationId">The correlationId of the agv.</param>
/// <param name="Cargos">The Cargos on the Agv.</param>
public record UpdateCargo(Guid CorrelationId, IReadOnlyCollection<Cargo> Cargos) : CorrelatedBy<Guid>;