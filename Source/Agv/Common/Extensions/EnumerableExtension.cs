namespace Gebhardt.Acc.Common.Extensions;

/// <summary>
/// Provides extension methods for <see cref="IEnumerable{T}"/>
/// </summary>
public static class EnumerableExtension
{
    /// <summary>
    /// True, if the enumerable does not contain any values or is null.
    /// </summary>
    /// <typeparam name="T">The type of the enumerable.</typeparam>
    /// <param name="value">The value of the enumerable</param>
    /// <returns></returns>
    public static bool IsNullOrEmpty<T>(this IEnumerable<T>? value)
    {
        return value == null || !value.Any();
    }
}