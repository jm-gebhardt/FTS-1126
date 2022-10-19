using Gebhardt.StoreWare.Agv.TransportOrder.Web.Controllers.Pagination;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Gebhardt.StoreWare.Agv.TransportOrderTest.Web.Controllers.Pagination
{
    [TestClass]
    public class PagedEnumerableTest
    {
        [TestMethod]
        public void Input_Empty_Output_Empty()
        {
            const int pageNumber = 1;
            const int pageSize = 1;
            var enumerable = new PagedEnumerable<int>(new List<int>().AsQueryable(), pageNumber, pageSize);
            Assert.AreEqual(pageNumber, enumerable.CurrentPage);
            Assert.AreEqual(pageSize, enumerable.PageSize);
            Assert.AreEqual(0, enumerable.Count());
            Assert.IsFalse(enumerable.HasNext);
            Assert.IsFalse(enumerable.HasPrevious);
        }

        [TestMethod]
        public void Input_Single_Output_Single()
        {
            var list = new List<int>(new[] { 1 });
            const int pageNumber = 1;
            const int pageSize = 1;
            var enumerable = new PagedEnumerable<int>(list.AsQueryable(), pageNumber, pageSize);
            Assert.AreEqual(pageNumber, enumerable.CurrentPage);
            Assert.AreEqual(pageSize, enumerable.PageSize);
            Assert.AreEqual(1, enumerable.Count());
            Assert.IsFalse(enumerable.HasNext);
            Assert.IsFalse(enumerable.HasPrevious);
        }

        [TestMethod]
        public void ListInput_TwoPages_FirstPage()
        {
            var list = new List<int>(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 });
            const int pageNumber = 1;
            const int pageSize = 5;
            var enumerable = new PagedEnumerable<int>(list.AsQueryable(), pageNumber, pageSize);
            Assert.AreEqual(pageNumber, enumerable.CurrentPage);
            Assert.AreEqual(pageSize, enumerable.PageSize);
            Assert.AreEqual(5, enumerable.Count());
            Assert.IsTrue(enumerable.HasNext);
            Assert.IsFalse(enumerable.HasPrevious);
            Assert.IsTrue(list.Take(pageSize).SequenceEqual(enumerable));
        }

        [TestMethod]
        public void ListInput_TwoPages_SecondPage()
        {
            var list = new List<int>(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 });
            const int pageNumber = 2;
            const int pageSize = 5;
            var enumerable = new PagedEnumerable<int>(list.AsQueryable(), pageNumber, pageSize);
            Assert.AreEqual(pageNumber, enumerable.CurrentPage);
            Assert.AreEqual(pageSize, enumerable.PageSize);
            Assert.AreEqual(4, enumerable.Count());
            Assert.IsFalse(enumerable.HasNext);
            Assert.IsTrue(enumerable.HasPrevious);
            Assert.IsTrue(list.Skip(pageSize).SequenceEqual(enumerable));
        }

        [TestMethod]
        public void PaginationHeader_Valid()
        {
            var list = new List<int>(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 });
            const int pageNumber = 2;
            const int pageSize = 5;
            var enumerable = new PagedEnumerable<int>(list.AsQueryable(), pageNumber, pageSize);
            var header = enumerable.PaginationHeader();
            Assert.AreEqual(pageSize, header.PageSize);
            Assert.AreEqual(pageNumber, header.CurrentPage);
        }
    }
}
