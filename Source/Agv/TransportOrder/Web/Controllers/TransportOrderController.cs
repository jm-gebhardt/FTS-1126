using Microsoft.AspNetCore.Mvc;
using Gebhardt.StoreWare.Agv.TransportOrder.Services;

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
		public IActionResult GetOrders()
		{
			return Ok(_orderRegistry.GetAllOrders().Select(order => order.OrderDetails).ToList());
		}
	}
}
