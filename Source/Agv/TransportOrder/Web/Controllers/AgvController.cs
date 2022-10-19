using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Microsoft.AspNetCore.Mvc;
using Gebhardt.StoreWare.Agv.TransportOrder.Services;
using Gebhardt.StoreWare.Agv.TransportOrder.Web.Dtos;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Commands;
using MassTransit;
using AutoMapper;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Web.Controllers
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
        public ActionResult<IEnumerable<Dtos.Agv>> GetAgvs()
        {
            IOrderedQueryable<IAgv> agvs = _agvRegistry.GetAllAgvs().OrderBy(a => a.AgvId);
            return Ok(agvs.Select(agv => _mapper.Map<Dtos.Agv>(agv)));
        }

        /// <summary>
        /// Retrieve graph for agv
        /// </summary>
        /// <response code="200">Ok</response>
        /// <response code="204">No content</response>
        /// <response code="404">Not found</response>
        [HttpGet]
        [Route("/agvs/{correlationId:guid}/graph")]
        public ActionResult<Graph> GetAgvGraph(Guid correlationId)
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

            var graph = _mapper.Map<Graph>(agv.AgvGraph);

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
        /// <response code="400">Bad Request: AGV is not in State  <see cref="AgvStateMachine.Discovered"/></response>
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

            if (agv.CurrentState != nameof(AgvStateMachine.Discovered))
            {
                return BadRequest("Agv cannot be set 'Idle' because it is not in state 'Discovered'");
            }

            await _bus.Publish(new ReleaseAgv(agv.CorrelationId));
            return Ok();
        }
    }
}
