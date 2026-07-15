using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;

/// <summary>
/// Provides a command to create an agv.
/// </summary>
/// <param name="CorrelationId">The correlation id of the agv.</param>
/// <param name="SerialNumber">The serial number of the agv</param>
/// <param name="AgvTypeId">The type id of the agv.</param>
/// <param name="HasValidConfiguration">True, if the agv has a valid configuration.</param>
[PublicAPI]
public record CreateAgv(Guid CorrelationId, string SerialNumber, Guid? AgvTypeId, bool HasValidConfiguration = true) : CorrelatedBy<Guid>;