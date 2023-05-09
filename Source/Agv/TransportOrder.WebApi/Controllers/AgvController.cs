using Microsoft.AspNetCore.Mvc;
using MassTransit;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Dtos;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Models;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Common;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Requests.Queries;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Requests.Commands;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Controllers
{
    /// <summary>
    /// Controller for host order data
    /// </summary>
    [ApiController]
    public class AgvController : Controller
    {
        private readonly IRequestClient<GetAgvsRequest> _getAgvsClient;
        private readonly IRequestClient<GetAgvGraphRequest> _getAgvGraphClient;
        private readonly IRequestClient<GetAgvStatusRequest> _getAgvStatusClient;
        private readonly IRequestClient<GetAgvDataRequest> _getAgvDataClient;
        private readonly IRequestClient<GetAgvPositionsRequest> _getAgvPositionsClient;
        private readonly IRequestClient<UnlockAgvCommand> _setIdleClient;
        private readonly IRequestClient<LockAgvCommand> _lockAgvClient;
        private readonly IRequestClient<RequestAgvDataCommand> _emitRequestDataClient;
        private readonly IRequestClient<RequestAgvGraphCommand> _emitRequestGraphClient;

        /// <summary>
        /// Init controller
        /// </summary>
        public AgvController(IRequestClient<GetAgvsRequest> getAgvsClient,
                             IRequestClient<GetAgvGraphRequest> getAgvGraphClient,
                             IRequestClient<GetAgvStatusRequest> getAgvStatusClient,
                             IRequestClient<GetAgvDataRequest> getAgvDataClient,
                             IRequestClient<GetAgvPositionsRequest> getAgvPositionsClient,
                             IRequestClient<UnlockAgvCommand> setIdleClient,
                             IRequestClient<LockAgvCommand> lockAgvClient,
                             IRequestClient<RequestAgvDataCommand> emitRequestDataClient,
                             IRequestClient<RequestAgvGraphCommand> emitRequestGraphClient)
        {
            _getAgvsClient = getAgvsClient;
            _getAgvGraphClient = getAgvGraphClient;
            _getAgvStatusClient = getAgvStatusClient;
            _getAgvDataClient = getAgvDataClient;
            _getAgvPositionsClient = getAgvPositionsClient;
            _setIdleClient = setIdleClient;
            _lockAgvClient = lockAgvClient;
            _emitRequestDataClient = emitRequestDataClient;
            _emitRequestGraphClient = emitRequestGraphClient;
        }

        /// <summary>
        /// Retrieve paged list of agvs
        /// </summary>
        /// <response code="200">Ok</response>
        [HttpGet]
        [Route("/agvs")]
        public async Task<ActionResult<IEnumerable<AgvDto>>> GetAgvs()
        {
            Response<GetAgvsResult> response = await _getAgvsClient.GetResponse<GetAgvsResult>(new GetAgvsRequest());
            return Ok(response.Message.Agvs);
        }

        /// <summary>
        /// Retrieve graph for agv
        /// </summary>
        /// <response code="200">Ok</response>
        /// <response code="204">No content</response>
        /// <response code="404">Not found</response>
        [HttpGet]
        [Route("/agvs/{correlationId:guid}/graph")]
        public async Task<ActionResult<GraphDto>> GetAgvGraph(Guid correlationId)
        {
            Response<GetAgvGraphResult> response = await _getAgvGraphClient.GetResponse<GetAgvGraphResult>(new GetAgvGraphRequest(correlationId));
            return Ok(response.Message.AgvGraph);
        }

        /// <summary>
        /// Retrieve status for agv
        /// </summary>
        /// <response code="200">Ok</response>
        /// <response code="204">No content</response>
        /// <response code="404">Not found</response>
        [HttpGet]
        [Route("/agvs/{correlationId:guid}/status")]
        public async Task<ActionResult<AgvStateDto>> GetAgvStatus(Guid correlationId)
        {
            Response<GetAgvStateResult> response = await _getAgvStatusClient.GetResponse<GetAgvStateResult>(new GetAgvStatusRequest(correlationId));
            return Ok(response.Message.AgvState);
        }

        /// <summary>
        /// Retrieve data for agv
        /// </summary>
        /// <response code="200">Ok</response>
        /// <response code="204">No Content</response>
        /// <response code="404">Not found</response>
        [HttpGet]
        [Route("/agvs/{correlationId}/data")]
        public async Task<ActionResult<AgvDataDto>> GetAgvData(Guid correlationId)
        {
            Response<GetAgvDataResult> response = await _getAgvDataClient.GetResponse<GetAgvDataResult>(new GetAgvDataRequest(correlationId));
            return Ok(response.Message.AgvData);
        }

        /// <summary>
        /// Retrieve list of agv positions
        /// </summary>
        /// <response code="200">Ok</response>
        [HttpGet]
        [Route("/agvsPositions")]
        public async Task<ActionResult<IEnumerable<AgvPositionStatus>>> GetAgvPositions()
        {
            Response<GetAgvPositionsResult> response = await _getAgvPositionsClient.GetResponse<GetAgvPositionsResult>(new GetAgvPositionsRequest());
            return Ok(response.Message.AgvPositions);
        }

        /// <summary>
        /// Unlock AGV to allow it to be assigned to transport orders
        /// </summary>
        /// <response code="200">Ok: AGV status was successfully set to Idle</response>
        /// <response code="400">Bad Request: AGV is not in State 'Discovered'</response>
        /// <response code="404">Not Found: AGV with the given correlationId does not exist</response>
        [HttpPatch]
        [Route("/agvs/{correlationId}/unlock")]
        public async Task<ActionResult> UnlockAgv(Guid correlationId)
        {
            await _setIdleClient.GetResponse<UnlockAgvResult>(new UnlockAgvCommand(correlationId));
            return Ok();
        }

        /// <summary>
        /// Lock AGV to prevent it from being assigned to transport orders
        /// </summary>
        /// <response code="200">Ok: AGV status was successfully unlocked</response>
        /// <response code="404">Not Found: AGV with the given correlationId does not exist</response>
        [HttpPatch]
        [Route("/agvs/{correlationId}/lock")]
        public async Task<ActionResult> LockAgv(Guid correlationId)
        {
            await _lockAgvClient.GetResponse<LockAgvResult>(new LockAgvCommand(correlationId));
            return Ok();
        }

        /// <summary>
        /// Emit RequestData telegram when receiving PATCH request
        /// </summary>
        /// <response code="200">Ok: Telegram was successfully emitted</response>
        /// <response code="404">Not Found: AGV with the given correlationId does not exist</response>
        [HttpPatch]
        [Route("/agvs/{correlationId}/emitRequestData")]
        public async Task<ActionResult> EmitRequestData(Guid correlationId)
        {
            await _emitRequestDataClient.GetResponse<RequestAgvDataResult>(new RequestAgvDataCommand(correlationId));
            return Ok();
        }

        /// <summary>
        /// Emit RequestGraph telegram when receiving PATCH request
        /// </summary>
        /// <response code="200">Ok: Telegram was successfully emitted</response>
        /// <response code="404">Not Found: AGV with the given correlationId does not exist</response>
        [HttpPatch]
        [Route("/agvs/{correlationId}/emitRequestGraph")]
        public async Task<ActionResult> EmitRequestGraph(Guid correlationId)
        {
            await _emitRequestGraphClient.GetResponse<RequestAgvGraphResult>(new RequestAgvGraphCommand(correlationId));
            return Ok();
        }
    }
}
