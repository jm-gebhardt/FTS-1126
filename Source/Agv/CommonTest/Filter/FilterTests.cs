using System.Linq.Expressions;
using Gebhardt.Acc.Common.Filter.Enums;
using Gebhardt.Acc.Common.Filter.Expressions;
using Gebhardt.Acc.Common.Filter.FilterBuilder;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.Acc.CommonTest.Filter;

[TestClass]
public class FilterTests
{
    [TestMethod]
    public void SplitFilter_UniformFilter_FilterSplit()
    {
        var filter = new CombinationFilter(
            LogicalCombinationVariant.And,
            [
                new CombinationFilter(LogicalCombinationVariant.And, [],
                    [
                        new FilterLeaf<string>("A", ComparisonVariant.Equals, "a"),
                        new FilterLeaf<string>("A", ComparisonVariant.Equals, "a")
                    ]
                ),
                new CombinationFilter(LogicalCombinationVariant.Or, [],
                    [
                        new FilterLeaf<string>("A", ComparisonVariant.Equals, "a"),
                        new FilterLeaf<string>("A", ComparisonVariant.Equals, "a")
                    ]
                ),
                new CombinationFilter(LogicalCombinationVariant.And, [],
                    [
                        new FilterLeaf<string>("A", ComparisonVariant.Equals, "a"),
                        new FilterLeaf<string>("A.B", ComparisonVariant.Equals, "a")
                    ]
                ),
                new CombinationFilter(LogicalCombinationVariant.Or, [],
                    [
                        new FilterLeaf<string>("A", ComparisonVariant.Equals, "a"),
                        new FilterLeaf<string>("A.B", ComparisonVariant.Equals, "a")
                    ]
                )
            ],
            []
        );
        
        Dictionary<int, FilterBase> result = filter.SplitFilters(["A", "A.B"]);

        result.Keys.Count.ShouldBe(2);
        
        result.TryGetValue(0, out var filter0).ShouldBeTrue();
        result.TryGetValue(1, out var filter1).ShouldBeTrue();

        (filter0 as CombinationFilter)!.CombinationVariantType.ShouldBe(LogicalCombinationVariant.And);
        (filter1 as CombinationFilter)!.CombinationVariantType.ShouldBe(LogicalCombinationVariant.And);

        filter0.Leaves.Count.ShouldBe(0);
        filter0.ChildNodes.ShouldAllBe(n => n.Leaves.All(l => l.Name == "A"));
        filter0.ChildNodes.ShouldContain(n => (n as CombinationFilter)!.CombinationVariantType == LogicalCombinationVariant.Or);
        filter0.ChildNodes.ShouldContain(n => (n as CombinationFilter)!.CombinationVariantType == LogicalCombinationVariant.And);

        filter1.Leaves.Count.ShouldBe(0);
        filter1.ChildNodes.ShouldAllBe(n => n.Leaves.Any(l => l.Name == "A.B"));
        filter1.ChildNodes.ShouldContain(n => (n as CombinationFilter)!.CombinationVariantType == LogicalCombinationVariant.Or);
        filter1.ChildNodes.ShouldContain(n => (n as CombinationFilter)!.CombinationVariantType == LogicalCombinationVariant.And);
    }

    [TestMethod]
    public void SplitFilter_WithMixedLeavesNodes_FilterSplit()
    {
        var filter = new CombinationFilter(
            LogicalCombinationVariant.And,
            [
                new CombinationFilter(LogicalCombinationVariant.And, [],
                    [
                        new FilterLeaf<string>("A", ComparisonVariant.Equals, "a"),
                        new FilterLeaf<string>("A", ComparisonVariant.Equals, "a")
                    ]
                ),
                new CombinationFilter(LogicalCombinationVariant.And, [],
                    [
                        new FilterLeaf<string>("A", ComparisonVariant.Equals, "a"),
                        new FilterLeaf<string>("A.B", ComparisonVariant.Equals, "a")
                    ]
                )
            ],
            [
                new FilterLeaf<string>("A", ComparisonVariant.Equals, "a"),
                new FilterLeaf<string>("A.B", ComparisonVariant.Equals, "a")
            ]
        );

        Dictionary<int, FilterBase> result = filter.SplitFilters(["A", "A.B"]);

        result.Keys.Count.ShouldBe(2);

        result.TryGetValue(0, out var filter0).ShouldBeTrue();
        result.TryGetValue(1, out var filter1).ShouldBeTrue();

        (filter0 as CombinationFilter)!.CombinationVariantType.ShouldBe(LogicalCombinationVariant.And);
        (filter1 as CombinationFilter)!.CombinationVariantType.ShouldBe(LogicalCombinationVariant.And);

        filter0.Leaves.Count.ShouldBe(1);
        filter0.ChildNodes.ShouldAllBe(n => n.Leaves.All(l => l.Name == "A"));

        filter1.Leaves.Count.ShouldBe(1);
        filter1.ChildNodes.ShouldAllBe(n => n.Leaves.Any(l => l.Name == "A.B"));
    }
    
    [TestMethod]
    public void SplitFilter_WithRootOr_FilterUnsplit()
    {
        var filter = new CombinationFilter(
            LogicalCombinationVariant.Or,
            [
                new CombinationFilter(LogicalCombinationVariant.And, [],
                    [
                        new FilterLeaf<string>("A", ComparisonVariant.Equals, "a"),
                        new FilterLeaf<string>("A", ComparisonVariant.Equals, "a")
                    ]
                ),
                new CombinationFilter(LogicalCombinationVariant.And, [],
                    [
                        new FilterLeaf<string>("A", ComparisonVariant.Equals, "a"),
                        new FilterLeaf<string>("A.B.C", ComparisonVariant.Equals, "a")
                    ]
                )
            ],
            [
                new FilterLeaf<string>("A", ComparisonVariant.Equals, "a"),
                new FilterLeaf<string>("A.B", ComparisonVariant.Equals, "a")
            ]
        );

        Dictionary<int, FilterBase> result = filter.SplitFilters(["A", "A.B", "A.B.C"]);

        result.Values.Count.ShouldBe(1);

        result.TryGetValue(2, out var resultFilter).ShouldBeTrue();

        (resultFilter as CombinationFilter)!.CombinationVariantType.ShouldBe(LogicalCombinationVariant.Or);

        resultFilter.Leaves.Count.ShouldBe(2);

        resultFilter.ChildNodes.Count.ShouldBe(2);

        resultFilter.ChildNodes.ShouldAllBe(n => n.Leaves.Count == 2);
    }

    [TestMethod]
    public void SplitFilter_UnrelatedLeaves_FilterUnsplit()
    {
        var filter = new CombinationFilter(
            LogicalCombinationVariant.Or,
            [
                new CombinationFilter(LogicalCombinationVariant.And, [],
                    [
                        new FilterLeaf<string>("A", ComparisonVariant.Equals, "a"),
                        new FilterLeaf<string>("D", ComparisonVariant.Equals, "a")
                    ]
                ),
                new CombinationFilter(LogicalCombinationVariant.And, [],
                    [
                        new FilterLeaf<string>("A", ComparisonVariant.Equals, "a"),
                        new FilterLeaf<string>("D", ComparisonVariant.Equals, "a")
                    ]
                )
            ],
            [
                new FilterLeaf<string>("A", ComparisonVariant.Equals, "a"),
                new FilterLeaf<string>("D", ComparisonVariant.Equals, "a")
            ]
        );

        Dictionary<int, FilterBase> result = filter.SplitFilters(["A", "A.B", "A.B.C"]);

        result.Values.Count.ShouldBe(1);

        result.TryGetValue(0, out var resultFilter).ShouldBeTrue();

        (resultFilter as CombinationFilter)!.CombinationVariantType.ShouldBe(LogicalCombinationVariant.Or);

        resultFilter.Leaves.Count.ShouldBe(2);

        resultFilter.ChildNodes.Count.ShouldBe(2);

        resultFilter.ChildNodes.ShouldAllBe(n => n.Leaves.Count == 2);
    }

    [TestMethod]
    public void ToExpressionTree_ValidIntData_CreatesExpressionTree()
    {
        var leaf = new FilterLeaf<int>(nameof(TestValueHolder.Value), ComparisonVariant.Equals, 10);
        Expression<Func<TestValueHolder, int>> accessor = FilterExpressionBuilder.CreatePropertyAccess<TestValueHolder, int>(nameof(TestValueHolder.Value));

        var accessors = new Dictionary<string, LambdaExpression> { { nameof(TestValueHolder.Value), accessor } };

        var result = leaf.ToExpressionTree(accessors);

        result.Compile().DynamicInvoke(new TestValueHolder(10, "")).ShouldBe(true);
    }

    [TestMethod]
    public void ToExpressionTree_ValidStringData_CreatesExpressionTree()
    {
        var leaf = new FilterLeaf<string>(nameof(TestValueHolder.StringValue), ComparisonVariant.Like, "EAE");
        Expression<Func<TestValueHolder, string>> accessor = FilterExpressionBuilder.CreatePropertyAccess<TestValueHolder, string>(nameof(TestValueHolder.StringValue));

        var accessors = new Dictionary<string, LambdaExpression> { { nameof(TestValueHolder.StringValue), accessor } };

        var result = leaf.ToExpressionTree(accessors);

        result.Compile().DynamicInvoke(new TestValueHolder(10, "EEAEE")).ShouldBe(true);
    }

    [TestMethod]
    public void ToExpressionTree_ValidCollectionData_CreatesExpressionTree()
    {
        var leaf = new FilterLeaf<int>(nameof(IntHolder.Value), ComparisonVariant.Gt, 0);

        var colFilter = new CollectionFilter(CollectionAggregateVariant.All, nameof(TestValueHolder.IntValues), leaf);

        Expression<Func<IntHolder, int>> accessor1 = FilterExpressionBuilder.CreatePropertyAccess<IntHolder, int>(nameof(IntHolder.Value));

        Expression<Func<TestValueHolder, ICollection<IntHolder>>> accessor2 = FilterExpressionBuilder.CreatePropertyAccess<TestValueHolder, ICollection<IntHolder>>(nameof(TestValueHolder.IntValues));

        var accessors = new Dictionary<string, LambdaExpression>
        {
            { nameof(IntHolder.Value), accessor1 },
            { nameof(TestValueHolder.IntValues), accessor2 }
        };

        var result = colFilter.ToExpressionTree(accessors);

        result.Compile().DynamicInvoke(new TestValueHolder(10, "A")).ShouldBe(true);
    }

    [TestMethod]
    public void ToExpressionTree_ValidPrimitiveCollectionData_CreatesExpressionTree()
    {
        var leaf = new FilterLeaf<int>("", ComparisonVariant.Gt, 0);

        var colFilter = new CollectionFilter(CollectionAggregateVariant.All, nameof(TestValueHolder.DirectValues), leaf);

        Expression<Func<IntHolder, int>> accessor1 = FilterExpressionBuilder.CreatePropertyAccess<IntHolder, int>(nameof(IntHolder.Value));

        Expression<Func<TestValueHolder, ICollection<int>>> accessor2 = FilterExpressionBuilder.CreatePropertyAccess<TestValueHolder, ICollection<int>>(nameof(TestValueHolder.DirectValues));

        var accessors = new Dictionary<string, LambdaExpression>
        {
            { nameof(IntHolder.Value), accessor1 },
            { nameof(TestValueHolder.DirectValues), accessor2 },
            { "", FilterExpressionBuilder.CreateIdentityExpression<TestValueHolder>() }
        };

        var result = colFilter.ToExpressionTree(accessors);

        result.Compile().DynamicInvoke(new TestValueHolder(10, "A")).ShouldBe(true);
    }

    [TestMethod]
    [DataRow(LogicalCombinationVariant.Or)]
    [DataRow(LogicalCombinationVariant.And)]
    public void ToExpressionTree_ValidDirectData_CreatesExpressionTree(LogicalCombinationVariant op)
    {
        var leaf1 = new FilterLeaf<int>(nameof(TestValueHolder.Value), ComparisonVariant.Gt, 0);
        var leaf2 = new FilterLeaf<int>(nameof(TestValueHolder.Value), ComparisonVariant.Gt, 0);
        var leaf3 = new FilterLeaf<int>(nameof(TestValueHolder.Value), ComparisonVariant.Gt, 0);
        var leaf4 = new FilterLeaf<int>(nameof(TestValueHolder.Value), ComparisonVariant.Gt, 0);

        var dirFilter = new CombinationFilter(op, [], [leaf1, leaf2, leaf3, leaf4]); 

        Expression<Func<TestValueHolder, int>> accessor1 = FilterExpressionBuilder.CreatePropertyAccess<TestValueHolder, int>(nameof(TestValueHolder.Value));

        Expression<Func<TestValueHolder, ICollection<IntHolder>>> accessor2 = FilterExpressionBuilder.CreatePropertyAccess<TestValueHolder, ICollection<IntHolder>>(nameof(TestValueHolder.IntValues));

        var accessors = new Dictionary<string, LambdaExpression>
        {
            { nameof(TestValueHolder.Value), accessor1 },
            { nameof(TestValueHolder.IntValues), accessor2 },
            { "", accessor1 }
        };

        var result = dirFilter.ToExpressionTree(accessors);

        result.Compile().DynamicInvoke(new TestValueHolder(10, "A")).ShouldBe(true);
    }

    [TestMethod]
    [DataRow(LogicalCombinationVariant.Or)]
    [DataRow(LogicalCombinationVariant.And)]
    public void ToExpressionTree_SingleLeaf_CreatesSimplifiedExpressionTree(LogicalCombinationVariant op)
    {
        var leaf1 = new FilterLeaf<int>(nameof(TestValueHolder.Value), ComparisonVariant.Gt, 0);

        var dirFilter = new CombinationFilter(op, [], [leaf1]);

        Expression<Func<TestValueHolder, int>> accessor1 = FilterExpressionBuilder.CreatePropertyAccess<TestValueHolder, int>(nameof(TestValueHolder.Value));

        Expression<Func<TestValueHolder, ICollection<IntHolder>>> accessor2 = FilterExpressionBuilder.CreatePropertyAccess<TestValueHolder, ICollection<IntHolder>>(nameof(TestValueHolder.IntValues));

        var accessors = new Dictionary<string, LambdaExpression>
        {
            { nameof(TestValueHolder.Value), accessor1 },
            { nameof(TestValueHolder.IntValues), accessor2 },
            { "", accessor1 }
        };

        var result = dirFilter.ToExpressionTree(accessors);

        result.Compile().DynamicInvoke(new TestValueHolder(10, "A")).ShouldBe(true);
    }

    [TestMethod]
    [DataRow(LogicalCombinationVariant.Or)]
    [DataRow(LogicalCombinationVariant.And)]
    public void ToExpressionTree_NonListSubProperty_CreatesSimplifiedExpressionTree(LogicalCombinationVariant op)
    {
        var leaf1 = new FilterLeaf<int>(nameof(TestValueHolder.NonList) + FilterBase.AccessLevelSeparator + nameof(TestValueHolder.Value), ComparisonVariant.Equals, 12);

        var dirFilter = new CombinationFilter(op, [], [leaf1]);

        Expression<Func<TestValueHolder, int>> accessor1 = FilterExpressionBuilder.CreatePropertyAccess<TestValueHolder, int>(nameof(TestValueHolder.Value));
        Expression<Func<TestValueHolder, IntHolder>> accessor2 = FilterExpressionBuilder.CreatePropertyAccess<TestValueHolder, IntHolder>(nameof(TestValueHolder.NonList));

        Expression<Func<IntHolder, int>> accessor3 = FilterExpressionBuilder.CreatePropertyAccess<IntHolder, int>(nameof(IntHolder.Value));

        var accessors = new Dictionary<string, LambdaExpression>
        {
            { nameof(TestValueHolder.Value), accessor1 },
            { nameof(TestValueHolder.NonList), accessor2},
            { nameof(TestValueHolder.NonList) + FilterBase.AccessLevelSeparator + nameof(IntHolder.Value), FilterExpressionBuilder.Compose(accessor2, accessor3) },
            { "", accessor1 }
        };

        var result = dirFilter.ToExpressionTree(accessors);

        result.Compile().DynamicInvoke(new TestValueHolder(10, "A")).ShouldBe(true);
    }

    private class TestValueHolder(int value, string stringValue)
    {
        public string StringValue => stringValue;

        public int Value => value;

        public List<IntHolder> IntValues { get; } = [new(1), new(2), new(3)];

        public List<int> DirectValues { get; } = [1, 2, 3];

        public IntHolder NonList { get; } = new(12);
    }

    private class IntHolder(int value)
    {
        public int Value => value;
    }
}