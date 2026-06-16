using AutoMapper;
using Gebhardt.Acc.TransportOrder.Messages.Extensions;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Graph;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.GraphAnalysis;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gebhardt.Acc.TransportOrder.WebApi.Controllers;

/// <summary>
/// Provides endpoints for querying graph bounding boxes and conflicts.
/// </summary>
[ApiController]
[Authorize]
[ApiExplorerSettings(GroupName = RestApiGroup.GroupName)]
public class GraphController(
    IRequestClient<GetBoundingBoxes> getBoundingBoxesClient,
    IRequestClient<GetBoundingBoxConflicts> getConflictsClient,
    IRequestClient<GetNodeBoundingBoxConflicts> getNodeConflictsClient,
    IRequestClient<GetEdgeBoundingBoxConflicts> getEdgeConflictsClient,
    IMapper mapper
) : Controller
{
    /// <summary>
    /// Returns bounding box polygons for all graph resources of the given AGV type, optionally filtered by resource type.
    /// </summary>
    /// <param name="agvType">The AGV type to retrieve bounding boxes for.</param>
    /// <response code="200">Ok</response>
    [HttpGet]
    [Route("/graph/boundingBoxes")]
    public async Task<ActionResult<IReadOnlyList<ResourceBoundingBoxDto>>> GetBoundingBoxes([FromQuery] string agvType)
    {
        Response<GotBoundingBoxes> response = await getBoundingBoxesClient.GetResponse<GotBoundingBoxes>(
            new GetBoundingBoxes(Guid.NewGuid(), agvType, null));
        return Ok(mapper.Map<IReadOnlyList<ResourceBoundingBoxDto>>(response.Message.BoundingBoxes));
    }

    /// <summary>
    /// Returns all pairs of graph resources whose worst-case bounding boxes geometrically intersect.
    /// </summary>
    /// <response code="200">Ok</response>
    [HttpGet]
    [Route("/graph/conflicts")]
    public async Task<ActionResult<IReadOnlyList<BoundingBoxConflictDto>>> GetConflicts()
    {
        Response<GotBoundingBoxConflicts> response = await getConflictsClient.GetResponse<GotBoundingBoxConflicts>(
            new GetBoundingBoxConflicts(Guid.NewGuid()));
        return Ok(mapper.Map<IReadOnlyList<BoundingBoxConflictDto>>(response.Message.Conflicts));
    }

    /// <summary>
    /// Returns the IDs of all resources whose worst-case bounding boxes intersect the given node.
    /// </summary>
    /// <param name="nodeId">The node to retrieve conflicts for.</param>
    /// <response code="200">Ok</response>
    /// <response code="404">No node with the given ID exists</response>
    [HttpGet]
    [Route("/graph/node/{nodeId:guid}/conflicts")]
    public async Task<ActionResult<IReadOnlyList<Guid>>> GetNodeConflicts(Guid nodeId)
    {
        Response<GotBoundingBoxConflicts> response = await getNodeConflictsClient.GetResponse<GotBoundingBoxConflicts>(
            new GetNodeBoundingBoxConflicts(Guid.NewGuid(), nodeId));
        return Ok(response.Message.Conflicts.ConflictingResourceIds(nodeId));
    }

    /// <summary>
    /// Returns the IDs of all resources whose worst-case bounding boxes intersect the given edge.
    /// </summary>
    /// <param name="edgeId">The edge to retrieve conflicts for.</param>
    /// <response code="200">Ok</response>
    /// <response code="404">No edge with the given ID exists</response>
    [HttpGet]
    [Route("/graph/edge/{edgeId:guid}/conflicts")]
    public async Task<ActionResult<IReadOnlyList<Guid>>> GetEdgeConflicts(Guid edgeId)
    {
        Response<GotBoundingBoxConflicts> response = await getEdgeConflictsClient.GetResponse<GotBoundingBoxConflicts>(
            new GetEdgeBoundingBoxConflicts(Guid.NewGuid(), edgeId));
        return Ok(response.Message.Conflicts.ConflictingResourceIds(edgeId));
    }
}