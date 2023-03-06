using Gebhardt.StoreWare.Agv.TransportOrder.Core.Dtos;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Orders.Requests.Queries.Filtering.DateRange;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Orders.Requests.Queries.Filtering.Pagination;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Orders.Common
{
    /// <summary>
    /// Defines a result for the GetOrders request
    /// </summary>
    public record GetOrdersResult(List<OrderDto> Orders, PaginationHeaders PaginationHeaders, DateRangeHeaders DateRangeHeaders);
}
