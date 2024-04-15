using Gebhardt.StoreWare.Agv.Common.Domain.Models;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Agv;

/// <summary>
/// Provides a command to update agv data.
/// </summary>
/// <param name="CorrelationId">The correlation id of the agv.</param>
/// <param name="Data">The agv data.</param>
public record UpdateAgvData(Guid CorrelationId, AgvData Data) : CorrelatedBy<Guid>;