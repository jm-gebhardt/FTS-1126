
namespace Gebhardt.StoreWare.Agv.Common.Messages.Actions
{
    /// <summary>
    /// Used to identify that an event belongs to a parent object
    /// </summary>
    /// <typeparam name="TKey">The type of the ParentId used</typeparam>
    [JetBrains.Annotations.PublicAPI]
    public interface IActionEventParent<out TKey>
    {
        /// <summary>
        /// Returns the ParentId for the action
        /// </summary>
        TKey ParentId { get; }
    }
}
