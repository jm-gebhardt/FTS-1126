using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Web.Controllers.DateRange
{
    /// <summary>
    /// Date range parameter
    /// </summary>
    [PublicAPI]
    public class DateRangeParameters
    {
        /// <summary>
        /// Start date of range
        /// </summary>
        public DateTime Start { get; set; }

        /// <summary>
        /// End date of range
        /// </summary>
        public DateTime End { get; set; }

        /// <summary>
        /// Date range parameter constructor
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        public DateRangeParameters(DateTime start, DateTime end)
        {
            Start = start;
            End = end;
        }

        /// <summary>
        /// Empty Date range parameter constructor
        /// </summary>
        public DateRangeParameters()
        {
            
        }

        /// <summary>
        /// Are Start end End equal to default
        /// </summary>
        /// <returns>True when Start end End equal to default</returns>
        public bool IsEmpty()
        {
            return Start == default && End == default;
        }
    }
}