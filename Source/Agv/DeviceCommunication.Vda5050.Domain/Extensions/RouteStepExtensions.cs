using Gebhardt.Acc.Common.Messages.Route.RouteSteps;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Routes;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Extensions;

/// <summary>
/// Provides extension methods for <see cref="RouteStep"/>
/// </summary>
public static class RouteStepExtensions
{
    extension(RouteStep[] routeSteps)
    {
        /// <summary>
        /// Convert an array <see cref="RouteStep"/> to a collection of <see cref="RouteStepExecution"/>.
        /// </summary>
        /// <returns></returns>
        public ICollection<RouteStepExecution> ToStepExecutions()
        {
            return routeSteps.Select(s => new RouteStepExecution(s.RouteStepId, s.StartNodeId, s.EdgeId, s.EndNodeId, s.ActionAtEndNode)).ToList();
        }
    }
}