using System.ComponentModel.DataAnnotations;
using AutoMapper;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.TransportOrder.Domain.Models.Polygon;
using Gebhardt.Acc.TransportOrder.Messages.Requests.RestrictedArea;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.RestrictedArea;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Trigger;
using Gebhardt.Acc.TransportOrder.WebApi.Policies;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gebhardt.Acc.TransportOrder.WebApi.Controllers.RestrictedArea;

/// <summary>
/// Restricted area requests
/// </summary>
[ApiExplorerSettings(GroupName = RestApiGroup.GroupName)]
[Authorize]
public class RestrictedAreaController : Controller
{
    private readonly IMapper _mapper;
    private readonly IRequestClient<GetRestrictedArea> _getRestrictedAreaClient;
    private readonly IRequestClient<AddRestrictedArea> _addRestrictedAreaClient;
    private readonly IRequestClient<TryDeleteRestrictedArea> _deleteRestrictedAreaClient;
    private readonly IRequestClient<TryUpdateRestrictedArea> _updateRestrictedAreaClient;

    /// <inheritdoc cref="RestrictedAreaController"/>>
    public RestrictedAreaController(IMapper mapper,
        IRequestClient<GetRestrictedArea> getRestrictedAreaClient,
        IRequestClient<AddRestrictedArea> addRestrictedAreaClient,
        IRequestClient<TryUpdateRestrictedArea> updateRestrictedAreaClient,
        IRequestClient<TryDeleteRestrictedArea> deleteRestrictedAreaClient)
    {
        _mapper = mapper;
        _getRestrictedAreaClient = getRestrictedAreaClient;
        _addRestrictedAreaClient = addRestrictedAreaClient;
        _deleteRestrictedAreaClient = deleteRestrictedAreaClient;
        _updateRestrictedAreaClient = updateRestrictedAreaClient;
    }
    
    /// <summary>
    /// Get a restricted area.
    /// </summary>
    /// <param name="id">The id of the restricted area.</param>
    /// <response code="200">Ok</response>
    /// <response code="404">Restricted area with given id not found.</response>
    [HttpGet]
    [Route("/restrictedArea/{id:guid}")]
    public async Task<ActionResult<RestrictedAreaDto>> GetRestrictedArea([FromRoute] Guid id)
    {
        Response<GotRestrictedArea> response = await _getRestrictedAreaClient.GetResponse<GotRestrictedArea>(new GetRestrictedArea(id));
        var restrictedArea = _mapper.Map<RestrictedAreaDto>(response.Message.RestrictedArea);
        return Ok(restrictedArea);
    }

    /// <summary>
    /// Add a restricted area.
    /// </summary>
    /// <param name="area">The restricted area polygon.</param>
    /// <response code="200">Ok</response>
    /// <response code="400">Invalid polygon with less than three points provided.</response>
    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.RestrictedAreasFullAccess)]
    [Route("/restrictedArea")]
    public async Task<ActionResult<RestrictedAreaDto>> AddRestrictedArea([FromBody] [Required] ICollection<PolygonPointDto> area)
    {
        Response<RestrictedAreaAdded> response = await _addRestrictedAreaClient.GetResponse<RestrictedAreaAdded>(new AddRestrictedArea(_mapper.Map<ICollection<PolygonPoint>>(area)));
        var restrictedArea = _mapper.Map<RestrictedAreaDto>(response.Message.RestrictedArea);
        return Ok(restrictedArea);
    }

    /// <summary>
    /// Delete a restricted area.
    /// </summary>
    /// <param name="id">The id of the restricted area.</param>
    /// <response code="200">Ok</response>
    /// <response code="404">Restricted area with given id not found.</response>
    [HttpDelete]
    [Authorize(Policy = AuthorizationPolicies.RestrictedAreasFullAccess)]
    [Route("/restrictedArea/{id:guid}")]
    public async Task<ActionResult> DeleteRestrictedArea([FromRoute] Guid id)
    {
        await _deleteRestrictedAreaClient.GetResponse<Success>(new TryDeleteRestrictedArea(id));
        return Ok();
    }

    /// <summary>
    /// Update a restricted area.
    /// </summary>
    /// <param name="id">The id of the restricted area.</param>
    /// <param name="area">The updated restricted area polygon.</param>
    /// <response code="200">Ok</response>
    /// <response code="404">Restricted area with given id not found.</response>
    /// <response code="400">Invalid polygon with less than three points provided.</response>
    [HttpPatch]
    [Authorize(Policy = AuthorizationPolicies.RestrictedAreasFullAccess)]
    [Route("/restrictedArea/{id:guid}")]
    public async Task<ActionResult> UpdateRestrictedArea([FromRoute] Guid id, [FromBody][Required] ICollection<PolygonPointDto> area)
    {
         await _updateRestrictedAreaClient.GetResponse<Success>(new TryUpdateRestrictedArea(id, _mapper.Map<ICollection<PolygonPoint>>(area)));
        return Ok();
    }
}