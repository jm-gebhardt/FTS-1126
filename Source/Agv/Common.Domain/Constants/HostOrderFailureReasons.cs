namespace Gebhardt.Acc.Common.Domain.Constants;

/// <summary>
/// Reasons for host order validation failures
/// </summary>
public static class HostOrderFailureReasons
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public const string ThereMustBeAtLeastOneAgvThatKnowsTheCompleteRouteOfAllActionsWithinTheGivenActionGroup = "There must be at least one Agv that knows the complete route of all actions within the given ActionGroup";
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}