using Gebhardt.StoreWare.Agv.Common.Domain.Models.Graphs;

namespace Gebhardt.StoreWare.Agv.Common.Exceptions;

/// <summary>
/// No free charge station available.
/// </summary>
public class NoChargeStationAvailableException() : NoNodesAvailableException(AgvGraph.NodeType.Charge.ToString());