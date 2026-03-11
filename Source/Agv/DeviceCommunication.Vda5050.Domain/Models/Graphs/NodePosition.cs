namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Graphs;

/// <summary>
/// The position of the node.
/// </summary>
/// <param name="X">X position on the layout in reference to the global origin.</param>
/// <param name="Y">Y position on the layout in reference to the global origin.</param>
public record NodePosition(double X, double Y);