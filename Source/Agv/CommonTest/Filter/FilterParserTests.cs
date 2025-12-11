using Gebhardt.Acc.Common.Filter.Api.Dtos;
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

        ICollection<TestCollectionEntityReadModel> result = (await filterBy(filter, 4, 10, SortDirection.Desc, [])).Items;

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

        List<Task<PagedResult<TestCollectionEntityReadModel>>> results = [];

        for (int i = 0; i < 10; i++)
        {
            results.Add(filterBy.Invoke(filter, i, 10, SortDirection.Desc, []));
        }

        List<ICollection<TestCollectionEntityReadModel>> awaitedResults = (await Task.WhenAll(results)).Select(a => a.Items).ToList();

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

        ICollection<TestDbEntityReadModel> result = (await filterBy.Invoke(filter, 0, 10, SortDirection.Desc, [])).Items;

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

        ICollection<TestJoinSourceReadModel> result = (await filterBy(filter, 0, 10, SortDirection.Desc, [])).Items;

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

        ICollection<TestJoinSourceReadModel> result = (await filterBy(filter, 0, 10, SortDirection.Desc, [])).Items;

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

    [TestMethod]
    public async Task InvokeFilterBy_WithOneToManyJoin_RetrievesParentWithChildren()
    {
        var fixture = new FilterExpressionHelperTestFixture();

        var parent1 = new TestParentEntity("Parent1");
        var parent2 = new TestParentEntity("Parent2");
        var child1 = new TestChildEntity(parent1.ParentKey, 10);
        var child2 = new TestChildEntity(parent1.ParentKey, 20);
        var child3 = new TestChildEntity(parent2.ParentKey, 30);

        fixture.Add(parent1);
        fixture.Add(parent2);
        fixture.Add(child1);
        fixture.Add(child2);
        fixture.Add(child3);

        var filterBy = FilterableBinder<TestParentEntity, TestParentReadModel>.CreateFilterWithPagination(fixture.ContextFactory);

        var filter = new CombinationFilter(LogicalCombinationVariant.And, [new ValueComparisonFilter<string>(nameof(TestParentReadModel.Name), ComparisonVariant.Equals, "Parent1")]);

        ICollection<TestParentReadModel> result = (await filterBy(filter, 0, 10, SortDirection.Desc, [])).Items;

        result.Count.ShouldBe(1);
        var parent = result.First();
        parent.Name.ShouldBe("Parent1");
        parent.Children.Count.ShouldBe(2);
        parent.Children.Select(c => c.Score).ShouldBe([10, 20], ignoreOrder: true);
    }

    [TestMethod]
    public async Task InvokeFilterBy_WithOneToManyJoin_RetrievesAllParentsWithTheirChildren()
    {
        var fixture = new FilterExpressionHelperTestFixture();

        var parent1 = new TestParentEntity("Parent1");
        var parent2 = new TestParentEntity("Parent2");
        var child1 = new TestChildEntity(parent1.ParentKey, 10);
        var child2 = new TestChildEntity(parent1.ParentKey, 20);
        var child3 = new TestChildEntity(parent2.ParentKey, 30);

        fixture.Add(parent1);
        fixture.Add(parent2);
        fixture.Add(child1);
        fixture.Add(child2);
        fixture.Add(child3);

        var filterBy = FilterableBinder<TestParentEntity, TestParentReadModel>.CreateFilter(fixture.ContextFactory);

        var filter = new CombinationFilter(LogicalCombinationVariant.And, []);

        ICollection<TestParentReadModel> result = await filterBy(filter);

        result.Count.ShouldBe(2);
        var p1 = result.First(p => p.Name == "Parent1");
        var p2 = result.First(p => p.Name == "Parent2");
        p1.Children.Count.ShouldBe(2);
        p2.Children.Count.ShouldBe(1);
        p2.Children.First().Score.ShouldBe(30);
    }

    [TestMethod]
    public async Task InvokeFilterBy_WithOneToManyJoin_ParentWithNoChildren()
    {
        var fixture = new FilterExpressionHelperTestFixture();

        var parent1 = new TestParentEntity("Parent1");
        var parent2 = new TestParentEntity("Parent2");
        var child1 = new TestChildEntity(parent1.ParentKey, 10);

        fixture.Add(parent1);
        fixture.Add(parent2);
        fixture.Add(child1);

        var filterBy = FilterableBinder<TestParentEntity, TestParentReadModel>.CreateFilter(fixture.ContextFactory);

        var filter = new CombinationFilter(LogicalCombinationVariant.And, []);

        ICollection<TestParentReadModel> result = await filterBy(filter);

        result.Count.ShouldBe(2);
        var p1 = result.First(p => p.Name == "Parent1");
        var p2 = result.First(p => p.Name == "Parent2");
        p1.Children.Count.ShouldBe(1);
        p2.Children.Count.ShouldBe(0);
    }

    [TestMethod]
    public async Task InvokeFilterBy_WithOneToManyJoin_FilterByChildProperty()
    {
        var fixture = new FilterExpressionHelperTestFixture();

        var parent1 = new TestParentEntity("Parent1");
        var parent2 = new TestParentEntity("Parent2");
        var parent3 = new TestParentEntity("Parent3");
        var child1 = new TestChildEntity(parent1.ParentKey, 10);
        var child2 = new TestChildEntity(parent1.ParentKey, 20);
        var child3 = new TestChildEntity(parent2.ParentKey, 30);
        var child4 = new TestChildEntity(parent3.ParentKey, 40);

        fixture.Add(parent1);
        fixture.Add(parent2);
        fixture.Add(parent3);
        fixture.Add(child1);
        fixture.Add(child2);
        fixture.Add(child3);
        fixture.Add(child4);

        var filterBy = FilterableBinder<TestParentEntity, TestParentReadModel>.CreateFilter(fixture.ContextFactory);

        // Filter for parents that have at least one child with Score == 20
        var filter = new CollectionFilter(CollectionAggregateVariant.Any, "Children", 
            new ValueComparisonFilter<int>("Children.Score", ComparisonVariant.Equals, 20)
        );

        ICollection<TestParentReadModel> result = await filterBy(filter);

        // Only Parent1 should match because it has a child with Score 20
        result.Count.ShouldBe(1);
        result.First().Name.ShouldBe("Parent1");
        // Parent1 should still have both its children
        result.First().Children.Count.ShouldBe(2);
    }

    [TestMethod]
    public async Task InvokeFilterBy_WithOneToManyJoin_FilterByChildPropertyWithOr()
    {
        var fixture = new FilterExpressionHelperTestFixture();

        var parent1 = new TestParentEntity("Parent1");
        var parent2 = new TestParentEntity("Parent2");
        var parent3 = new TestParentEntity("Parent3");
        var child1 = new TestChildEntity(parent1.ParentKey, 10);
        var child2 = new TestChildEntity(parent2.ParentKey, 30);
        var child3 = new TestChildEntity(parent3.ParentKey, 40);

        fixture.Add(parent1);
        fixture.Add(parent2);
        fixture.Add(parent3);
        fixture.Add(child1);
        fixture.Add(child2);
        fixture.Add(child3);

        var filterBy = FilterableBinder<TestParentEntity, TestParentReadModel>.CreateFilter(fixture.ContextFactory);

        // Filter for parents that have at least one child with Score == 10 or Score == 30
        var filter = new CollectionFilter(CollectionAggregateVariant.Any, "Children", new CombinationFilter(LogicalCombinationVariant.Or, [
            new ValueComparisonFilter<int>("Children.Score", ComparisonVariant.Equals, 10),
            new ValueComparisonFilter<int>("Children.Score", ComparisonVariant.Equals, 30)
        ]));

        ICollection<TestParentReadModel> result = await filterBy(filter);

        // Parent1 (Score 10) and Parent2 (Score 30) should match
        result.Count.ShouldBe(2);
        result.Select(p => p.Name).ShouldBe(["Parent1", "Parent2"], ignoreOrder: true);
    }

    [TestMethod]
    public async Task InvokeFilterByWithProjection_WithOneToManyJoin_FiltersChildrenOnly()
    {
        var fixture = new FilterExpressionHelperTestFixture();

        var parent1 = new TestParentEntity("Parent1");
        var parent2 = new TestParentEntity("Parent2");
        var child1 = new TestChildEntity(parent1.ParentKey, 10);
        var child2 = new TestChildEntity(parent1.ParentKey, 20);
        var child3 = new TestChildEntity(parent2.ParentKey, 30);

        fixture.Add(parent1);
        fixture.Add(parent2);
        fixture.Add(child1);
        fixture.Add(child2);
        fixture.Add(child3);

        var filterBy = FilterableBinder<TestParentEntity, TestParentReadModel>.CreateFilter(fixture.ContextFactory);

        // Empty parent filter (all parents should be returned)
        var filter = new CollectionFilter(CollectionAggregateVariant.Where, nameof(TestParentReadModel.Children), new ValueComparisonFilter<int>(nameof(TestParentReadModel.Children)+".Score", ComparisonVariant.Equals, 20));

        ICollection<TestParentReadModel> result = await filterBy(filter);

        // Both parents should be returned
        result.Count.ShouldBe(2);

        var p1 = result.First(p => p.Name == "Parent1");
        var p2 = result.First(p => p.Name == "Parent2");

        // Parent1 should only have the child with Score 20
        p1.Children.Count.ShouldBe(1);
        p1.Children.First().Score.ShouldBe(20);

        // Parent2 should have no children (none match the projection filter)
        p2.Children.Count.ShouldBe(0);
    }

    [TestMethod]
    public async Task InvokeFilterByWithProjection_WithOneToManyJoin_CombinesParentAndProjectionFilters()
    {
        var fixture = new FilterExpressionHelperTestFixture();

        var parent1 = new TestParentEntity("Parent1");
        var parent2 = new TestParentEntity("Parent2");
        var parent3 = new TestParentEntity("Parent3");
        var child1 = new TestChildEntity(parent1.ParentKey, 10);
        var child2 = new TestChildEntity(parent1.ParentKey, 20);
        var child3 = new TestChildEntity(parent2.ParentKey, 15);
        var child4 = new TestChildEntity(parent2.ParentKey, 25);
        var child5 = new TestChildEntity(parent3.ParentKey, 100);

        fixture.Add(parent1);
        fixture.Add(parent2);
        fixture.Add(parent3);
        fixture.Add(child1);
        fixture.Add(child2);
        fixture.Add(child3);
        fixture.Add(child4);
        fixture.Add(child5);

        var filterBy = FilterableBinder<TestParentEntity, TestParentReadModel>.CreateFilter(fixture.ContextFactory);

        // Parent filter: only parents named Parent1 or Parent2
        var filter = new CombinationFilter(LogicalCombinationVariant.Or, [
            new ValueComparisonFilter<string>("Name", ComparisonVariant.Equals, "Parent1"),
            new ValueComparisonFilter<string>("Name", ComparisonVariant.Equals, "Parent2"),
            new CollectionFilter(CollectionAggregateVariant.Where, nameof(TestParentReadModel.Children), new ValueComparisonFilter<int>(nameof(TestParentReadModel.Children)+ ".Score", ComparisonVariant.Geq, 15))
        ]);

        ICollection<TestParentReadModel> result = await filterBy(filter);

        // Only Parent1 and Parent2 should be returned (Parent3 is filtered out by parent filter)
        result.Count.ShouldBe(2);

        var p1 = result.First(p => p.Name == "Parent1");
        var p2 = result.First(p => p.Name == "Parent2");

        // Parent1 should only have child with Score 20 (Score 10 filtered out by projection filter)
        p1.Children.Count.ShouldBe(1);
        p1.Children.First().Score.ShouldBe(20);

        // Parent2 should have both children (both >= 15)
        p2.Children.Count.ShouldBe(2);
        p2.Children.Select(c => c.Score).ShouldBe([15, 25], ignoreOrder: true);
    }

    [TestMethod]
    public async Task InvokeFilterByWithProjection_WithOneToManyJoin_MultipleConditionsInProjectionFilter()
    {
        var fixture = new FilterExpressionHelperTestFixture();

        var parent1 = new TestParentEntity("Parent1");
        var child1 = new TestChildEntity(parent1.ParentKey, 10);
        var child2 = new TestChildEntity(parent1.ParentKey, 20);
        var child3 = new TestChildEntity(parent1.ParentKey, 30);
        var child4 = new TestChildEntity(parent1.ParentKey, 15);

        fixture.Add(parent1);
        fixture.Add(child1);
        fixture.Add(child2);
        fixture.Add(child3);
        fixture.Add(child4);

        var filterBy = FilterableBinder<TestParentEntity, TestParentReadModel>.CreateFilter(fixture.ContextFactory);

        var filter = new CollectionFilter(CollectionAggregateVariant.Where, nameof(TestParentReadModel.Children), new CombinationFilter(LogicalCombinationVariant.Or, [
            new ValueComparisonFilter<int>(nameof(TestParentReadModel.Children)+".Score", ComparisonVariant.Equals, 10),
            new ValueComparisonFilter<int>(nameof(TestParentReadModel.Children)+".Score", ComparisonVariant.Equals, 30)
        ]));

        ICollection<TestParentReadModel> result = await filterBy(filter);

        result.Count.ShouldBe(1);
        var p1 = result.First();

        // Should only include children with scores 10 and 30
        p1.Children.Count.ShouldBe(2);
        p1.Children.Select(c => c.Score).ShouldBe([10, 30], ignoreOrder: true);
    }
}
