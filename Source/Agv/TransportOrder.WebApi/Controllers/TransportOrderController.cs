using Microsoft.AspNetCore.Mvc;
using MassTransit;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Dtos;
using System.Text.Json;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Orders.Common;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Orders.Requests.Commands;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Orders.Requests.Queries;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Orders.Requests.Queries.Filtering.DateRange;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Orders.Requests.Queries.Filtering.Pagination;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Controllers
{
    /// <summary>
    /// Controller for host order data
    /// </summary>
    [ApiController]
    public class TransportOrderController : Controller
    {
        private readonly IRequestClient<ResetOrderCommand> _resetOrderClient;
        private readonly IRequestClient<CompleteOrderCommand> _completeOrderClient;
        private readonly IRequestClient<GetOrdersRequest> _getOrdersClient;

        /// <summary>
        /// Init controller
        /// </summary>
        public TransportOrderController(IRequestClient<ResetOrderCommand> resetOrderClient, IRequestClient<CompleteOrderCommand> completeOrderClient, IRequestClient<GetOrdersRequest> getOrdersClient)
        {
            _resetOrderClient = resetOrderClient;
            _completeOrderClient = completeOrderClient;
            _getOrdersClient = getOrdersClient;
        }

        /// <summary>
        /// Retrieve active transport orders
        /// If correlation Id is provided, all other parameters are ignored
        /// </summary>
        /// <response code="200">Ok</response>
        [HttpGet]
        [Route("/orders")]
        public async Task<ActionResult<IEnumerable<OrderDto>>> GetOrders(
            [FromQuery] PaginationParameters paginationParameters,
            [FromQuery] DateRangeParameters dateRangeParameters)
        {
            Response<GetOrdersResult> response = await _getOrdersClient.GetResponse<GetOrdersResult>(new GetOrdersRequest(paginationParameters, dateRangeParameters));

            Response.Headers.Add(response.Message.PaginationHeaders.HeaderName, JsonSerializer.Serialize(response.Message.PaginationHeaders));
            Response.Headers.Add(response.Message.DateRangeHeaders.HeaderName, JsonSerializer.Serialize(response.Message.DateRangeHeaders));

            return Ok(response.Message.Orders);
        }

        /// <summary>
        /// Resets order and starts a new transport
        /// </summary>
        /// <response code="200">Ok: Order was reset</response>
        /// <response code="400">BadRequest: Order is not completed yet</response>
        /// <response code="404">Not Found: Order with the given correlationId does not exist</response>
        [HttpPatch]
        [Route("/orders/{correlationId}/resetOrder")]
        public async Task<ActionResult> ResetOrder(Guid correlationId)
        {
            await _resetOrderClient.GetResponse<ResetOrderResult>(new ResetOrderCommand(correlationId));
            return Ok();
        }

        /// <summary>
        /// Completes order manually
        /// </summary>
        /// <response code="200">Ok: Order was completed</response>
        /// <response code="404">Not Found: Order with the given correlationId does not exist</response>
        [HttpPatch]
        [Route("/orders/{correlationId}/completeOrder")]
        public async Task<ActionResult> CompleteOrder(Guid correlationId)
        {
            await _completeOrderClient.GetResponse<CompleteOrderResult>(new CompleteOrderCommand(correlationId));
            return Ok();
        }
    }
}
