using System.Globalization;
using System.Text.Json;
using Gebhardt.Acc.Common.Filter.Dtos;
using Gebhardt.Acc.Common.Filter.Enums;
using Gebhardt.Acc.Common.Filter.Exceptions;
using Gebhardt.Acc.Common.Filter.FilterBuilder;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.Acc.CommonTest.Filter;

[TestClass]
public class FilterDtoTranslatorTests
{
    [TestMethod]
    public void TranslateFilterDto_ValidData_TranslatesFilter()
    {
        var dto = new CombinationFilterDto( [], [new FilterLeafDto(nameof(TestJoinSourceReadModel.TjtFKey) + FilterBase.AccessLevelSeparator + nameof(TestJoinTargetReadModel.Value), ComparisonVariant.Equals, "10")], LogicalCombinationVariant.And);

        var result = dto.Translate<TestJoinSourceEntity, TestJoinSourceReadModel>();
        result.ShouldNotBeNull();
    }

    [TestMethod]
    public void TranslateFilterDto_DateValue_TranslatesFilter()
    {
        var dto = new CombinationFilterDto([], [new FilterLeafDto(nameof(SubPropertyHelperReadModel.Date), ComparisonVariant.Equals, DateTime.UtcNow.ToString(CultureInfo.CurrentCulture))], LogicalCombinationVariant.And);

        var result = dto.Translate<SubPropertyHelper, SubPropertyHelperReadModel>();
        result.ShouldNotBeNull();
    }

    [TestMethod]
    public void TranslateFilterDto_MultipleChildren_TranslatesFilter()
    {
        var dto = new CombinationFilterDto([
            new CombinationFilterDto([], [new FilterLeafDto(nameof(TestJoinSourceReadModel.TjtFKey) + FilterBase.AccessLevelSeparator + nameof(TestJoinTargetReadModel.Value), ComparisonVariant.Equals, "10")], LogicalCombinationVariant.And),
            new CombinationFilterDto([], [new FilterLeafDto(nameof(TestJoinSourceReadModel.TjtFKey) + FilterBase.AccessLevelSeparator + nameof(TestJoinTargetReadModel.Value), ComparisonVariant.Equals, "10")], LogicalCombinationVariant.And),
            new CombinationFilterDto([], [new FilterLeafDto(nameof(TestJoinSourceReadModel.TjtFKey) + FilterBase.AccessLevelSeparator + nameof(TestJoinTargetReadModel.Value), ComparisonVariant.Equals, "10")], LogicalCombinationVariant.And),
            new CombinationFilterDto([], [new FilterLeafDto(nameof(TestJoinSourceReadModel.TjtFKey) + FilterBase.AccessLevelSeparator + nameof(TestJoinTargetReadModel.Value), ComparisonVariant.Equals, "10")], LogicalCombinationVariant.And)
        ], [new FilterLeafDto(nameof(TestJoinSourceReadModel.TjtFKey) + FilterBase.AccessLevelSeparator + nameof(TestJoinTargetReadModel.Value), ComparisonVariant.Equals, "10")], LogicalCombinationVariant.And);

        var result = dto.Translate<TestJoinSourceEntity, TestJoinSourceReadModel>();
        result.ShouldNotBeNull();
        result.Leaves.Count.ShouldBe(1);
        result.ChildNodes.Count.ShouldBe(4);
        result.ChildNodes.ShouldAllBe(n => n.Leaves.Count == 1);
    }

    [TestMethod]
    public void TranslateFilterDto_ValueMismatch_Throws()
    {
        var dto = new CombinationFilterDto([], [new FilterLeafDto(nameof(TestJoinSourceReadModel.TjtFKey) + FilterBase.AccessLevelSeparator + nameof(TestJoinTargetReadModel.Value), ComparisonVariant.Equals, DateTime.UtcNow.ToString(CultureInfo.InvariantCulture))], LogicalCombinationVariant.And);

        Should.Throw<FilterBuilderException>(() => dto.Translate<TestJoinSourceEntity, TestJoinSourceReadModel>());
    }

    [TestMethod]
    public void TranslateFilterDto_CollectionFilterValidData_TranslatesFilter()
    {
        var dto = new CollectionFilterDto(new FilterLeafDto(nameof(TestCollectionEntityReadModel.Values) + FilterBase.AccessLevelSeparator + nameof(TestDbEntityReadModel.IntValue), ComparisonVariant.Equals, "1"), nameof(TestCollectionEntityReadModel.Values), CollectionAggregateVariant.All);

        var result =  dto.Translate<TestCollectionEntity, TestCollectionEntityReadModel>() as CollectionFilter;
        result.ShouldNotBeNull();
        result.Leaves.Count.ShouldBe(1);
        result.CollectionName.ShouldBe(nameof(TestCollectionEntityReadModel.Values));
        result.Leaves[0].Name.ShouldBe(nameof(TestCollectionEntityReadModel.Values) + FilterBase.AccessLevelSeparator + nameof(TestDbEntityReadModel.IntValue));
        (result.Leaves[0] as FilterLeaf<int>).ShouldNotBeNull();
    }

    [TestMethod]
    public void TranslateFilterDto_CollectionFilterWithNoCollection_Throws()
    {
        var dto = new CollectionFilterDto(new FilterLeafDto(nameof(TestJoinSourceReadModel.TjtFKey) + FilterBase.AccessLevelSeparator + nameof(TestJoinTargetReadModel.Value), ComparisonVariant.Equals, "1"), nameof(TestJoinSourceReadModel.TjtFKey), CollectionAggregateVariant.All);

        Should.Throw<FilterBuilderException>(() => dto.Translate<TestJoinSourceEntity, TestJoinSourceReadModel>());
    }

    [TestMethod]
    public void JsonSerialize_ValidData_RoundTripIsIdempotent()
    {
        FilterBaseDto dto = new CombinationFilterDto([
            new CombinationFilterDto([], [new FilterLeafDto("ABC", ComparisonVariant.Equals, "101")], LogicalCombinationVariant.Or),
            new CollectionFilterDto(new CombinationFilterDto([], [], LogicalCombinationVariant.And), "TestCollectionName", CollectionAggregateVariant.Any),
            new CollectionFilterDto(new FilterLeafDto("GHI", ComparisonVariant.Neq, "Test"), "TestCollectionName", CollectionAggregateVariant.Any)
        ], [new FilterLeafDto("DEF", ComparisonVariant.Equals, "10")], LogicalCombinationVariant.And);

        var opt = new JsonSerializerOptions { Converters = { new FilterJsonConverter() }, WriteIndented = true};

        string serialized = JsonSerializer.Serialize(dto, opt);

        var deserialized = JsonSerializer.Deserialize<FilterBaseDto>(serialized, opt);

        deserialized.ShouldBeEquivalentTo(dto);
    }
}