using System.Text.Json;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
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
		[Route("/order")]
		public ActionResult<IEnumerable<Order>> GetOrders([FromQuery] PaginationParameters paginationParameters)
		{
			PagedEnumerable<IOrder> orders = PagedEnumerable<IOrder>.ToPagedList(
				_orderRegistry.GetAllOrders().OrderByDescending(x => x.OrderDetails.CreationDate), 
				paginationParameters.PageNumber, 
				paginationParameters.PageSize);
			
			Response.Headers.Add(PaginationHeader.HeaderName, JsonSerializer.Serialize(orders.PaginationHeader()));
			return Ok(orders.Select(Order.FromIOrder));
		}
	}
}
