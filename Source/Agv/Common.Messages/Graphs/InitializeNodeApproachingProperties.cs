using Gebhardt.Acc.Common.Domain.Models.Graphs;

namespace Gebhardt.Acc.Common.Messages.Graphs;

/// <summary>
/// Provides a command to initializes the node approaching properties-
/// </summary>
/// <param name="NodeId">The node id.</param>
/// <param name="PickLiftHeight">The lift height for picking.</param>
/// <param name="DropLiftHeight">The lift height for dropping.</param>
/// <param name="DockingDirection">The docking direction.</param>
public record InitializeNodeApproachingProperties(string NodeId, double? PickLiftHeight, double? DropLiftHeight, DockingDirection? DockingDirection);