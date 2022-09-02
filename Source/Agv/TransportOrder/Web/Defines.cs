namespace Gebhardt.StoreWare.Agv.TransportOrder.Web
{
    /// <summary>
    /// Utility class for definitions used across beckend and frontend
    /// </summary>
    public static class Defines
    {
        /// <summary>
        /// SignalR hub methods
        /// </summary>
        public enum HubMethods
        {
            /// <summary>
            /// Hub method for order state changed event
            /// </summary>
            OrderStateChanged,

            /// <summary>
            /// Hub method for agv state changed event
            /// </summary>
            AgvStateChanged,
            
            /// <summary>
            /// New AGV was discovered
            /// </summary>
            AgvDiscovered
        }
    }
}
