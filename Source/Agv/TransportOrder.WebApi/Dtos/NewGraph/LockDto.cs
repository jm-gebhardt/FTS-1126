using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.WebApi.Dtos.NewGraph;

/// <summary>
/// A lock set by an operator.
/// </summary>
/// <param name="User">The operator who set the lock.</param>
/// <param name="Reason">The reason for the lock.</param>
/// <param name="Description">The description of the lock.</param>
/// <param name="LockedAt">The time the lock was set.</param>
[PublicAPI]
public record LockDto(string? User, string? Reason, string? Description, DateTime LockedAt);