using System.ComponentModel;
using System.Text.Json;

namespace Gebhardt.Acc.Common.Extensions;

/// <summary>
/// Provides extension methods for objects
/// </summary>
public static class ObjectExtension
{
    ///<summary>
    /// Provides extension methods for objects
    ///</summary>
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

        /// <summary>
        /// Get string value from json object.
        /// </summary>
        public string? GetStringValue() => customObject switch
        {
            JsonElement e => e.GetString(),
            string s => s,
            null => null,
            _ => customObject.ToString()
        };

        /// <summary>
        /// Get float value from json object.
        /// </summary>
        public float GetFloatValue() => customObject switch
        {
            JsonElement e => e.GetSingle(),
            float f => f,
            double d => (float)d,
            _ => Convert.ToSingle(customObject)
        };
    }
}