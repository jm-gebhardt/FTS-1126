using Microsoft.AspNetCore.Mvc;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Orders.Requests.Queries.Filtering.DateRange;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Orders.Requests.Queries.Filtering.Pagination;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Orders.Requests.Queries
{
    /// <summary>
    /// Represents the GetOrders request which retrieve active transport orders
    /// If correlation Id is provided, all other parameters are ignored
    /// </summary>
    [JetBrains.Annotations.UsedImplicitly]
    public record GetOrdersRequest
    {
        /// <summary>
        /// The filtering pagination
        /// </summary>
        public PaginationParameters PaginationParameters { get; }

        /// <summary>
        /// The filtering date range
        /// </summary>
        public DateRangeParameters DateRangeParameters { get; }

        /// <inheritdoc cref="GetOrdersRequest"/>
        /// <param name="paginationParameters"></param>
        /// <param name="dateRangeParameters"></param>
        public GetOrdersRequest([FromQuery] PaginationParameters paginationParameters, [FromQuery] DateRangeParameters dateRangeParameters)
        {
            PaginationParameters = paginationParameters;
            DateRangeParameters = dateRangeParameters;
        }
    }
}
