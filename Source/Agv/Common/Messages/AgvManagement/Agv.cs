
namespace Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement
{
    /// <summary>
    /// Holds information about an AGV
    /// </summary>
    public class Agv
    {
        /// <summary>
        /// Identifier of the AGV
        /// </summary>
        public string AgvId { get; }

        /// <summary>
        /// Current state of AGV
        /// </summary>
        public AgvState State { get; set; }

        /// <summary>
        /// Creates new AGV instence
        /// </summary>
        public Agv(string agvId, AgvState state)
        {
            AgvId = agvId;
            State = state;
        }
    }
}
