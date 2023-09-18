using Gebhardt.StoreWare.Agv.Common.Services;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Domain.SemaphoreProvider;

/// <summary>
/// Provides an interface for a db lock provider on the node state.
/// </summary>
public interface INodeStateDbSemaphoreProvider : ISemaphoreProvider
{
}