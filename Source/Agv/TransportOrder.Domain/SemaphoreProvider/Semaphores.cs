namespace Gebhardt.Acc.TransportOrder.Domain.SemaphoreProvider;

/// <summary>
/// Provides an interface for an action semaphore provider.
/// </summary>
public interface IAgvsSemaphore;

/// <summary>
/// Provides an interface for the charging semaphore service.
/// </summary>
public interface IChargingSemaphore;

/// <summary>
/// Provides an interface for the graph repository semaphore service.
/// </summary>
public interface IGraphRepositorySemaphore;

/// <summary>
/// Provides an interface for a map repository semaphore provider.
/// </summary>
public interface IMapRepositorySemaphore;

/// <summary>
/// Provides an interface for a db lock provider on the node state.
/// </summary>
public interface INodeStateDbSemaphore;

/// <summary>
/// Provides an interface for the parking semaphore service.
/// </summary>
public interface IParkingSemaphore;