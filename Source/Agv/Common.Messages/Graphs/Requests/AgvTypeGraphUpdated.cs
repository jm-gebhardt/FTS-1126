using Gebhardt.Acc.Common.Domain.Models.Graphs;

namespace Gebhardt.Acc.Common.Messages.Graphs.Requests;

/// <summary>
/// Represents the event raised when an agv type graph has been updated.
/// </summary>
/// <param name="UpdatedTypeGraphs">The graphs to that have been updated.</param>
public record AgvTypeGraphUpdated(ICollection<Graph> UpdatedTypeGraphs);