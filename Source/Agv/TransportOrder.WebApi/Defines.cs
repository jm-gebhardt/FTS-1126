namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi
{
    /// <summary>
    /// Utility class for definitions used across backend and frontend
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
            /// New AGV was discovered
            /// </summary>
            AgvDiscovered,

            /// <summary>
            /// Hub method for agv state changed event
            /// </summary>
            AgvStateChanged,

            /// <summary>
            /// Hub method for agv position changed event
            /// </summary>
            AgvPositionChanged
        }
    }
}
