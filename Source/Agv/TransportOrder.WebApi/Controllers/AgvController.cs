using Microsoft.AspNetCore.Mvc;
using AutoMapper;
using MassTransit;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Commands;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Dtos;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Models;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Controllers
{
    /// <summary>
    /// Controller for host order data
    /// </summary>
    [ApiController]
    public class AgvController : Controller
    {
        private readonly IAgvRegistry _agvRegistry;
        private readonly IAgvPositionRegistry _agvPositionRegistry;
        private readonly IBus _bus;
        private readonly IMapper _mapper;

        /// <summary>
        /// Init controller
        /// </summary>
        public AgvController(IAgvRegistry agvRegistry, IAgvPositionRegistry agvPositionRegistry, IBus bus, IMapper mapper)
        {
            _agvRegistry = agvRegistry;
            _agvPositionRegistry = agvPositionRegistry;
            _bus = bus;
            _mapper = mapper;
        }

        /// <summary>
        /// Retrieve paged list of agvs
        /// </summary>
        /// <response code="200">Ok</response>
        [HttpGet]
        [Route("/agvs")]
        public ActionResult<IEnumerable<AgvDto>> GetAgvs()
        {
            IOrderedQueryable<IAgv> agvs = _agvRegistry.GetAllAgvs().OrderBy(a => a.AgvId);
            return Ok(agvs.Select(agv => _mapper.Map<AgvDto>(agv)));
        }

        /// <summary>
        /// Retrieve graph for agv
        /// </summary>
        /// <response code="200">Ok</response>
        /// <response code="204">No content</response>
        /// <response code="404">Not found</response>
        [HttpGet]
        [Route("/agvs/{correlationId:guid}/graph")]
        public ActionResult<GraphDto> GetAgvGraph(Guid correlationId)
        {
            var agv = _agvRegistry.GetAgv(correlationId);
            if (agv == null)
            {
                return NotFound();
            }

            if (agv.AgvGraph == null)
            {
                return NoContent();
            }

            var graph = _mapper.Map<GraphDto>(agv.AgvGraph);

            return Ok(graph);
        }

        /// <summary>
        /// Retrieve status for agv
        /// </summary>
        /// <response code="200">Ok</response>
        /// <response code="204">No content</response>
        /// <response code="404">Not found</response>
        [HttpGet]
        [Route("/agvs/{correlationId:guid}/status")]
        public ActionResult<AgvStatus> GetAgvStatus(Guid correlationId)
        {
            var agv = _agvRegistry.GetAgv(correlationId);
            if (agv == null)
            {
                return NotFound();
            }

            return Ok(agv.AgvStatus);
        }

        /// <summary>
        /// Retrieve data for agv
        /// </summary>
        /// <response code="200">Ok</response>
        /// <response code="204">No Content</response>
        /// <response code="404">Not found</response>
        [HttpGet]
        [Route("/agvs/{correlationId}/data")]
        public ActionResult<AgvData> GetAgvData(Guid correlationId)
        {
            var agv = _agvRegistry.GetAgv(correlationId);
            if (agv == null)
            {
                return NotFound();
            }

            return Ok(agv.AgvData);
        }

        /// <summary>
        /// Retrieve list of agv positions
        /// </summary>
        /// <response code="200">Ok</response>
        [HttpGet]
        [Route("/agvsPositions")]
        public ActionResult<IEnumerable<AgvPositionStatus>> GetAgvPositions()
        {
            return Ok(_agvPositionRegistry.GetAllPositions());
        }

        /// <summary>
        /// Set status of agv from Discovered to Idle
        /// </summary>
        /// <response code="200">Ok: AGV status was successfully set to Idle</response>
        /// <response code="400">Bad Request: AGV is not in State 'Discovered'</response>
        /// <response code="404">Not Found: AGV with the given correlationId does not exist</response>
        [HttpPatch]
        [Route("/agvs/{correlationId}/setIdle")]
        public async Task<ActionResult> SetIdle(Guid correlationId)
        {
            var agv = _agvRegistry.GetAgv(correlationId);

            if (agv == null)
            {
                return NotFound();
            }

            if (agv.CurrentState != nameof(AgvStateMachine.Discovered) && agv.CurrentState != nameof(AgvStateMachine.Locked))
            {
                return BadRequest("Agv cannot be released because it is still busy");
            }

            await _bus.Publish(new ForceReleaseAgv(agv.CorrelationId));
            return Ok();
        }

        /// <summary>
        /// Lock AGV to prevent it from being assigned to transport orders
        /// </summary>
        /// <response code="200">Ok: AGV status was successfully set to Idle</response>
        /// <response code="404">Not Found: AGV with the given correlationId does not exist</response>
        [HttpPatch]
        [Route("/agvs/{correlationId}/lockAgv")]
        public async Task<ActionResult> LockAgv(Guid correlationId)
        {
            var agv = _agvRegistry.GetAgv(correlationId);

            if (agv == null)
            {
                return NotFound();
            }

            await _bus.Publish(new LockAgv(agv.CorrelationId));
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
            var agv = _agvRegistry.GetAgv(correlationId);

            if (agv == null)
            {
                return NotFound();
            }

            await _bus.Publish(new RequestAgvData(agv.CorrelationId, agv.AgvId));
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
            var agv = _agvRegistry.GetAgv(correlationId);

            if (agv == null)
            {
                return NotFound();
            }

            await _bus.Publish(new RequestAgvGraph(agv.CorrelationId, agv.AgvId));
            return Ok();
        }
    }
}
