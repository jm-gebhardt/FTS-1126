using Gebhardt.Acc.Vda5050Simulator.Domain;

namespace Gebhardt.Acc.Vda5050Simulator.Runtime;

public abstract record AgvCommand(AgvIdentity AgvId);

public sealed record OrderReceived(AgvIdentity AgvId, Order Order) : AgvCommand(AgvId);

public sealed record InstantActionsCommand(AgvIdentity AgvId, IReadOnlyList<AgvAction> Actions) : AgvCommand(AgvId);

/// <summary>
/// The factsheet requested by the actor was resolved (null when none is available).
/// </summary>
public sealed record FactsheetResolved(AgvIdentity AgvId, string ActionId, string? FactsheetJson) : AgvCommand(AgvId);

/// <summary>
/// A received message failed structural validation; the actor reports it as validation error.
/// </summary>
public sealed record MessageRejected(AgvIdentity AgvId, string Topic, string? ReferenceId, IReadOnlyList<string> Errors) : AgvCommand(AgvId);

/// <summary>
/// Switches the operating mode of the AGV.
/// </summary>
public sealed record SwitchOperatingModeCommand(AgvIdentity AgvId, Gebhardt.Acc.Common.Domain.Models.AgvOperatingMode OperatingMode) : AgvCommand(AgvId);
