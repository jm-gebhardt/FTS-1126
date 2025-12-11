using Gebhardt.Acc.Common.Filter.Enums;

namespace Gebhardt.Acc.Common.Filter.Api.Swagger;

/// <summary>
/// Marks a collection property as supporting <see cref="CollectionAggregateVariant.Where"/>> aggregation filtering.
/// <br/>
/// Adds x-SupportsWhereAggregationFilter and x-FilterablePropertyName extensions to the annotated property in the schema
/// if <see cref="FilterApiExtensions.AddFilterSwaggerExtensions"/> was called.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class SwaggerSupportsWhereAggregationFilter : Attribute;
