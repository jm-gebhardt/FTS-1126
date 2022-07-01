
namespace Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement
{
    /// <summary>
    /// Holds information about an AGV
    /// </summary>
    [JetBrains.Annotations.UsedImplicitly]
    public class Agv
    {
        /// <summary>
        /// Identifier of the AGV
        /// </summary>
        [JetBrains.Annotations.UsedImplicitly]
        public string AgvId { get; }

        /// <summary>
        /// Current state of AGV
        /// </summary>
        [JetBrains.Annotations.UsedImplicitly]
        public AgvState State { get; set; }

        /// <summary>
        /// Creates new AGV instence
        /// </summary>
        [JetBrains.Annotations.UsedImplicitly]
        public Agv(string agvId, AgvState state)
        {
            AgvId = agvId;
            State = state;
        }
    }
}
