using System.Linq.Expressions;
using Gebhardt.Acc.Common.Filter.Enums;
using Gebhardt.Acc.Common.Filter.Expressions;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace Gebhardt.Acc.CommonTest.Filter;

[TestClass]
public class FilterExpressionBuilderTests
{
    [TestMethod]
    [DataRow(3)]
    [DataRow(10)]
    [DataRow(12)]
    [DataRow(-4)]
    public void CreateCompareToExpression_ValueType_CreatesCompareToExpression(int compareTo)
    {
        const int testValue = 10;
        var valueHolder = new TestValueHolder<int>(testValue);

        Expression<Func<TestValueHolder<int>, int>> expression = FilterExpressionBuilder.CreateCompareToExpression<TestValueHolder<int>, int>(compareTo, nameof(TestValueHolder<>.Value));

        Func<TestValueHolder<int>, int> compiled = expression.Compile();

        compiled.Invoke(valueHolder).ShouldBe(compareTo.CompareTo(testValue));
    }

    [TestMethod]
    [DataRow("3")]
    [DataRow("10")]
    [DataRow("12")]
    [DataRow("-4")]
    public void CreateCompareToExpression_StringValue_CreatesCompareToExpression(string compareTo)
    {
        const string testValue = "10";
        var valueHolder = new TestValueHolder<string>(testValue);

        Expression<Func<TestValueHolder<string>, int>> expression = FilterExpressionBuilder.CreateCompareToExpression<TestValueHolder<string>, string>(compareTo, nameof(TestValueHolder<>.Value));

        Func<TestValueHolder<string>, int> compiled = expression.Compile();

        // ReSharper disable once StringCompareToIsCultureSpecific
        compiled.Invoke(valueHolder).ShouldBe(compareTo.CompareTo(testValue));
    }

    [TestMethod]
    [DataRow(3)]
    [DataRow(10)]
    [DataRow(12)]
    [DataRow(-4)]
    public void CreateCompareToExpression_ReferenceType_CreatesCompareToExpression(int compareTo)
    {
        var testValue = new RefType(10);
        var compareToValue = new RefType(compareTo);
        var valueHolder = new TestValueHolder<RefType>(testValue);

        Expression<Func<TestValueHolder<RefType>, int>> expression = FilterExpressionBuilder.CreateCompareToExpression<TestValueHolder<RefType>, RefType>(compareToValue, nameof(TestValueHolder<>.Value));

        Func<TestValueHolder<RefType>, int> compiled = expression.Compile();

        compiled.Invoke(valueHolder).ShouldBe(compareToValue.CompareTo(testValue));
    }

    [TestMethod]
    [DataRow(1, ComparisonVariant.Gt, 1)]
    [DataRow(10, ComparisonVariant.Leq, 5)]
    [DataRow(-1, ComparisonVariant.Equals, 1)]
    [DataRow(1, ComparisonVariant.Geq, 2)]
    [DataRow(-1, ComparisonVariant.Lt, 1)]
    [DataRow(0, ComparisonVariant.Neq, 4)]
    public async Task GeneratePredicate_ValidIntData_RetrievesCorrectItemCount(int targetValue, ComparisonVariant variant, int expectedCount)
    {
        var fixture = new FilterExpressionHelperTestFixture();

        fixture.Add(new TestDbEntity("1", 10));
        fixture.Add(new TestDbEntity("2", 0));
        fixture.Add(new TestDbEntity("3", 1));
        fixture.Add(new TestDbEntity("4", -1));
        fixture.Add(new TestDbEntity("5", -10));

        await using var context = await fixture.ContextFactory.CreateDbContextAsync();
        
        Expression<Func<TestDbEntity, bool>> predicate = FilterExpressionBuilder.GenerateNumericalPredicate<TestDbEntity, int>(targetValue, nameof(TestDbEntity.IntValue), variant);

        List<TestDbEntity> results = await context.Set<TestDbEntity>().Where(predicate).ToListAsync();

        results.Count.ShouldBe(expectedCount);
    }

    [TestMethod]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(3)]
    [DataRow(4)]
    [DataRow(5)]
    public async Task CreateIsOneOfPredicate_ValidIntData_RetrievesCorrectItemCount(int expectedValue)
    {
        var fixture = new FilterExpressionHelperTestFixture();

        fixture.Add(new TestDbEntity("1", 10));
        fixture.Add(new TestDbEntity("2", 0));
        fixture.Add(new TestDbEntity("3", 1));
        fixture.Add(new TestDbEntity("4", -1));
        fixture.Add(new TestDbEntity("5", -10));

        List<int> values = [1, 0, 10, -1, -10];

        await using var context = await fixture.ContextFactory.CreateDbContextAsync();

        Expression<Func<TestDbEntity, bool>> predicate = FilterExpressionBuilder.CreateIsOneOfPredicate<TestDbEntity, int>(values.Take(expectedValue).ToList(), nameof(TestDbEntity.IntValue));

        List<TestDbEntity> results = await context.Set<TestDbEntity>().Where(predicate).ToListAsync();

        results.Count.ShouldBe(expectedValue);
    }

    [TestMethod]
    [DataRow("4", ComparisonVariant.Gt, 1)]
    [DataRow("5", ComparisonVariant.Leq, 5)]
    [DataRow("3", ComparisonVariant.Equals, 1)]
    [DataRow("4", ComparisonVariant.Geq, 2)]
    [DataRow("2", ComparisonVariant.Lt, 1)]
    [DataRow("3", ComparisonVariant.Neq, 4)]
    public async Task GeneratePredicate_ValidStringData_RetrievesCorrectItemCount(string targetValue, ComparisonVariant variant, int expectedCount)
    {
        var fixture = new FilterExpressionHelperTestFixture();

        fixture.Add(new TestDbEntity("1", 10));
        fixture.Add(new TestDbEntity("2", 0));
        fixture.Add(new TestDbEntity("3", 1));
        fixture.Add(new TestDbEntity("4", -1));
        fixture.Add(new TestDbEntity("5", -10));

        await using var context = await fixture.ContextFactory.CreateDbContextAsync();

        Expression<Func<TestDbEntity, bool>> predicate = FilterExpressionBuilder.GenerateNumericalPredicate<TestDbEntity, string>(targetValue, nameof(TestDbEntity.StringValue), variant);

        List<TestDbEntity> results = await context.Set<TestDbEntity>().Where(predicate).ToListAsync();

        results.Count.ShouldBe(expectedCount);
    }

    [TestMethod]
    [DataRow(1, ComparisonVariant.Gt, 1)]
    [DataRow(10, ComparisonVariant.Leq, 5)]
    [DataRow(-1, ComparisonVariant.Equals, 1)]
    [DataRow(1, ComparisonVariant.Geq, 2)]
    [DataRow(-1, ComparisonVariant.Lt, 1)]
    [DataRow(0, ComparisonVariant.Neq, 4)]
    public async Task NegatePredicate_ValidIntData_RetrievesCorrectItemCount(int targetValue, ComparisonVariant variant, int invertedCount)
    {
        var fixture = new FilterExpressionHelperTestFixture();

        fixture.Add(new TestDbEntity("1", 10));
        fixture.Add(new TestDbEntity("2", 0));
        fixture.Add(new TestDbEntity("3", 1));
        fixture.Add(new TestDbEntity("4", -1));
        fixture.Add(new TestDbEntity("5", -10));

        int expectedCount = 5 - invertedCount;

        await using var context = await fixture.ContextFactory.CreateDbContextAsync();

        Expression<Func<TestDbEntity, bool>> predicate = FilterExpressionBuilder.GenerateNumericalPredicate<TestDbEntity, int>(targetValue, nameof(TestDbEntity.IntValue), variant);

        Expression<Func<TestDbEntity, bool>> inverted = FilterExpressionBuilder.NegatePredicate(predicate);

        List<TestDbEntity> results = await context.Set<TestDbEntity>().Where(inverted).ToListAsync();

        results.Count.ShouldBe(expectedCount);
    }

    [TestMethod]
    [DataRow(LogicalCombinationVariant.Or, 4)]
    [DataRow(LogicalCombinationVariant.And, 1)]
    public async Task CombinePredicates_ValidData_RetrievesCorrectItemCount(LogicalCombinationVariant op, int expectedCount)
    {
        var fixture = new FilterExpressionHelperTestFixture();

        fixture.Add(new TestDbEntity("1", 1));
        fixture.Add(new TestDbEntity("2", 0));
        fixture.Add(new TestDbEntity("3", 10));
        fixture.Add(new TestDbEntity("4", -1));
        fixture.Add(new TestDbEntity("5", -10));

        await using var context = await fixture.ContextFactory.CreateDbContextAsync();

        Expression<Func<TestDbEntity, bool>> predicate1 = FilterExpressionBuilder.GenerateNumericalPredicate<TestDbEntity, int>(1, nameof(TestDbEntity.IntValue), ComparisonVariant.Geq);
        Expression<Func<TestDbEntity, bool>> predicate2 = FilterExpressionBuilder.GenerateNumericalPredicate<TestDbEntity, string>("3", nameof(TestDbEntity.StringValue), ComparisonVariant.Geq);

        Expression<Func<TestDbEntity, bool>> predicate = FilterExpressionBuilder.ConnectPredicates(predicate1, predicate2, op);
        
        List<TestDbEntity> results = await context.Set<TestDbEntity>().Where(predicate).ToListAsync();

        results.Count.ShouldBe(expectedCount);
    }

    [TestMethod]
    [DataRow("A", 5)]
    [DataRow("AA", 4)]
    [DataRow("AAA", 3)]
    [DataRow("AAAA", 2)]
    [DataRow("AAAAA", 1)]
    [DataRow("AAAAAA", 0)]
    public async Task GenerateContainsPredicate_ValidStringData_RetrievesCorrectItemCount(string targetValue, int expectedCount)
    {
        var fixture = new FilterExpressionHelperTestFixture();

        fixture.Add(new TestDbEntity("A", 10));
        fixture.Add(new TestDbEntity("AA", 0));
        fixture.Add(new TestDbEntity("AAA", 1));
        fixture.Add(new TestDbEntity("AAAA", -1));
        fixture.Add(new TestDbEntity("AAAAA", -10));

        await using var context = await fixture.ContextFactory.CreateDbContextAsync();

        Expression<Func<TestDbEntity, bool>> predicate = FilterExpressionBuilder.GenerateContainsPredicate<TestDbEntity>(targetValue, nameof(TestDbEntity.StringValue));

        List<TestDbEntity> results = await context.Set<TestDbEntity>().Where(predicate).ToListAsync();

        results.Count.ShouldBe(expectedCount);
    }

    [TestMethod]
    public void CreateAggregationPredicate_ValidData_GeneratesPredicate()
    {
        Expression<Func<IEnumerable<bool>, bool>> predicate = FilterExpressionBuilder.CreateAggregationPredicate((bool a) => a, CollectionAggregateVariant.All);

        predicate.Compile().Invoke([true, true, false]).ShouldBeFalse();
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

        await using var context = await fixture.ContextFactory.CreateDbContextAsync();

        Expression<Func<TestDbEntity, bool>> predicate = FilterExpressionBuilder.GenerateContainsPredicate<TestDbEntity>("AAA", nameof(TestDbEntity.StringValue));

        Expression<Func<IEnumerable<TestDbEntity>, bool>> aggregatePredicate = FilterExpressionBuilder.CreateAggregationPredicate(predicate, variant);

        Expression<Func<TestCollectionEntity, List<TestDbEntity>>> accessor = FilterExpressionBuilder.CreatePropertyAccess<TestCollectionEntity, List<TestDbEntity>>(nameof(TestCollectionEntity.Values));

        Expression<Func<TestCollectionEntity, bool>> accessorAndPredicate = ExpressionHelper.Compose(accessor, aggregatePredicate);

        List<TestCollectionEntity> results = await context.Set<TestCollectionEntity>().Include(a => a.Values).Where(accessorAndPredicate).ToListAsync();

        results.Count.ShouldBe(expectedCount);
    }

    [TestMethod]
    public void CreateIdentityExpression_ValidData_CreatesIdentityExpression()
    {
        Expression<Func<string, string>> identity = ExpressionHelper.CreateIdentityExpression<string>();

        const string test = "Test";

        identity.Compile().Invoke(test).ShouldBe(test);
    }

    [TestMethod]
    public void MakeNullCoalescing_ValueAccess_CoalescesCorrectly()
    {
        TestValueHolder<TestValueHolder<int>>? nullableValue = null;

        Expression<Func<TestValueHolder<TestValueHolder<int>>?, TestValueHolder<int>?>> accessor = FilterExpressionBuilder.CreatePropertyAccess<TestValueHolder<TestValueHolder<int>>?, TestValueHolder<int>?>(nameof(TestValueHolder<>.Value));

        Should.Throw<NullReferenceException>(() => accessor.Compile()(nullableValue));

        Expression<Func<TestValueHolder<TestValueHolder<int>>?, TestValueHolder<int>?>> nullableAccessor = FilterExpressionBuilder.MakeNullCoalescing(accessor);

        nullableAccessor.Compile()(nullableValue).ShouldBeNull();
    }

    [TestMethod]
    public void CreateCoalescingStructAccessor_ValueAccessRefType_CoalescesCorrectly()
    {
        DateTime? nullableValue = null;
        
        Expression<Func<DateTime?, DayOfWeek?>> nullableAccessor = FilterExpressionBuilder.CreateCoalescingStructAccessor<DateTime, DayOfWeek>(nameof(DateTime.DayOfWeek));

        nullableAccessor.Compile()(nullableValue).ShouldBeNull();

        nullableValue = DateTime.UtcNow;
        
        nullableAccessor.Compile()(nullableValue).ShouldBe(nullableValue.Value.DayOfWeek);
    }
}

file class TestValueHolder<T>(T value)
{
    public T Value => value;
}

file class RefType(int content) : IComparable<RefType>
{
    private int Content => content;

    public int CompareTo(RefType? other)
    {
        return content.CompareTo(other?.Content);
    }
}