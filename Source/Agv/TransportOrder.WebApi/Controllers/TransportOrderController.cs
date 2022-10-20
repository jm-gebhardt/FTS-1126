using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Common.Services;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Controllers.DateRange;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Controllers.Pagination;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Controllers
{
    /// <summary>
    /// Controller for host order data
    /// </summary>
    [ApiController]
    public class TransportOrderController : Controller
    {
        private readonly IOrderRegistry _orderRegistry;
        private readonly IMapper _mapper;

        /// <summary>
        /// Init controller
        /// </summary>
        public TransportOrderController(IOrderRegistry orderRegistry, IMapper mapper)
        {
            _orderRegistry = orderRegistry;
            _mapper = mapper;
        }

        /// <summary>
        /// Retrieve active transport orders
        /// If correlation Id is provided, all other parameters are ignored
        /// </summary>
        /// <response code="200">Ok</response>
        [HttpGet]
        [Route("/orders")]
        public ActionResult<IEnumerable<Order>> GetOrders(
            [FromQuery] PaginationParameters paginationParameters,
            [FromQuery] DateRangeParameters dateRangeParameters
            )
        {
            IOrderedQueryable<IOrder> sortedOrders = _orderRegistry.GetAllOrders().OrderByDescending(x => x.OrderDetails.CreationDate);
            
            if(dateRangeParameters.IsEmpty() && sortedOrders.Any())
            {
                dateRangeParameters = new DateRangeParameters(sortedOrders.Last().OrderDetails.CreationDate, sortedOrders.First().OrderDetails.CreationDate);
            }
            
            IQueryable<IOrder> ordersInRange = sortedOrders.Where(order =>
                order.OrderDetails.CreationDate >= dateRangeParameters.Start && 
                order.OrderDetails.CreationDate <= dateRangeParameters.End
            );
            
            var pagedOrders = new PagedEnumerable<IOrder>(ordersInRange, paginationParameters.PageNumber, paginationParameters.PageSize);
            
            var pageHeaders = pagedOrders.PaginationHeader();
            Response.Headers.Add(pageHeaders.HeaderName, JsonSerializer.Serialize(pageHeaders));

            var dateRangeHeaders = new DateRangeHeaders(dateRangeParameters.Start, dateRangeParameters.End);
            Response.Headers.Add(dateRangeHeaders.HeaderName, JsonSerializer.Serialize(dateRangeHeaders));
            
            return Ok(pagedOrders.Select(pageOrder => _mapper.Map<Order>(pageOrder)));
        }
    }
}
