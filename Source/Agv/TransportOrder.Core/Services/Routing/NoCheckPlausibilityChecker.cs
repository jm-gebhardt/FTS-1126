using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Routing
{
    /// <summary>
    /// Considers any request plausible
    /// </summary>
    public class NoCheckPlausibilityChecker : IRoutingPlausibilityChecker
    {
        /// <summary>
        /// No test, let it all pass
        /// </summary>
        /// <param name="start">Does not matter</param>
        /// <param name="end">Does not matter</param>
        /// <returns>True</returns>
        public bool IsPlausible(AgvGraph.Node start, AgvGraph.Node end)
        {
            return true;
        }
    }
}
