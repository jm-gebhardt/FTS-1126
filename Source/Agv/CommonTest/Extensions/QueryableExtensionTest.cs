using Gebhardt.Acc.Common.Exceptions;
using Gebhardt.Acc.Common.Extensions;
using JetBrains.Annotations;
using Shouldly;

namespace Gebhardt.Acc.CommonTest.Extensions;

[TestClass]
public class QueryableExtensionTest
{
    private record TestType(DateTime ADate, [UsedImplicitly] string AString);

    [TestMethod]
    public void OrderByProperty_DefaultDirection_ReturnsQueryableOrderedDescending()
    {
        var firstEntry = new TestType(DateTime.UtcNow, "a");
        var secondEntry = new TestType(DateTime.UtcNow.AddDays(2), "b");
        IQueryable<TestType> testQueryAble = new List<TestType> { firstEntry, secondEntry }.AsQueryable();

        IOrderedQueryable<TestType> ordered = testQueryAble.OrderByProperty("ADate");
        ordered.First().ShouldBe(secondEntry);
        ordered.Last().ShouldBe(firstEntry);
    }

    [TestMethod]
    public void OrderByProperty_Ascending_ReturnsQueryableOrderedAscending()
    {
        var firstEntry = new TestType(DateTime.UtcNow, "a");
        var secondEntry = new TestType(DateTime.UtcNow.AddDays(2), "b");
        IQueryable<TestType> testQueryAble = new List<TestType> { firstEntry, secondEntry }.AsQueryable();

        IOrderedQueryable<TestType> ordered = testQueryAble.OrderByProperty("ADate", isDescending: false);
        ordered.First().ShouldBe(firstEntry);
        ordered.Last().ShouldBe(secondEntry);
    }

    [TestMethod]
    public void OrderByProperty_UnknownProperty_ThrowsPropertyNotFoundException()
    {
        var firstEntry = new TestType(DateTime.UtcNow, "a");
        var secondEntry = new TestType(DateTime.UtcNow.AddDays(2), "b");
        IQueryable<TestType> testQueryAble = new List<TestType> { firstEntry, secondEntry }.AsQueryable();

        Should.Throw<PropertyNotFoundException>(() => testQueryAble.OrderByProperty("unknown"));
    }
}