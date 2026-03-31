using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Graphs;
using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Services.Graphs;

/// <summary>
/// Converts LIF layouts into the graph models the rest of the system understands.
/// </summary>
[PublicAPI]
public interface ILifGraphConverter
{
    /// <summary>
    /// Takes a VDA5050 domain graph and turns it into the common graph format.
    /// Edge geometry comes from the NURBS trajectories in the LIF.
    /// </summary>
    Common.Domain.Models.Graphs.AgvGraph ConvertToCommonAgvGraph(AgvGraph domainGraph);
}