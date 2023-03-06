using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Orders.Requests.Queries.Filtering.DateRange;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.Controllers.DateRange
{
    /// <summary>
    /// Date Range Parameters Test
    /// </summary>
    [TestClass]
    public class DateRangeParametersTest
    {
        /// <summary>
        /// Date Range Parameters empty tests
        /// </summary>
        [TestMethod]
        public void DateRangeParametersEmpty()
        {
            new DateRangeParameters().IsEmpty().ShouldBeTrue();
            new DateRangeParameters(default, default).IsEmpty().ShouldBeTrue();
            new DateRangeParameters(default, DateTime.Now).IsEmpty().ShouldBeFalse();
            new DateRangeParameters(DateTime.Now, default).IsEmpty().ShouldBeFalse();
            new DateRangeParameters(DateTime.Now, DateTime.Now).IsEmpty().ShouldBeFalse();
        }
    }
}