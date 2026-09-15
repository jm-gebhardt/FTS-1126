namespace Gebhardt.Acc.TransportOrder.Domain.SemaphoreProvider;

/// <summary>
/// Provides an interface for an action semaphore provider.
/// </summary>
public interface IAgvsSemaphore;

/// <summary>
/// Provides an interface for the bounding box calculation semaphore service.
/// </summary>
public interface IBoundingBoxCalculationSemaphore;

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

/// <summary>
/// Provides an interface for the queueing semaphore service.
/// </summary>
public interface IQueueingSemaphore;

/// <summary>
/// Provides an interface for the Doppelspiel semaphore service.
/// </summary>
public interface IDoppelspielSemaphore;

/// <summary>
/// Provides an interface for the target selector semaphore service.
/// </summary>
public interface ITargetSelectionSemaphore;

/// <summary>
/// Provides an interface for the waiting handler semaphore service.
/// </summary>
public interface IWaitingHandlerSemaphore;