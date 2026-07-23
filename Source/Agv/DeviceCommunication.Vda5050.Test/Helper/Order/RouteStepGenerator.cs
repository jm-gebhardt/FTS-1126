using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.Route.AgvActions;
using Gebhardt.Acc.Common.Messages.Route.RouteSteps;
using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Helper.Order;

/// <summary>
/// Provides a helper for generating wire-level <see cref="RouteStep"/> sequences (as used in
/// <c>RoutePlanned</c>/<c>RouteUpdated</c>) for integration testing.
/// </summary>
[PublicAPI]
public static class RouteStepGenerator
{
    /// <summary>
    /// Generate a sequence of travel steps connecting the given node ids in order, hashing each
    /// node id via <c>ToGuid()</c> so the produced steps resolve against a matching lif layout
    /// built with the same node ids. Edge ids follow the pattern <c>"edge{n}"</c> (1-based).
    /// </summary>
    /// <param name="nodeIds">The node ids to travel through, in order. At least two required.</param>
    /// <param name="actionAtEnd">An action to attach to the last step's end node, if any.</param>
    public static RouteStep[] GenerateFromNodeIds(string[] nodeIds, AgvAction? actionAtEnd = null)
    {
        List<RouteStep> steps = [];
        for (int i = 1; i < nodeIds.Length; i++)
        {
            bool isLast = i == nodeIds.Length - 1;
            steps.Add(new RouteStep(
                Guid.NewGuid(),
                nodeIds[i - 1].ToGuid(),
                $"edge{i}".ToGuid(),
                nodeIds[i].ToGuid(),
                isLast ? actionAtEnd : null));
        }
        return [.. steps];
    }

    /// <summary>
    /// Append one more travel step to an existing sequence, continuing from the last step's end
    /// node to <paramref name="nextNodeId"/>. Every existing step's <c>RouteStepId</c> is kept
    /// unchanged, so a route execution can match them as the same steps across a route update.
    /// </summary>
    /// <param name="existingSteps">The existing step sequence. Must not be empty.</param>
    /// <param name="nextNodeId">The node id to extend the route to.</param>
    /// <param name="actionAtEnd">An action to attach to the new step's end node, if any.</param>
    public static RouteStep[] AppendStep(RouteStep[] existingSteps, string nextNodeId, AgvAction? actionAtEnd = null)
    {
        var lastStep = existingSteps[^1];
        var newStep = new RouteStep(
            Guid.NewGuid(),
            lastStep.EndNodeId,
            $"edge{existingSteps.Length + 1}".ToGuid(),
            nextNodeId.ToGuid(),
            actionAtEnd);
        return [.. existingSteps, newStep];
    }
}
