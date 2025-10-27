namespace Gebhardt.Acc.Common.Messages.LogEntry;

/// <summary>
/// Represents an event raised when a log worthy event has occurred.
/// </summary>
/// <param name="Entry">The entry that should be generated.</param>
/// <param name="Enrichment">The <see cref="Categories"/> that the entry should be enriched by.</param>
public record LogOccurred(LogEntryDto Entry, HashSet<Categories> Enrichment);