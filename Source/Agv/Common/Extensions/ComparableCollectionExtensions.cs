using Microsoft.CSharp.RuntimeBinder;

namespace Gebhardt.StoreWare.Agv.Common.Extensions;

/// <summary>
/// Provides extension methods for <see cref="ICollection{T}"/>.
/// </summary>
public static class ComparableCollectionExtensions
{
    /// <summary>
    /// Calculates the median.
    /// </summary>
    /// <param name="data">The data values.</param>
    /// <returns>The median.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the IComparable does not implement +/- operators.</exception>
    public static T Median<T>(this ICollection<T> data) where T : IComparable<T>
    {
        //Sorted list of key-value pairs, where the keys are the elements of the input data and the values are the original indices.
        List<KeyValuePair<T, int>> sortedList = data.Select((x, i) => new KeyValuePair<T, int>(x, i))
            .OrderBy(x => x.Key).ToList();
        int size = sortedList.Count;
        int mid = size / 2;

        if (size % 2 != 0)
        {
            return sortedList[mid].Key;
        }

        dynamic a = sortedList[mid - 1].Key;
        dynamic b = sortedList[mid].Key;
        try
        {
            return (a + b) / 2;
        }
        catch (RuntimeBinderException)
        {
            throw new InvalidOperationException("Median calculation failed because the data elements do not support arithmetic operations.");
        }
    }
}