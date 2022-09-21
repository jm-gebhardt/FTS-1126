using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Microsoft.AspNetCore.Mvc;
using Gebhardt.StoreWare.Agv.TransportOrder.Services;
using Gebhardt.StoreWare.Agv.TransportOrder.Web.Controllers.Pagination;
using System.Text.Json;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;

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

        /// <summary>
        /// Init controller
        /// </summary>
        public AgvController(IAgvRegistry agvRegistry, IAgvPositionRegistry agvPositionRegistry)
        {
            _agvRegistry = agvRegistry;
            _agvPositionRegistry = agvPositionRegistry;
        }

        /// <summary>
        /// Retrieve paged list of agvs
        /// </summary>
        /// <response code="200">Ok</response>
        [HttpGet]
        [Route("/agvs")]
        public ActionResult<IEnumerable<Models.Agv>> GetAgvs([FromQuery] int pageNumber = 0, [FromQuery] int pageSize = 0)
        {
            List<Models.Agv> agvs = _agvRegistry.GetAllAgvs().OrderBy(a => a.AgvId).Select(Models.Agv.FromIAgv).ToList();

            if (pageNumber == 0 || pageSize == 0)
            {
                return Ok(agvs);
            }

            var agvList = new PagedEnumerable<Models.Agv>(agvs, pageNumber, pageSize);

            Response.Headers.Add(PaginationHeader.HeaderName, JsonSerializer.Serialize(agvList.PaginationHeader()));
            return Ok(agvList);
        }

        /// <summary>
        /// Retrieve graph for agv
        /// </summary>
        /// <response code="200">Ok</response>
        /// <response code="404">Not found</response>
        [HttpGet]
        [Route("/agvs/{correlationId:guid}/graph")]
        public ActionResult<AgvGraph> GetAgvGraph(Guid correlationId)
        {
            var agv = _agvRegistry.GetAgv(correlationId);
            if (agv == null)
            {
                return NotFound();
            }

            return Ok(agv.AgvGraph);
        }

        /// <summary>
        /// Retrieve status for agv
        /// </summary>
        /// <response code="200">Ok</response>
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
        public ActionResult<IEnumerable<AgvPositionReceived>> GetAgvPositionss()
        {
            return Ok(_agvPositionRegistry.GetAllPositions());
        }
    }
}
