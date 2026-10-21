using Gebhardt.Acc.Common.Domain.Models.Graphs;
using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Messages.Graphs.Requests;

/// <summary>
/// Represents the event raised when an agv type graph has been updated.
/// </summary>
/// <param name="UpdatedTypeGraphs">The graphs to that have been updated.</param>
[UsedImplicitly]
public record AgvTypeGraphUpdated(ICollection<Graph> UpdatedTypeGraphs);