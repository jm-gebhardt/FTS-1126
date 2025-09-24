namespace Gebhardt.Acc.Common.Filter.Api.Swagger;

/// <summary>
/// Adds x-FilterableProperty and x-FilterPropertyName extensions to the annotated property in the schema if <see cref="FilterApiExtensions.AddFilterSwaggerExtensions"/> was called.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class SwaggerFilterableProperty : Attribute;