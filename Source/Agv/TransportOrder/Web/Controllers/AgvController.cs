using Microsoft.AspNetCore.Mvc;
using Gebhardt.StoreWare.Agv.TransportOrder.Services;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Web.Controllers
{
    /// <summary>
    /// Controller for host order data
    /// </summary>
    [ApiController]
    public class AgvController : Controller
    {
        private readonly IAgvRegistry _agvRegistry;

        /// <summary>
        /// Init controller
        /// </summary>
        public AgvController(IAgvRegistry agvRegistry)
        {
            _agvRegistry = agvRegistry;
        }

        /// <summary>
        /// Retrieve active agvs
        /// </summary>
        /// <response code="200">Ok</response>
        [HttpGet]
        [Route("/agv")]
        public async Task<IActionResult> GetAgvs()
        {
            List<Models.Agv> agvs = new();
            await Task.Run(() =>
            {
                foreach (var agv in _agvRegistry.GetAllAgvs())
                {
                    agvs.Add(new Models.Agv(agv.CorrelationId, agv.AgvId, agv.CurrentState));
                }
            });

            return Ok(agvs);
        }

        /// <summary>
        /// Retrieve graph for agv
        /// </summary>
        /// <response code="200">Ok</response>
        /// <response code="404">Not found</response>
        [HttpGet]
        [Route("/agv/{correlationId}/graph")]
        public IActionResult GetAgvGraph(Guid correlationId)
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
        [Route("/agv/{correlationId}/status")]
        public IActionResult GetAgvStatus(Guid correlationId)
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
        [Route("/agv/{correlationId}/data")]
        public IActionResult GetAgvData(Guid correlationId)
        {
            var agv = _agvRegistry.GetAgv(correlationId);
            if (agv == null)
            {
                return NotFound();
            }

            return Ok(agv.AgvData);
        }
    }
}
