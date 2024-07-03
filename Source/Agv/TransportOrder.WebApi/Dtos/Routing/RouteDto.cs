namespace Gebhardt.Acc.TransportOrder.WebApi.Dtos.Routing;

/// <summary>
/// Represents the route of an Agv.
/// </summary>
/// <param name="CorrelationId">The correlationId of the Agv.</param>
/// <param name="Nodes">List of node ids that define the route</param>
/// <param name="Edges">List of edge ids that define the route</param>
public record RouteDto(Guid CorrelationId, string[] Nodes, Guid[] Edges);