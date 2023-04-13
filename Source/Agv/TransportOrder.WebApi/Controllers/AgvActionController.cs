using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Dtos;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Actions.Common;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Actions.Requests.Queries;
using MassTransit;
using Microsoft.AspNetCore.Mvc;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Controllers
{
    /// <summary>
    /// Controller for <see cref="AgvAction"/>
    /// </summary>
    public class AgvActionController: Controller
    {


        private readonly IRequestClient<GetAgvActionGroupsRequest> _getAgvActionGroupsRequest;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="getAgvActionGroupsRequest"></param>
        public AgvActionController(IRequestClient<GetAgvActionGroupsRequest> getAgvActionGroupsRequest)
        {
            _getAgvActionGroupsRequest = getAgvActionGroupsRequest;
        }

        /// <summary>
        ///  Retrieve actions
        /// </summary>
        /// <response code="200">Ok</response>
        [HttpGet]
        [Route("/agvGroups")]
        public async Task<ActionResult<IEnumerable<AgvActionGroupDto>>> GetActions(ActionState[] states, DateTime toDate, int limit)
        {
            Response<GetAgvActionGroupsResult> groups = await _getAgvActionGroupsRequest.GetResponse<GetAgvActionGroupsResult>( new GetAgvActionGroupsRequest(states, toDate, limit));
                
            // Response<GetOrdersResult> response = await _getOrdersClient.GetResponse<GetOrdersResult>(new GetOrdersRequest(paginationParameters, dateRangeParameters));
            //
            // Response.Headers.Add(response.Message.PaginationHeaders.HeaderName, JsonSerializer.Serialize(response.Message.PaginationHeaders));
            // Response.Headers.Add(response.Message.DateRangeHeaders.HeaderName, JsonSerializer.Serialize(response.Message.DateRangeHeaders));
            //
            // return Ok(response.Message.Orders);
            await Task.Delay(1);
            return Ok();
        }
    }
}