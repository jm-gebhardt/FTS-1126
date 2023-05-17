
using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.Common.Messages.Actions
{
    /// <summary>
    /// State of an AGV action
    /// </summary>
    public enum ActionState
    {
        /// <summary>
        /// Action created
        /// </summary>
        Created,

        /// <summary>
        /// Action transmitted to DeviceCommunication
        /// </summary>
        [PublicAPI]
        Started,

        /// <summary>
        /// Action completed
        /// </summary>
        [PublicAPI]
        Completed
    }
}
