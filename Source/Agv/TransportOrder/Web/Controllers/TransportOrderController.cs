using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Gebhardt.StoreWare.Agv.TransportOrder.Services;
using Gebhardt.StoreWare.Agv.TransportOrder.Web.Controllers.Pagination;
using Gebhardt.StoreWare.Agv.TransportOrder.Web.Models;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Web.Controllers
{
    /// <summary>
    /// Controller for host order data
    /// </summary>
    [ApiController]
    public class TransportOrderController : Controller
    {
        private readonly IOrderRegistry _orderRegistry;

        /// <summary>
        /// Init controller
        /// </summary>
        public TransportOrderController(IOrderRegistry orderRegistry)
        {
            _orderRegistry = orderRegistry;
        }

        /// <summary>
        /// Retrieve active transport orders
        /// </summary>
        /// <response code="200">Ok</response>
        [HttpGet]
        [Route("/orders")]
        public ActionResult<IEnumerable<Order>> GetOrders([FromQuery] PaginationParameters paginationParameters)
        {
            List<Order> orders = _orderRegistry.GetAllOrders().OrderByDescending(x => x.OrderDetails.CreationDate).Select(Order.FromIOrder).ToList();
            var orderList = new PagedEnumerable<Order>(orders, paginationParameters.PageNumber, paginationParameters.PageSize);

            Response.Headers.Add(PaginationHeader.HeaderName, JsonSerializer.Serialize(orderList.PaginationHeader()));
            return Ok(orderList);
        }
    }
}
