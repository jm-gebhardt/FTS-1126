using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Agv;

/// <summary>
/// Provides a command to update or add an the Cargos of a specific Agv.
/// </summary>
/// <param name="CorrelationId">The correlationId of the agv.</param>
/// <param name="Cargos">The Cargos on the Agv.</param>
public record UpdateCargo(Guid CorrelationId, IReadOnlyCollection<Cargo> Cargos);