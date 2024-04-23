using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.Common.Messages.AgvManagement.Commands;

/// <summary>
/// Request an AGV to transition from Discovered to to Idle state
/// </summary>
[PublicAPI]
public record AcceptAgv(Guid CorrelationId) : CorrelatedBy<Guid>;