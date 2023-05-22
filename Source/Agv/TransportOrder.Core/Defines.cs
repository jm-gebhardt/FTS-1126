namespace Gebhardt.StoreWare.Agv.TransportOrder.Core
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
            /// New AGV was discovered
            /// </summary>
            AgvDiscovered,

            /// <summary>
            /// Hub method for agv state and/or properties changed event
            /// </summary>
            AgvChanged,

            /// <summary>
            /// Hub method for agv position changed event
            /// </summary>
            AgvPositionChanged
        }
    }
}
