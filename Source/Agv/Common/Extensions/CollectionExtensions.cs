namespace Gebhardt.StoreWare.Agv.Common.Extensions
{
    /// <summary>
    /// Extensions for collection types.
    /// </summary>
    public static class CollectionExtensions
    {
        /// <summary>
        /// Add range to set (similar to <see cref="List{T}.AddRange(IEnumerable{T})"/>.
        /// </summary>
        /// <param name="set"></param>
        /// <param name="range"></param>
        /// <returns>Returns true when all elements from range has been added, returns false when there were any duplicates.</returns>
        [JetBrains.Annotations.PublicAPI]
        public static bool AddRange<T>(this HashSet<T> set, IEnumerable<T> range)
        {
            bool allAdded = true;
            foreach (var item in range)
            {
                allAdded &= set.Add(item);
            }
            return allAdded;
        }
    }
}
