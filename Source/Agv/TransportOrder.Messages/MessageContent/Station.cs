using Gebhardt.Acc.Common.Domain.Models.Graphs;

namespace Gebhardt.Acc.TransportOrder.Messages.MessageContent;

/// <summary>
/// Station message content.
/// </summary>
/// <param name="Id">The Id of the node.</param>
/// <param name="Length">Length of the station [m].</param>
/// <param name="Width">Width of the station [m].</param>
/// <param name="StationType">The type of the station.</param>
public record Station(string Id, double Length, double Width, AgvGraph.NodeType StationType);