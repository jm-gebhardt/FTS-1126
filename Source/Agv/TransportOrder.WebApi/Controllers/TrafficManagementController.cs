using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.TransportOrder.Messages.Requests.TrafficManagement;
using MassTransit;
using Microsoft.AspNetCore.Mvc;

namespace Gebhardt.Acc.TransportOrder.WebApi.Controllers;

/// <summary>
/// Provides endpoints for controlling routes within the traffic management. Temporary solution to test the traffic management.
/// </summary>
[ApiController]
[ApiExplorerSettings(GroupName = RestApiGroup.GroupName)]
public class TrafficManagementController(
    IRequestClient<StartRouteToNode> startRouteToNodeClient,
    IRequestClient<AbortRoute> abortRouteClient,
    IRequestClient<StartRouteWithPick> startRouteWithPickClient,
    IRequestClient<StartRouteWithDrop> startRouteWithDropClient,
    IRequestClient<GetRoute> getRouteClient) : Controller
{
    /// <summary>
    /// Gets the current route of the given AGV.
    /// </summary>
    /// <param name="agvId">Id of the AGV whose route should be returned.</param>
    /// <response code="200">The current route of the AGV.</response>
    /// <response code="400">The AGV id is missing.</response>
    /// <response code="404">The AGV or its route was not found.</response>
    [HttpGet]
    [Route("/trafficManagement/getRoute")]
    public async Task<ActionResult<GotRoute>> GetRoute([FromQuery] Guid agvId)
    {
        if (agvId == Guid.Empty)
        {
            return BadRequest();
        }

        var response = await getRouteClient.GetResponse<GotRoute, Rejected>(new GetRoute(agvId));
        if (response.Is(out Response<GotRoute>? gotRoute))
        {
            return Ok(gotRoute.Message);
        }

        return response.Is(out Response<Rejected>? rejected) ? NotFound(rejected.Message.Reason) : NotFound();
    }

    /// <summary>
    /// Starts a route for an AGV to a specific node.
    /// </summary>
    /// <param name="agvId">Id of the AGV which should drive the route.</param>
    /// <param name="nodeShortName">Short name of the target node.</param>
    /// <response code="200">The route was started.</response>
    /// <response code="400">The AGV id or the node short name is missing.</response>
    /// <response code="404">The AGV or the target node was not found.</response>
    [HttpPost]
    [Route("/trafficManagement/startRouteToNode")]
    public async Task<ActionResult> StartRouteToNode([FromQuery] Guid agvId, [FromQuery] string nodeShortName)
    {
        if (agvId == Guid.Empty || string.IsNullOrWhiteSpace(nodeShortName))
        {
            return BadRequest();
        }

        var response = await startRouteToNodeClient.GetResponse<Success, Rejected>(new StartRouteToNode(agvId, nodeShortName));
        return response.Is(out Response<Rejected>? rejected) ? NotFound(rejected.Message.Reason) : Ok();
    }

    /// <summary>
    /// Starts a route for an AGV to a node and performs a pick action at that node.
    /// </summary>
    /// <param name="agvId">Id of the AGV which should drive the route.</param>
    /// <param name="nodeShortName">Short name of the target node.</param>
    /// <response code="200">The route was started.</response>
    /// <response code="400">The AGV id or the node short name is missing.</response>
    /// <response code="404">The AGV or the target node was not found.</response>
    [HttpPost]
    [Route("/trafficManagement/startRouteWithPick")]
    public async Task<ActionResult> StartRouteWithPick([FromQuery] Guid agvId, [FromQuery] string nodeShortName)
    {
        if (agvId == Guid.Empty || string.IsNullOrWhiteSpace(nodeShortName))
        {
            return BadRequest();
        }

        var response = await startRouteWithPickClient.GetResponse<Success, Rejected>(new StartRouteWithPick(agvId, nodeShortName));
        return response.Is(out Response<Rejected>? rejected) ? NotFound(rejected.Message.Reason) : Ok();
    }

    /// <summary>
    /// Starts a route for an AGV to a node and performs a drop action at that node.
    /// </summary>
    /// <param name="agvId">Id of the AGV which should drive the route.</param>
    /// <param name="nodeShortName">Short name of the target node.</param>
    /// <response code="200">The route was started.</response>
    /// <response code="400">The AGV id or the node short name is missing.</response>
    /// <response code="404">The AGV or the target node was not found.</response>
    [HttpPost]
    [Route("/trafficManagement/startRouteWithDrop")]
    public async Task<ActionResult> StartRouteWithDrop([FromQuery] Guid agvId, [FromQuery] string nodeShortName)
    {
        if (agvId == Guid.Empty || string.IsNullOrWhiteSpace(nodeShortName))
        {
            return BadRequest();
        }

        var response = await startRouteWithDropClient.GetResponse<Success, Rejected>(new StartRouteWithDrop(agvId, nodeShortName));
        return response.Is(out Response<Rejected>? rejected) ? NotFound(rejected.Message.Reason) : Ok();
    }

    /// <summary>
    /// Aborts the current route of the given AGV.
    /// </summary>
    /// <param name="agvId">Id of the AGV whose route should be aborted.</param>
    /// <response code="200">The route was aborted.</response>
    /// <response code="400">The AGV id is missing.</response>
    /// <response code="404">The AGV was not found.</response>
    [HttpPost]
    [Route("/trafficManagement/abortRoute")]
    public async Task<ActionResult> AbortRoute([FromQuery] Guid agvId)
    {
        if (agvId == Guid.Empty)
        {
            return BadRequest();
        }

        var response = await abortRouteClient.GetResponse<Success, Rejected>(new AbortRoute(agvId));
        return response.Is(out Response<Rejected>? rejected) ? NotFound(rejected.Message.Reason) : Ok();
    }
}