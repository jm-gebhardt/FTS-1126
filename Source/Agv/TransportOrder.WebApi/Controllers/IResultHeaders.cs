using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Controllers
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