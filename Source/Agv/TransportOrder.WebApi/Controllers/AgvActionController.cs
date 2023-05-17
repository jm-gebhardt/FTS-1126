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
        private readonly IRequestClient<GetAgvActionGroupCountsByStateRequest> _getAgvActionGroupCountsByStateRequest;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="getAgvActionGroupsRequest"></param>
        /// <param name="getAgvActionGroupCountsByStateRequest"></param>
        public AgvActionController(IRequestClient<GetAgvActionGroupsRequest> getAgvActionGroupsRequest, IRequestClient<GetAgvActionGroupCountsByStateRequest> getAgvActionGroupCountsByStateRequest)
        {
            _getAgvActionGroupsRequest = getAgvActionGroupsRequest;
            _getAgvActionGroupCountsByStateRequest = getAgvActionGroupCountsByStateRequest;
        }

        /// <summary>
        ///  Retrieve actions
        /// </summary>
        /// <response code="200">Ok</response>
        [HttpGet]
        [Route("/actionGroups")]
        public async Task<ActionResult<IEnumerable<AgvActionGroupDto>>> GetActionGroups(ActionGroupState[] states, DateTime? fromDate, DateTime? toDate, int limit=1000)
        {
            Response<GetAgvActionGroupsResult> groups = await _getAgvActionGroupsRequest.GetResponse<GetAgvActionGroupsResult>( new GetAgvActionGroupsRequest(
                states, 
                fromDate ?? DateTime.MinValue, 
                toDate ?? DateTime.MaxValue, 
                limit));
            return Ok(groups.Message.AgvActionGroups);
        }

        /// <summary>
        /// Get Action Group Meta
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("/actionGroups/CountsByState")]
        public async Task<ActionResult<IEnumerable<AgvGroupCountByStateDto>>> GetActionGroupCountsByState()
        {
            Response<GetAgvActionGroupCountsByStateResult> counts = await _getAgvActionGroupCountsByStateRequest.GetResponse<GetAgvActionGroupCountsByStateResult>(new GetAgvActionGroupCountsByStateRequest());
            return Ok(counts.Message.AgvGroupCountByState);
        }
    }
}