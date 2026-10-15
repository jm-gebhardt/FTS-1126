using Gebhardt.Acc.Vda5050Simulator.Domain;

namespace Gebhardt.Acc.Vda5050Simulator.Runtime;

public abstract record AgvCommand(AgvId AgvId);

public sealed record OrderReceived(AgvId AgvId, Order Order) : AgvCommand(AgvId);

public sealed record InstantActionsReceived(AgvId AgvId, IReadOnlyList<InstantAction> Actions) : AgvCommand(AgvId);

/// <summary>
/// A received message failed structural validation; the actor reports it as validation error.
/// </summary>
public sealed record MessageRejected(AgvId AgvId, string MessageKind, string? ReferenceId, IReadOnlyList<string> Errors) : AgvCommand(AgvId);
