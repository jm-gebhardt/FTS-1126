using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.Common.Messages.AgvManagement.Events;

/// <summary>
/// Represents the event raised when a new agv has been discovered.
/// </summary>
/// <param name="CorrelationId">The correlation id of the agv.</param>
/// <param name="SerialNumber">The serial number of the agv</param>
/// <param name="AgvTypeId">The type id of the agv.</param>
/// <param name="HasValidConfiguration">True, if the agv has a valid configuration.</param>
[PublicAPI]
public record NewAgvDiscovered(Guid CorrelationId, string SerialNumber, Guid? AgvTypeId, bool HasValidConfiguration=true) : CorrelatedBy<Guid>;