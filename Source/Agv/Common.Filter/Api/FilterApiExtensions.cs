using System.ComponentModel;
using System.Linq.Expressions;
using System.Text.Json.Nodes;
using Gebhardt.Acc.Common.Filter.Api.Dtos;
using Gebhardt.Acc.Common.Filter.Api.Swagger;
using Gebhardt.Acc.Common.Filter.Exceptions;
using Gebhardt.Acc.Common.Filter.Expressions;
using Gebhardt.Acc.Common.Filter.FilterableParser;
using Gebhardt.Acc.Common.Filter.FilterBuilder;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Gebhardt.Acc.Common.Filter.Api;

/// <summary>
/// Contains functions for translating filter dtos.
/// </summary>
[PublicAPI]
public static class FilterApiExtensions
{
    /// <param name="dto">A filter dto.</param>
    extension(FilterDto dto)
    {
        /// <summary>
        /// Translates a filter dto to the filter object with respect to the provided read model. <para />
        /// Write a <see cref="TypeConverter"/> from string for any property type in your read model that:<br />
        /// 1.) Does not natively contain one and<br />
        /// 2.) Is not itself also a read model.<br />
        /// and annotate the respective type with <see cref="TypeConverterAttribute"/> to enable this translation.
        /// </summary>
        /// <typeparam name="TDomainObject">The domain object.</typeparam>
        /// <typeparam name="TReadModel">The read model.</typeparam>
        public IFilter Translate<TDomainObject, TReadModel>() where TReadModel : class, TDomainObject, IFilterable<TDomainObject, TReadModel>, new() where TDomainObject : class
        {
            Dictionary<string, (LambdaExpression, bool)> propertyAccessors = FilterableBinder<TDomainObject, TReadModel>.PropertyAccessors;

            return FromDto(dto, propertyAccessors);
        }

        /// <summary>
        /// Translates this filter dto into an <see cref="JsonObject"/>.
        /// </summary>
        public JsonObject ToOpenApiObject()
        {
            return dto switch
            {
                CollectionFilterDto coll => ToOpenApiObject(coll),
                CombinationFilterDto comb => ToOpenApiObject(comb),
                ValueComparisonFilterDto comp => ToOpenApiObject(comp),
                _ => throw new FilterDtoParserException(nameof(dto))
            };
        }
    }

    private static IFilter FromDto(FilterDto? dto, Dictionary<string, (LambdaExpression, bool)> propertyAccessors)
    {
        if (dto is null)
        {
            throw new FilterDtoParserException("Could not translate filter because the dto was null");
        }

        return dto switch
        {
            CombinationFilterDto combDto => FromDto(combDto, propertyAccessors),
            CollectionFilterDto collDto => FromDto(collDto, propertyAccessors),
            ValueComparisonFilterDto valCompDto => FromDto(valCompDto, propertyAccessors),
            _ => throw new FilterDtoParserException($"Unknown dto type: {dto.GetType().Name}")
        };
    }

    private static CombinationFilter FromDto(CombinationFilterDto dto, Dictionary<string, (LambdaExpression, bool)> propertyAccessors)
    {
        List<IFilter> children = dto.Children.Select(c => FromDto(c, propertyAccessors)).ToList();

        return new CombinationFilter(dto.Combination, children);
    }

    private static CollectionFilter FromDto(CollectionFilterDto dto, Dictionary<string, (LambdaExpression, bool)> propertyAccessors)
    {
        var child = dto.Child;

        if (child == null)
        {
            throw new FilterDtoParserException($"Could not create collection filter for  {dto.Name}, child was null.");
        }

        if (!propertyAccessors.TryGetValue(dto.Name, out var accessorInfo) || accessorInfo is not { Item1: { } accessor })
        {
            throw new FilterDtoParserException($"Could not create collection filter for {dto.Name}, no accessor registered");
        }

        if (accessor.ReturnType.GetEnumerableContentType() == null)
        {
            throw new FilterDtoParserException($"Could not create collection filter for {dto.Name}, underlying property is not a collection");
        }

        return new CollectionFilter(dto.Aggregation, dto.Name, FromDto(child, propertyAccessors));
    }

    private static IFilter FromDto(ValueComparisonFilterDto dto, Dictionary<string, (LambdaExpression, bool)> propertyAccessors)
    {
        if (dto.Name == string.Empty)
        {
            throw new FilterDtoParserException("Could not create leaf filter, name was empty");
        }

        if (!propertyAccessors.TryGetValue(dto.Name, out var accessorInfo) || accessorInfo is not {Item1 : { } accessor})
        {
            throw new FilterDtoParserException($"Could not create leaf filter for {dto.Name}, no accessor registered");
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
            throw new FilterDtoParserException($"Could not create leaf filter for {dto.Name}, conversion of {dto.Content} to {propType.Name} failed");
        }

        var valueType = typeof(ValueComparisonFilter<>).MakeGenericType(propType);

        if (Activator.CreateInstance(valueType, isCollection ? string.Empty : dto.Name, dto.Variant, convertedValue) is not IValueComparisonFilter leaf)
        {
            throw new FilterDtoParserException($"Could not create leaf {dto.Name}");
        }

        return leaf;
    }

    private static JsonObject ToOpenApiObject(CollectionFilterDto dto)
    {
        var result = new JsonObject
        {
            [nameof(CollectionFilterDto.Name)] = dto.Name,
            [nameof(CollectionFilterDto.Aggregation)] = dto.Aggregation.ToString()
        };

        result.Add(nameof(CollectionFilterDto.Child), dto.Child.ToOpenApiObject());

        return result;
    }

    private static JsonObject ToOpenApiObject(CombinationFilterDto dto)
    {
        var children = new JsonArray();
        foreach (var jsonObject in dto.Children.Select(ToOpenApiObject))
        {
            children.Add(jsonObject);
        }

        return new JsonObject
        {
            [nameof(CombinationFilterDto.Children)] = children,
            [nameof(CombinationFilterDto.Combination)] = dto.Combination.ToString()
        };
    }

    private static JsonObject ToOpenApiObject(ValueComparisonFilterDto dto)
    {
        return new JsonObject
        {
            [nameof(ValueComparisonFilterDto.Name)] = dto.Name,
            [nameof(ValueComparisonFilterDto.Content)] = dto.Content,
            [nameof(ValueComparisonFilterDto.Variant)] = dto.Variant.ToString()
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

    /// <summary>
    /// Adds the schema extensions defined by <see cref="SwaggerFilterableProperty"/>.
    /// </summary>
    /// <param name="options"></param>
    /// <returns></returns>
    public static SwaggerGenOptions AddFilterSwaggerExtensions(this SwaggerGenOptions options)
    {
        options.SchemaFilter<SwaggerFilterableSchemaExtender>();
        return options;
    }
}