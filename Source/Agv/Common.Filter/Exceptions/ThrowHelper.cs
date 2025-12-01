namespace Gebhardt.Acc.Common.Filter.Exceptions;

/// <summary>
/// Contains helper functions for exception throwing.
/// </summary>
internal static class ThrowHelper
{
    /// <summary>
    /// Throws a <see cref="FilterParserException"/> if the provided type is a value type.
    /// </summary>
    internal static void ThrowIfValueType(Type? type)
    {
        if (type?.IsValueType == true)
        {
            throw new FilterParserException($"{type.FullName} is a value type where a class was expected");
        }
    }
}