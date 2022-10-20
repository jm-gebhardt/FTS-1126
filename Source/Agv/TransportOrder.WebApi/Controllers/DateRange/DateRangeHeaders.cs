using JetBrains.Annotations;

// ReSharper disable ConvertToPrimaryConstructor
namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Controllers.DateRange
{
    /// <summary>
    /// Pagination X Headers
    /// </summary>
    [PublicAPI]
    public record DateRangeHeaders: IResultHeaders
    {
        /// <summary>
        /// Header name
        /// </summary>
        public string HeaderName => "X-DateRange";
        /// <summary>
        /// Current Page
        /// </summary>
        public DateTime Start { get; }

        /// <summary>
        /// Total number of pages
        /// </summary>
        public DateTime End { get; }
        
        /// <summary>
        /// Pagination X Headers Constructor
        /// </summary>
        public DateRangeHeaders(DateTime start, DateTime end)
        {
            Start = start;
            End = end;
        }
    }
}
