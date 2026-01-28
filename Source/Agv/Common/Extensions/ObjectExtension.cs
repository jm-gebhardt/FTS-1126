using System.ComponentModel;

namespace Gebhardt.Acc.Common.Extensions;

/// <summary>
/// Provides extension methods for objects
/// </summary>
public static class ObjectExtension
{
    extension(object customObject)
    {
        /// <summary>
        /// Converts any object into a dictionary of its properties.
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, object?> AsDict(string[]? skippedProperties=null)
        {
            string[] skipped = skippedProperties ?? [];

            return TypeDescriptor.GetProperties(customObject)
                .Cast<PropertyDescriptor>()
                .Where(property => !skipped.Contains(property.Name))
                .ToDictionary(
                    property => property.Name,
                    property => property.GetValue(customObject)
                );
        }
    }
}