using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Dtos;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Orders.Common;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services;
using JetBrains.Annotations;
using MassTransit;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Orders.Requests.Queries;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Orders.Requests.Queries.Filtering.DateRange;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Orders.Requests.Queries.Filtering.Pagination;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Orders.Handlers.Queries
{
    /// <summary>
    /// Defines a handler for the GetOrders request
    /// Retrieve active transport orders. If correlation Id is provided, all other parameters are ignored
    /// </summary>
    /// <response code="200">Ok</response>
    [UsedImplicitly]
    public class GetOrdersRequestHandler : IConsumer<GetOrdersRequest>
    {
        private readonly IOrderRegistry _orderRegistry;
        private readonly IMapper _mapper;

        /// <summary>
        /// <inheritdoc cref="GetOrdersRequestHandler"/>
        /// </summary>
        /// <param name="orderRegistry"></param>
        /// <param name="mapper"></param>
        public GetOrdersRequestHandler(IOrderRegistry orderRegistry, IMapper mapper)
        {
            _orderRegistry = orderRegistry;
            _mapper = mapper;
        }

        /// <summary>
        /// <inheritdoc cref="GetOrdersRequestHandler"/>
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task Consume(ConsumeContext<GetOrdersRequest> context)
        {
            var request = context.Message;
            var dateRangeParameters = request.DateRangeParameters;
            var paginationParameters = request.PaginationParameters;

            IOrderedQueryable<IOrder> sortedOrders = _orderRegistry.GetAllOrders().OrderByDescending(x => x.OrderDetails.CreationDate);

            if (dateRangeParameters.IsEmpty() && sortedOrders.Any())
            {
                dateRangeParameters = new DateRangeParameters(sortedOrders.Last().OrderDetails.CreationDate, sortedOrders.First().OrderDetails.CreationDate);
            }

            IQueryable<IOrder> ordersInRange = sortedOrders.Where(order =>
                order.OrderDetails.CreationDate >= dateRangeParameters.Start &&
                order.OrderDetails.CreationDate <= dateRangeParameters.End
            );

            var pagedOrders = new PagedEnumerable<IOrder>(ordersInRange, paginationParameters.PageNumber, paginationParameters.PageSize);
            var pageHeaders = pagedOrders.PaginationHeader();
            var dateRangeHeaders = new DateRangeHeaders(dateRangeParameters.Start, dateRangeParameters.End);
            List<OrderDto> orders = pagedOrders.Select(pageOrder => _mapper.Map<OrderDto>(pageOrder)).ToList();

            await context.RespondAsync(new GetOrdersResult(orders, pageHeaders, dateRangeHeaders));
        }
    }
}