using Gebhardt.StoreWare.Agv.TransportOrder.Core;
using Microsoft.AspNetCore.Mvc;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Controllers
{
    /// <summary>
    /// Controller for meta data
    /// </summary>
    [ApiController]
    public class MetaController : Controller
    {
        /// <summary>
        /// Get available SignalR hub methods
        /// </summary>
        /// <response code="200">Ok</response>
        [HttpGet]
        [Route("/meta/hubMethods")]
        public ActionResult<IEnumerable<Defines.HubMethods>> GetHubMethods()
        {
            return Enum.GetValues<Defines.HubMethods>();
        }
    }
}
