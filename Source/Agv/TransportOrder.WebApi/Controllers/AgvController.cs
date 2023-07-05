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
        private readonly IRequestClient<GetAgvDataRequest> _getAgvDataClient;
        private readonly IRequestClient<GetAgvPositionsRequest> _getAgvPositionsClient;
        private readonly IRequestClient<UnlockAgvCommand> _setIdleClient;
        private readonly IRequestClient<LockAgvCommand> _lockAgvClient;
        private readonly IRequestClient<RequestAgvDataCommand> _emitRequestDataClient;
        private readonly IRequestClient<RequestAgvGraphCommand> _emitRequestGraphClient;
        private readonly IRequestClient<GetActiveAgvErrorsRequest> _getAgvErrorsRequestClient;
        private readonly IRequestClient<GetAgvErrorsRequest> _getAgvErrorHistoryRequestClient;

        /// <summary>
        /// Init controller
        /// </summary>
        public AgvController(IRequestClient<GetAgvsRequest> getAgvsClient,
            IRequestClient<GetAgvGraphRequest> getAgvGraphClient,
            IRequestClient<GetAgvDataRequest> getAgvDataClient,
            IRequestClient<GetAgvPositionsRequest> getAgvPositionsClient,
            IRequestClient<UnlockAgvCommand> setIdleClient,
            IRequestClient<LockAgvCommand> lockAgvClient,
            IRequestClient<RequestAgvDataCommand> emitRequestDataClient,
            IRequestClient<RequestAgvGraphCommand> emitRequestGraphClient,
            IRequestClient<GetActiveAgvErrorsRequest> getAgvErrorsRequestClient,
            IRequestClient<GetAgvErrorsRequest> getAgvErrorHistoryRequestClient)
        {
            _getAgvsClient = getAgvsClient;
            _getAgvGraphClient = getAgvGraphClient;
            _getAgvDataClient = getAgvDataClient;
            _getAgvPositionsClient = getAgvPositionsClient;
            _setIdleClient = setIdleClient;
            _lockAgvClient = lockAgvClient;
            _emitRequestDataClient = emitRequestDataClient;
            _emitRequestGraphClient = emitRequestGraphClient;
            _getAgvErrorsRequestClient = getAgvErrorsRequestClient;
            _getAgvErrorHistoryRequestClient = getAgvErrorHistoryRequestClient;
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

        /// <summary>
        /// Get active errors for given Agv
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("/agvs/{agvCorrelationId}/errors")]
        public async Task<ActionResult<IEnumerable<AgvErrorDto>>> GetAgvErrors(Guid agvCorrelationId)
        {
            Response<GetActiveAgvErrorsResult> response = await _getAgvErrorsRequestClient.GetResponse<GetActiveAgvErrorsResult>(new GetActiveAgvErrorsRequest(agvCorrelationId));
            return Ok(response.Message.AgvErrorsDto);
        }

        /// <summary>
        /// Get all historic (solved and unsolved) errors for given Agv including resolved errors
        /// </summary>
        /// <param name="agvCorrelationId"></param>
        /// <param name="dateFrom">Only consider errors with a ErrorReceived newer than this parameter (if set)</param>
        /// <param name="dateTo">Only consider errors a ErrorReceived older than this parameter (if set)</param>
        /// <returns></returns>
        [HttpGet]
        [Route("/agvs/{agvCorrelationId}/errorHistory")]
        public async Task<ActionResult<IEnumerable<AgvErrorDto>>> GetAgvErrorHistory(Guid agvCorrelationId, DateTime? dateFrom, DateTime? dateTo)
        {
            Response<GetAgvErrorsResult> response = await _getAgvErrorHistoryRequestClient.GetResponse<GetAgvErrorsResult>(new GetAgvErrorsRequest(agvCorrelationId, dateFrom ?? DateTime.MinValue, dateTo ?? DateTime.MaxValue));
            return Ok(response.Message.AgvErrorsDto);
        }
    }
}