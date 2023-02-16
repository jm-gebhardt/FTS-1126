namespace Gebhardt.StoreWare.Agv.Common.Configurations
{
    /// <summary>
    /// Observer for changes on entities
    /// </summary>
    public class EntityChangeObserver
    {
        private static readonly Lazy<EntityChangeObserver> _lazy = new(() => new EntityChangeObserver());

        /// <summary>
        /// Singleton instance of EntityChangeObserver.
        /// </summary>
        public static EntityChangeObserver Instance => _lazy.Value;

        /// <summary>
        /// An event fired when an entity that is tracked by the associated Microsoft.EntityFrameworkCore.DbContext
        /// has moved from one Microsoft.EntityFrameworkCore.EntityState to another.
        /// </summary>
        public event EventHandler<EventArgs>? Changed;

        /// <summary>
        /// Let the EntityChangeObserver know an entity has changed.
        /// </summary>
        /// <param name="e">Event arguments for events relating to tracked Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntrys.</param>
        public void OnChanged(EventArgs e)
        {
            ThreadPool.QueueUserWorkItem(_ => Changed?.Invoke(this, e));
        }
    }
}