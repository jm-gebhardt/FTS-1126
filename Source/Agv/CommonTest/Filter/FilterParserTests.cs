using Gebhardt.Acc.Common.Filter.Enums;
using Gebhardt.Acc.Common.Filter.FilterableParser;
using Gebhardt.Acc.Common.Filter.FilterBuilder;
using Shouldly;
using SortDirection = Gebhardt.Acc.Common.Filter.Enums.SortDirection;

namespace Gebhardt.Acc.CommonTest.Filter;

[TestClass]
public class FilterParserBaseTests
{
    [TestMethod]
    public void VerifyParsed_ValidData_ParsesCorrectly()
    {
        FilterableBinder<FilterableTestHelper, FilterableTestHelperReadModel>.VerifyParsing(1, 1, 1, 1).ShouldBeTrue();
    }

    [TestMethod]
    [DataRow(CollectionAggregateVariant.Any, 2)]
    [DataRow(CollectionAggregateVariant.All, 1)]
    public async Task CreateAggregationPredicate_ValidData_RetrievesCorrectItemCount(CollectionAggregateVariant variant, int expectedCount)
    {
        var fixture = new FilterExpressionHelperTestFixture();

        var coll1 = new TestCollectionEntity(
        [
            new TestDbEntity("A", 10),
            new TestDbEntity("AA", 0),
            new TestDbEntity("AAA", 1),
            new TestDbEntity("AAAA", -1),
            new TestDbEntity("AAAAA", -10)
        ]);

        var coll2 = new TestCollectionEntity(
        [
            new TestDbEntity("AAA", 10),
            new TestDbEntity("AAA", 0),
            new TestDbEntity("AAA", 1),
            new TestDbEntity("AAAA", -1),
            new TestDbEntity("AAAAA", -10)
        ]);

        fixture.Add(coll1);
        fixture.Add(coll2);

        var filterBy = FilterableBinder<TestCollectionEntity, TestCollectionEntityReadModel>.CreateFilter(fixture.ContextFactory);

        var filter = new CollectionFilter(variant, nameof(TestCollectionEntity.Values), new ValueComparisonFilter<string>(nameof(TestCollectionEntity.Values) + IFilter.AccessLevelSeparator + nameof(TestDbEntity.StringValue),ComparisonVariant.Like, "AAA"));

        ICollection<TestCollectionEntityReadModel> result = await filterBy(filter);

        result.Count.ShouldBe(expectedCount);
    }

    [TestMethod]
    public async Task InvokeFilterBy_WithPagination_RetrievesCorrectItemCount()
    {
        var fixture = new FilterExpressionHelperTestFixture();
        for (int i = 0; i < 100; i++)
        {
            var coll = new TestCollectionEntity(
            [
                new TestDbEntity("A", i),
                new TestDbEntity("AA", i),
                new TestDbEntity("AAA", i)
            ]);

            fixture.Add(coll);
        }
        
        var filterBy = FilterableBinder<TestCollectionEntity, TestCollectionEntityReadModel>.CreateFilterWithPagination(fixture.ContextFactory);

        var filter = new CollectionFilter(CollectionAggregateVariant.All, nameof(TestCollectionEntityReadModel.Values), new ValueComparisonFilter<int>(nameof(TestCollectionEntityReadModel.Values) + IFilter.AccessLevelSeparator + nameof(TestDbEntityReadModel.IntValue), ComparisonVariant.Gt, -1));

        ICollection<TestCollectionEntityReadModel> result = await filterBy(filter, 4, 10, SortDirection.Desc, []);

        result.Count.ShouldBe(10);
    }

    [TestMethod]
    public async Task InvokeFilterBy_WithPagination_NoOverlappingElements()
    {
        var fixture = new FilterExpressionHelperTestFixture();
        for (int i = 0; i < 100; i++)
        {
            var coll = new TestCollectionEntity(
            [
                new TestDbEntity("A", i)
            ]);

            fixture.Add(coll);
        }

        var filterBy = FilterableBinder<TestCollectionEntity, TestCollectionEntityReadModel>.CreateFilterWithPagination(fixture.ContextFactory);

        var filter = new CollectionFilter(CollectionAggregateVariant.All, nameof(TestCollectionEntityReadModel.Values), new ValueComparisonFilter<int>(nameof(TestCollectionEntityReadModel.Values) + IFilter.AccessLevelSeparator + nameof(TestDbEntityReadModel.IntValue), ComparisonVariant.Gt, -1));

        List<Task<ICollection<TestCollectionEntityReadModel>>> results = [];

        for (int i = 0; i < 10; i++)
        {
            results.Add(filterBy.Invoke(filter, i, 10, SortDirection.Desc, []));
        }

        ICollection<TestCollectionEntityReadModel>[] awaitedResults = await Task.WhenAll(results);

        List<HashSet<int>> intValues = awaitedResults.Select(t => t.SelectMany(e => e.Values.Select(ee => ee.IntValue)).ToHashSet()).ToList();

        foreach (HashSet<int> page in intValues)
        {
            page.Count.ShouldBe(10);
            foreach (HashSet<int> otherPage in intValues.Where(p => p!=page))
            {
                page.Intersect(otherPage).ShouldBeEmpty();
            }
        }
    }

    [TestMethod]
    public async Task InvokeFilterBy_WithFilteringKeyProperty_CorrectFilterResult()
    {
        var fixture = new FilterExpressionHelperTestFixture();

        var entity = new TestDbEntity("1", 1);
        fixture.Add(entity);

        var filterBy = FilterableBinder<TestDbEntity, TestDbEntityReadModel>.CreateFilterWithPagination(fixture.ContextFactory);

        var filter = new CombinationFilter(LogicalCombinationVariant.And, [new ValueComparisonFilter<Guid>(nameof(TestDbEntityReadModel.Key), ComparisonVariant.Equals, entity.Key)]);

        ICollection<TestDbEntityReadModel> result = await filterBy.Invoke(filter, 0, 10, SortDirection.Desc, []);

        result.Count.ShouldBe(1);
    }

    [TestMethod]
    public async Task InvokeFilterBy_WithJoin_RetrievesCorrectItemCount()
    {
        var fixture = new FilterExpressionHelperTestFixture();

        var target = new TestJoinTargetEntity(10);
        var source = new TestJoinSourceEntity(target.TjTKey);
        var outer = new TestJoinSourceEntity();
        fixture.Add(target);
        fixture.Add(source);
        fixture.Add(outer);

        var filterBy = FilterableBinder<TestJoinSourceEntity, TestJoinSourceReadModel>.CreateFilterWithPagination(fixture.ContextFactory);

        var filter = new CombinationFilter(LogicalCombinationVariant.And, [new ValueComparisonFilter<int>(nameof(TestJoinSourceReadModel.TjtFKey) + IFilter.AccessLevelSeparator + nameof(TestJoinTargetReadModel.Value), ComparisonVariant.Equals, 10)]);

        ICollection<TestJoinSourceReadModel> result = await filterBy(filter, 0, 10, SortDirection.Desc, []);

        result.Count.ShouldBe(1);
    }

    [TestMethod]
    public async Task InvokeFilterBy_WithOuterJoin_RetrievesCorrectItemCount()
    {
        var fixture = new FilterExpressionHelperTestFixture();

        var target = new TestJoinTargetEntity(10);
        var source = new TestJoinSourceEntity(target.TjTKey);
        var outer = new TestJoinSourceEntity();
        fixture.Add(target);
        fixture.Add(source);
        fixture.Add(outer);

        var filterBy = FilterableBinder<TestJoinSourceEntity, TestJoinSourceReadModel>.CreateFilterWithPagination(fixture.ContextFactory);

        var filter = new CombinationFilter(LogicalCombinationVariant.And, [new ValueComparisonFilter<bool>(nameof(TestJoinSourceReadModel.Exists), ComparisonVariant.Equals, true)]);

        ICollection<TestJoinSourceReadModel> result = await filterBy(filter, 0, 10, SortDirection.Desc, []);

        result.Count.ShouldBe(2);
    }

    [TestMethod]
    public async Task InvokeFilterBy_NullableStruct_RetrievesCorrectItemCount()
    {
        var fixture = new FilterExpressionHelperTestFixture();

        var empty = new TestDbEntity("10",10);
        var full = new TestDbEntity("10",10) {NullablePrimitive = 10};
        fixture.Add(empty);
        fixture.Add(full);

        var filterBy = FilterableBinder<TestDbEntity, TestDbEntityReadModel>.CreateFilter(fixture.ContextFactory);

        var filter = new CombinationFilter(LogicalCombinationVariant.And, [new ValueComparisonFilter<int?>(nameof(TestDbEntity.NullablePrimitive) , ComparisonVariant.Equals, 10)]);

        ICollection<TestDbEntityReadModel> result = await filterBy(filter);

        result.Count.ShouldBe(1);
    }
    
    [TestMethod]
    public async Task InvokeFilterBy_ValueConversion_RetrievesCorrectItemCount()
    {
        var fixture = new FilterExpressionHelperTestFixture();

        var empty = new TestDbEntity("10", 4);
        var full = new TestDbEntity("10", 10) { Converted = new Dictionary<string, string>{{"a", "b"}}};
        fixture.Add(empty);
        fixture.Add(full);

        var filterBy = FilterableBinder<TestDbEntity, TestDbEntityReadModel>.CreateFilter(fixture.ContextFactory);

        var filter = new CombinationFilter(LogicalCombinationVariant.And, [new ValueComparisonFilter<int>(nameof(TestDbEntity.IntValue), ComparisonVariant.Equals, 10)]);

        ICollection<TestDbEntityReadModel> result = await filterBy(filter);

        result.Count.ShouldBe(1);
        result.First().Converted.ShouldContainKeyAndValue("a", "b");
    }
}