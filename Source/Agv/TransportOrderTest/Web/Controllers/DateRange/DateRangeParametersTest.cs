using Gebhardt.StoreWare.Agv.TransportOrder.Web.Controllers.DateRange;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Gebhardt.StoreWare.Agv.TransportOrderTest.Web.Controllers.DateRange
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
            Assert.IsTrue(new DateRangeParameters().IsEmpty());
            Assert.IsTrue(new DateRangeParameters(default, default).IsEmpty());
            Assert.IsFalse(new DateRangeParameters(default, DateTime.Now).IsEmpty());
            Assert.IsFalse(new DateRangeParameters(DateTime.Now, default).IsEmpty());
            Assert.IsFalse(new DateRangeParameters(DateTime.Now, DateTime.Now).IsEmpty());
        }
    }
}