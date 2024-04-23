using Gebhardt.Acc.Common.Domain.Models.Graphs;

namespace Gebhardt.Acc.Common.Exceptions;

/// <summary>
/// No free charge station available.
/// </summary>
public class NoChargeStationAvailableException() : NoNodesAvailableException(AgvGraph.NodeType.Charge.ToString());