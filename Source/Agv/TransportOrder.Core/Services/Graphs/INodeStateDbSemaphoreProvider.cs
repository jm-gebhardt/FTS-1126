using Gebhardt.StoreWare.Agv.Common.Services;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Graphs;

/// <summary>
/// Provides an interface for a db lock provider on the node state.
/// </summary>
public interface INodeStateDbSemaphoreProvider : ISemaphoreProvider
{
}