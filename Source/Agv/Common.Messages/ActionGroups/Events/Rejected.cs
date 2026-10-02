namespace Gebhardt.Acc.Common.Messages.ActionGroups.Events;

/// <summary>
/// Defines a basic, rejected result
/// </summary>
/// <param name="Reason">Human-readable reason why the command was rejected.</param>
public record Rejected(string Reason);
