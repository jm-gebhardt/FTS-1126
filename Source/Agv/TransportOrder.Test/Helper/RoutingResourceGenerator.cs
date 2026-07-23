using Gebhardt.Acc.TransportOrder.Domain.Models.NewGraph;

namespace Gebhardt.Acc.TransportOrder.Test.Helper;

/// <summary>
/// A helper class for generating routing resources for testing purposes.
/// </summary>
public static class RoutingResourceGenerator
{
    public static RoutingResource GenerateResource(params RoutingResource[] conflictingResources)
    {
        var parent = new Node(Guid.NewGuid(), null, null, 0, 0, null);
        var agvType = new AgvType(Guid.NewGuid(), "TestAgvType");
        var resource = new RoutingNode(Guid.NewGuid(), parent, agvType, new LoadState(false), 0);

        foreach (var conflictingResource in conflictingResources)
        {
            resource.AddConflictingResource(conflictingResource);
        }

        return resource;
    }
}