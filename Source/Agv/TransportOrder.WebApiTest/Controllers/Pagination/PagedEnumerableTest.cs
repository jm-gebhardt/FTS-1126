using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Controllers.Pagination;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.Controllers.Pagination
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
            enumerable.CurrentPage.ShouldBe(pageNumber);
            enumerable.PageSize.ShouldBe(pageSize);
            enumerable.Count().ShouldBe(0);
            enumerable.HasNext.ShouldBeFalse();
            enumerable.HasPrevious.ShouldBeFalse();
        }

        [TestMethod]
        public void Input_Single_Output_Single()
        {
            var list = new List<int>(new[] { 1 });
            const int pageNumber = 1;
            const int pageSize = 1;
            var enumerable = new PagedEnumerable<int>(list.AsQueryable(), pageNumber, pageSize);
            enumerable.CurrentPage.ShouldBe(pageNumber);
            enumerable.PageSize.ShouldBe(pageSize);
            enumerable.Count().ShouldBe(1);
            enumerable.HasNext.ShouldBeFalse();
            enumerable.HasPrevious.ShouldBeFalse();
        }

        [TestMethod]
        public void ListInput_TwoPages_FirstPage()
        {
            var list = new List<int>(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 });
            const int pageNumber = 1;
            const int pageSize = 5;
            var enumerable = new PagedEnumerable<int>(list.AsQueryable(), pageNumber, pageSize);
            enumerable.CurrentPage.ShouldBe(pageNumber);
            enumerable.PageSize.ShouldBe(pageSize);
            enumerable.Count().ShouldBe(5);
            enumerable.HasNext.ShouldBeTrue();
            enumerable.HasPrevious.ShouldBeFalse();
            list.Take(pageSize).SequenceEqual(enumerable).ShouldBeTrue();
        }

        [TestMethod]
        public void ListInput_TwoPages_SecondPage()
        {
            var list = new List<int>(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 });
            const int pageNumber = 2;
            const int pageSize = 5;
            var enumerable = new PagedEnumerable<int>(list.AsQueryable(), pageNumber, pageSize);
            enumerable.CurrentPage.ShouldBe(pageNumber);
            enumerable.PageSize.ShouldBe(pageSize);
            enumerable.Count().ShouldBe(4);
            enumerable.HasNext.ShouldBeFalse();
            enumerable.HasPrevious.ShouldBeTrue();
            list.Skip(pageSize).SequenceEqual(enumerable).ShouldBeTrue();
        }

        [TestMethod]
        public void PaginationHeader_Valid()
        {
            var list = new List<int>(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 });
            const int pageNumber = 2;
            const int pageSize = 5;
            var enumerable = new PagedEnumerable<int>(list.AsQueryable(), pageNumber, pageSize);
            var header = enumerable.PaginationHeader();
            header.PageSize.ShouldBe(pageSize);
            header.CurrentPage.ShouldBe(pageNumber);
        }
    }
}
