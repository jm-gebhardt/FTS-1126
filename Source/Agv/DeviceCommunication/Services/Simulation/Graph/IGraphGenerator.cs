using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;

namespace Gebhardt.StoreWare.Agv.DeviceCommunication.Services.Simulation.Graph
{
    /// <summary>
    /// Graph Generator
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public interface IGraphGenerator
    {

        /// <summary>
        /// List of source nodes
        /// </summary>
        string[] Sources { get; }

        /// <summary>
        /// List of destination nodes
        /// </summary>
        string[] Destinations { get; }

        /// <summary>
        /// Generate Agv Graph
        /// </summary>
        /// <returns></returns>
        public AgvGraph Generate(Guid correlationId);
    }
}