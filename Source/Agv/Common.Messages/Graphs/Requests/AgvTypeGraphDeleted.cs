using Gebhardt.Acc.Common.Domain.Models.Graphs;

namespace Gebhardt.Acc.Common.Messages.Graphs.Requests;

/// <summary>
/// Represents the event raised when an agv type graph has been deleted.
/// </summary>
/// <param name="UpdatedTypeGraphs">The graphs to that have been deleted.</param>
public record AgvTypeGraphDeleted(ICollection<Graph> UpdatedTypeGraphs);