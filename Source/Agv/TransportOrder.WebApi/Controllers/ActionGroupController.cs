using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Dtos.AgvActions;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Actions.Common;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Actions.Requests.Queries;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using static MassTransit.Transports.ReceiveEndpoint;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Controllers
{
    /// <summary>
    /// Controller for <see cref="AgvAction"/>
    /// </summary>
    public class ActionGroupController : Controller
    {
        private readonly IRequestClient<GetAgvActionGroupsRequest> _getAgvActionGroupsRequest;
        private readonly IRequestClient<GetAgvActionGroupCountsByStateRequest> _getAgvActionGroupCountsByStateRequest;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="getAgvActionGroupsRequest"></param>
        /// <param name="getAgvActionGroupCountsByStateRequest"></param>
        public ActionGroupController(IRequestClient<GetAgvActionGroupsRequest> getAgvActionGroupsRequest, IRequestClient<GetAgvActionGroupCountsByStateRequest> getAgvActionGroupCountsByStateRequest)
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
        public async Task<ActionResult<IEnumerable<ActionGroupDto>>> GetActionGroups(ActionGroupState[] states, DateTime? fromDate, DateTime? toDate, int limit = 1000)
        {
            // DEBUG
            if (limit == 1)
            {
                var groupId = Guid.Parse("10106e12-84f6-0301-daa0-138020db9422");
                var assignedAgv = "karis0".ToGuid();

                var result = new ActionGroupDto(
                    groupId,
                    "Order#1",
                    assignedAgv, // AssignedAgv
                    DateTime.Now, // createdDate
                    null, // startedDate
                    null, // completedDate
                    ActionGroupState.Created,
                    new List<AgvActionDto>()
                    {
                        new AgvActionDto(
                            Guid.Parse("e2bbb93d-3ff2-451a-92f8-1fa01bd413b3"),
                            groupId,
                            0,
                            ActionState.Created,
                            ActionType.Pick,
                            "PVB02",
                            new string[0],
                            DateTime.Now, // createdDate
                            null, // startedDate
                            null // completedDate
                        ),
                        new AgvActionDto(
                            Guid.Parse("65469fa1-548a-446f-a8de-ad1cbb7c16ad"),
                            groupId,
                            1,
                            ActionState.Created,
                            ActionType.Drop,
                            "PVB03",
                            new string[0],
                            DateTime.Now, // createdDate
                            null, // startedDate
                            null // completedDate
                        )
                    }
                );

                await Task.CompletedTask;
                return Ok(result);
            }

            Response<GetAgvActionGroupsResult> groups = await _getAgvActionGroupsRequest.GetResponse<GetAgvActionGroupsResult>(new GetAgvActionGroupsRequest(
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