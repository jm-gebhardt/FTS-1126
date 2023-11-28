using MassTransit;

namespace Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Commands;

/// <summary>
/// Represents the command sent when the agv errors should be reset.
/// </summary>
/// <param name="CorrelationId">Agv correlation id.</param>
/// <param name="ErrorIds">List of errors to reset. If empty, resets all agv errors for the specified agv.</param>
public record ResetAgvErrors(Guid CorrelationId, ICollection<Guid> ErrorIds) : CorrelatedBy<Guid>;
