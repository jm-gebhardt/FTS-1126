using Gebhardt.StoreWare.Agv.Common.Contracts;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Agv;

/// <summary>
/// Provides a command to update an agv status.
/// </summary>
/// <param name="CorrelationId">The correlationId of the Agv</param>
/// <param name="SerialNumber">The serial number of the agv.</param>
/// <param name="AgvStatus">The agv status.</param>
public record UpdateAgvStatus(Guid CorrelationId, [UsedImplicitly] string SerialNumber, AgvStatus AgvStatus) : CorrelatedBy<Guid>, IAgv;