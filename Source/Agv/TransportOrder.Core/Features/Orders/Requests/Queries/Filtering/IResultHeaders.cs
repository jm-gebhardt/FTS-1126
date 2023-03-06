using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Orders.Requests.Queries.Filtering
{
    /// <summary>
    /// HTTP Result Header
    /// </summary>
    [PublicAPI]
    public interface IResultHeaders
    {
        /// <summary>
        /// Header Name
        /// </summary>
        public string HeaderName { get; }
    }
}