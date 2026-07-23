using Gebhardt.Acc.Common.Domain.Exceptions;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Exceptions;

/// <summary>
/// Provides an exception raised when a <see cref="Models.Routes.RouteExecution"/> could not be found.
/// </summary>
public class RouteExecutionNotFoundException : NotFoundException
{
    /// <inheritdoc/>
    public RouteExecutionNotFoundException(Guid routeId)
        : base("RouteExecution", nameof(routeId), routeId)
    {
    }
}
