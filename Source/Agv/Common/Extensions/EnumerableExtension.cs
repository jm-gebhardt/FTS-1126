using System.Runtime.CompilerServices;

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

    /// <summary>
    /// Executes the given <paramref name="action"/> for each element of <paramref name="source"/>.
    /// </summary>
    /// <typeparam name="T">The type of the elements.</typeparam>
    /// <param name="source">The sequence to iterate over.</param>
    /// <param name="action">The action to execute for each element.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
    {
        foreach (var item in source)
        {
            action(item);
        }
    }
}