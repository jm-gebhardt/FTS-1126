using System.Globalization;
using System.Text.Json;
using Gebhardt.Acc.Common.Filter.Api;
using Gebhardt.Acc.Common.Filter.Api.Dtos;
using Gebhardt.Acc.Common.Filter.Api.Swagger;
using Gebhardt.Acc.Common.Filter.Enums;
using Gebhardt.Acc.Common.Filter.Exceptions;
using Gebhardt.Acc.Common.Filter.FilterBuilder;
using Shouldly;

namespace Gebhardt.Acc.CommonTest.Filter;

[TestClass]
public class FilterExtensionsTests
{
    [TestMethod]
    public void TranslateFilterDto_ValidData_TranslatesFilter()
    {
        var dto = new CombinationFilterDto([new ValueComparisonFilterDto(nameof(TestJoinSourceReadModel.TjtFKey) + IFilter.AccessLevelSeparator + nameof(TestJoinTargetReadModel.Value), ComparisonVariant.Equals, "10")], LogicalCombinationVariant.And);

        var result = dto.Translate<TestJoinSourceEntity, TestJoinSourceReadModel>();
        result.ShouldNotBeNull();
    }

    [TestMethod]
    public void TranslateFilterDto_DateValue_TranslatesFilter()
    {
        var dto = new CombinationFilterDto( [new ValueComparisonFilterDto(nameof(SubPropertyHelperReadModel.Date), ComparisonVariant.Equals, DateTime.UtcNow.ToString(CultureInfo.CurrentCulture))], LogicalCombinationVariant.And);

        var result = dto.Translate<SubPropertyHelper, SubPropertyHelperReadModel>();
        result.ShouldNotBeNull();
    }

    [TestMethod]
    public void TranslateFilterDto_MultipleChildren_TranslatesFilter()
    {
        var dto = new CombinationFilterDto([
            new CombinationFilterDto([new ValueComparisonFilterDto(nameof(TestJoinSourceReadModel.TjtFKey) + IFilter.AccessLevelSeparator + nameof(TestJoinTargetReadModel.Value), ComparisonVariant.Equals, "10")], LogicalCombinationVariant.And),
            new CombinationFilterDto([new ValueComparisonFilterDto(nameof(TestJoinSourceReadModel.TjtFKey) + IFilter.AccessLevelSeparator + nameof(TestJoinTargetReadModel.Value), ComparisonVariant.Equals, "10")], LogicalCombinationVariant.And),
            new CombinationFilterDto([new ValueComparisonFilterDto(nameof(TestJoinSourceReadModel.TjtFKey) + IFilter.AccessLevelSeparator + nameof(TestJoinTargetReadModel.Value), ComparisonVariant.Equals, "10")], LogicalCombinationVariant.And),
            new CombinationFilterDto([new ValueComparisonFilterDto(nameof(TestJoinSourceReadModel.TjtFKey) + IFilter.AccessLevelSeparator + nameof(TestJoinTargetReadModel.Value), ComparisonVariant.Equals, "10")], LogicalCombinationVariant.And),
        new ValueComparisonFilterDto(nameof(TestJoinSourceReadModel.TjtFKey) + IFilter.AccessLevelSeparator + nameof(TestJoinTargetReadModel.Value), ComparisonVariant.Equals, "10")
        ], LogicalCombinationVariant.And);

        var result = dto.Translate<TestJoinSourceEntity, TestJoinSourceReadModel>() as CombinationFilter;
        result.ShouldNotBeNull();
        result.Children.Count.ShouldBe(5);
        foreach (var child in result.Children)
        {
            if (child is CombinationFilter comb)
            {
                comb.Children.Count.ShouldBe(1);
            }
        }
    }

    [TestMethod]
    public void TranslateFilterDto_TypeMismatch_Throws()
    {
        var dto = new CombinationFilterDto([new ValueComparisonFilterDto(nameof(TestJoinSourceReadModel.TjtFKey) + IFilter.AccessLevelSeparator + nameof(TestJoinTargetReadModel.Value), ComparisonVariant.Equals, DateTime.UtcNow.ToString(CultureInfo.InvariantCulture))], LogicalCombinationVariant.And);

        Should.Throw<FilterBuilderException>(() => dto.Translate<TestJoinSourceEntity, TestJoinSourceReadModel>());
    }

    [TestMethod]
    public void TranslateFilterDto_CollectionFilterValidData_TranslatesFilter()
    {
        var dto = new CollectionFilterDto(new ValueComparisonFilterDto(nameof(TestCollectionEntityReadModel.Values) + IFilter.AccessLevelSeparator + nameof(TestDbEntityReadModel.IntValue), ComparisonVariant.Equals, "1"), nameof(TestCollectionEntityReadModel.Values), CollectionAggregateVariant.All);

        var result =  dto.Translate<TestCollectionEntity, TestCollectionEntityReadModel>() as CollectionFilter;
        result.ShouldNotBeNull();
        result.CollectionName.ShouldBe(nameof(TestCollectionEntityReadModel.Values));
        (result.Child is ValueComparisonFilter<int>).ShouldBeTrue();
        if (result.Child is ValueComparisonFilter<int> child)
        {
            child.Name.ShouldBe(nameof(TestCollectionEntityReadModel.Values) + IFilter.AccessLevelSeparator + nameof(TestDbEntityReadModel.IntValue));
        }
    }

    [TestMethod]
    public void TranslateFilterDto_CollectionFilterWithNoCollection_Throws()
    {
        var dto = new CollectionFilterDto(new ValueComparisonFilterDto(nameof(TestJoinSourceReadModel.TjtFKey) + IFilter.AccessLevelSeparator + nameof(TestJoinTargetReadModel.Value), ComparisonVariant.Equals, "1"), nameof(TestJoinSourceReadModel.TjtFKey), CollectionAggregateVariant.All);

        Should.Throw<FilterDtoParserException>(() => dto.Translate<TestJoinSourceEntity, TestJoinSourceReadModel>());
    }

    [TestMethod]
    public void JsonSerialize_ValidData_RoundTripIsIdempotent()
    {
        FilterDto dto = new CombinationFilterDto([
            new CombinationFilterDto([new ValueComparisonFilterDto("ABC", ComparisonVariant.Equals, "101")], LogicalCombinationVariant.Or),
            new CollectionFilterDto(new CombinationFilterDto([], LogicalCombinationVariant.And), "TestCollectionName", CollectionAggregateVariant.Any),
            new CollectionFilterDto(new ValueComparisonFilterDto("GHI", ComparisonVariant.Neq, "Test"), "TestCollectionName", CollectionAggregateVariant.Any),
            new ValueComparisonFilterDto("DEF", ComparisonVariant.Equals, "10")], LogicalCombinationVariant.And);

        var opt = new JsonSerializerOptions { Converters = { new FilterJsonConverter() }, WriteIndented = true};

        string serialized = JsonSerializer.Serialize(dto, opt);

        var deserialized = JsonSerializer.Deserialize<FilterDto>(serialized, opt);

        deserialized.ShouldBeEquivalentTo(dto);
    }
}