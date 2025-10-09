using System.ComponentModel;
using System.Linq.Expressions;
using Gebhardt.Acc.Common.Filter.Dtos;
using Gebhardt.Acc.Common.Filter.Exceptions;
using Gebhardt.Acc.Common.Filter.Expressions;
using Gebhardt.Acc.Common.Filter.ReadModelParser;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Any;

namespace Gebhardt.Acc.Common.Filter.FilterBuilder;

/// <summary>
/// Contains functions for translating filter dtos.
/// </summary>
[PublicAPI]
public static class FilterDtoTranslator
{
    /// <summary>
    /// Translates a filter dto to the filter object with respect to the provided read model. <br /><br />
    /// Write a from string <see cref="TypeConverter"/> for any property type in your read model that:<br />
    /// 1.) Does not natively contain one and<br />
    /// 2.) Is not itself also a read model.<br />
    /// and annotate the respective type with <see cref="TypeConverterAttribute"/> to enable translation.
    /// </summary>
    /// <typeparam name="TDomainObject">The domain object.</typeparam>
    /// <typeparam name="TReadModel">The read model.</typeparam>
    /// <param name="dto">The dto to translate.</param>
    public static FilterBase Translate<TDomainObject, TReadModel>(this FilterBaseDto dto) where TReadModel : class, TDomainObject, IFilterReadModel<TDomainObject, TReadModel>, new() where TDomainObject : class
    {
        Dictionary<string, LambdaExpression> propertyAccessors = Filter<TDomainObject, TReadModel>.PropertyAccessors;

        return FromDto(dto, propertyAccessors);
    }

    private static FilterBase FromDto(FilterBaseDto? dto, Dictionary<string, LambdaExpression> propertyAccessors)
    {
        if (dto is null)
        {
            throw new FilterBuilderException("Could not translate filter because the dto was null");
        }

        return dto switch
        {
            CombinationFilterDto combDto => FromDto(combDto, propertyAccessors),
            CollectionFilterDto collDto => FromDto(collDto, propertyAccessors),
            _ => throw new FilterBuilderException($"Unknown dto type: {dto.GetType().Name}")
        };
    }

    private static CombinationFilter FromDto(CombinationFilterDto dto, Dictionary<string, LambdaExpression> propertyAccessors)
    {
        List<IFilterLeaf> leaves = dto.Leaves.Select(l => FromDto(l, propertyAccessors)).ToList();
        List<FilterBase> children = dto.ChildNodes.Select(c => FromDto(c, propertyAccessors)).ToList();

        return new CombinationFilter(dto.Combination, children, leaves);
    }

    private static CollectionFilter FromDto(CollectionFilterDto dto, Dictionary<string, LambdaExpression> propertyAccessors)
    {
        var leaf = dto.Leaf;
        var child = dto.Child;

        if ((leaf == null && child == null) || (leaf != null && child != null))
        {
            throw new FilterBuilderException($"Could not create collection filter for  {dto.Name}, needs to have exactly one child or leaf but had {child}, {leaf}");
        }

        if (!propertyAccessors.TryGetValue(dto.Name, out var accessor))
        {
            throw new FilterBuilderException($"Could not create collection filter for {dto.Name}, no accessor registered");
        }

        if (accessor.ReturnType.GetEnumerableContentType() == null)
        {
            throw new FilterBuilderException($"Could not create collection filter for {dto.Name}, underlying property is not a collection");
        }

        return leaf != null ? new CollectionFilter(dto.Aggregation, dto.Name, FromDto(leaf, propertyAccessors)) : new CollectionFilter(dto.Aggregation, dto.Name, FromDto(child!, propertyAccessors));
    }

    private static IFilterLeaf FromDto(FilterLeafDto dto, Dictionary<string, LambdaExpression> propertyAccessors)
    {
        if (dto.Name == string.Empty)
        {
            throw new FilterBuilderException("Could not create leaf filter, name was empty");
        }

        if (!propertyAccessors.TryGetValue(dto.Name, out var accessor))
        {
            throw new FilterBuilderException($"Could not create leaf filter for {dto.Name}, no accessor registered");
        }

        bool isCollection = accessor.ReturnType.GetEnumerableContentType() != null;

        var propType = accessor.ReturnType.GetEnumerableContentType() ?? accessor.ReturnType;

        var converter = TypeDescriptor.GetConverter(propType);

        object? convertedValue;
        try
        {
            convertedValue = converter.ConvertFromString(dto.Content);
        }
        catch (ArgumentException e)
        {
            throw new FilterBuilderException($"Could not create leaf filter for {dto.Name}, {e.Message}");
        }

        if (convertedValue == null)
        {
            throw new FilterBuilderException($"Could not create leaf filter for {dto.Name}, conversion of {dto.Content} to {propType.Name} failed");
        }

        var leafType = typeof(FilterLeaf<>).MakeGenericType(propType);

        if (Activator.CreateInstance(leafType, isCollection ? string.Empty : dto.Name, dto.Variant, convertedValue) is not IFilterLeaf leaf)
        {
            throw new FilterBuilderException($"Could not create leaf {dto.Name}");
        }

        return leaf;
    }

    /// <summary>
    /// Translates this filter dto into a <see cref="OpenApiObject"/>.
    /// </summary>
    public static OpenApiObject ToOpenApiObject(this FilterBaseDto dto)
    {
        return dto switch
        {
            CollectionFilterDto coll => ToOpenApiObject(coll),
            CombinationFilterDto comb => ToOpenApiObject(comb),
            _ => throw new ArgumentOutOfRangeException(nameof(dto))
        };
    }

    private static OpenApiObject ToOpenApiObject(CollectionFilterDto dto)
    {
        var result = new OpenApiObject
        {
            [nameof(CollectionFilterDto.Name)] = new OpenApiString(dto.Name),
            [nameof(CollectionFilterDto.Aggregation)] = new OpenApiString(dto.Aggregation.ToString())
        };

        if (dto.Leaf is not null)
        {
            result.Add(nameof(CollectionFilterDto.Leaf), ToOpenApiObject(dto.Leaf));
        }

        if (dto.Child is not null)
        {
            result.Add(nameof(CollectionFilterDto.Child), ToOpenApiObject(dto.Child));
        }

        return result;
    }

    private static OpenApiObject ToOpenApiObject(CombinationFilterDto dto)
    {
        var leaves = new OpenApiArray();
        leaves.AddRange(dto.Leaves.Select(ToOpenApiObject));

        var childNodes = new OpenApiArray();
        childNodes.AddRange(dto.ChildNodes.Select(ToOpenApiObject));

        return new OpenApiObject
        {
            [nameof(CombinationFilterDto.Leaves)] = leaves,
            [nameof(CombinationFilterDto.ChildNodes)] = childNodes,
            [nameof(CombinationFilterDto.Combination)] = new OpenApiString(dto.Combination.ToString()),
        };
    }

    private static OpenApiObject ToOpenApiObject(FilterLeafDto dto)
    {
        return new OpenApiObject
        {
            [nameof(FilterLeafDto.Name)] = new OpenApiString(dto.Name),
            [nameof(FilterLeafDto.Content)] = new OpenApiString(dto.Content),
            [nameof(FilterLeafDto.Variant)] = new OpenApiString(dto.Variant.ToString())
        };
    }

    /// <summary>
    /// Adds the serialization functionality for filter dtos.
    /// </summary>
    public static IMvcBuilder AddFilterSerializer(this IMvcBuilder builder)
    {
        builder.AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new FilterJsonConverter()));
        return builder;
    }
}