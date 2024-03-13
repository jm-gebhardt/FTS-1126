using Gebhardt.StoreWare.Agv.Common.Models.Graphs;

namespace Gebhardt.StoreWare.Agv.Common.Exceptions;

/// <summary>
/// No free charge station available.
/// </summary>
public class NoChargeStationAvailableException() : NoNodesAvailableException(AgvGraph.NodeType.Charge.ToString());