using Microsoft.AspNetCore.Authorization;

namespace Gebhardt.Acc.TransportOrder.WebApi.Policies;

/// <summary>
/// Provides authorization policies.
/// </summary>
public static class AuthorizationPolicies
{
    /// <summary>
    /// Policy name for allowing editing background scenes.
    /// </summary>
    public const string BackGroundEdit = "BackgroundEdit";

    /// <summary>
    /// Policy name for allowing full access to trigger endpoints.
    /// </summary>
    public const string TriggersFullAccess = "TriggersFullAccess";

    /// <summary>
    /// Policy name for allowing full access to restricted area endpoints.
    /// </summary>
    public const string RestrictedAreasFullAccess = "RestrictedAreasFullAccess";

    /// <summary>
    /// Policy name for allowing full access to map endpoints, like background image updates.
    /// </summary>
    public const string MapFullAccess = "MapFullAccess";

    /// <summary>
    /// Policy name for allowing full access to maintenance log endpoints.
    /// </summary>
    public const string MaintenanceLogFullAccess = "MaintenanceLogFullAccess";

    /// <summary>
    /// Policy name for allowing updates to node properties.
    /// </summary>
    public const string NodePropertiesUpdate = "NodePropertiesUpdate";

    /// <summary>
    /// Policy name for allowing full access to queue endpoints.
    /// </summary>
    public const string QueueFullAccess = "QueueFullAccess";

    /// <summary>
    /// Policy name for allowing queue endpoints for repair.
    /// </summary>
    public const string QueueRepair = "QueueRepair";

    private static class ClaimTypes
    {
        public const string ResourceAccess = "resource_access";
    }

    private record Policy(string PolicyName, string ClaimContent, string ClaimType);

    private static readonly IReadOnlyList<Policy> _policies =
    [
        new(TriggersFullAccess, "fm-triggers-full", ClaimTypes.ResourceAccess),
        new(BackGroundEdit, "fm-background-edit", ClaimTypes.ResourceAccess),
        new(RestrictedAreasFullAccess, "fm-restrictedAreas-full", ClaimTypes.ResourceAccess),
        new(MapFullAccess, "fm-map-full", ClaimTypes.ResourceAccess),
        new(NodePropertiesUpdate, "fm-nodeProperties-update", ClaimTypes.ResourceAccess),
        new(MaintenanceLogFullAccess, "fm-maintenanceLogs-full", ClaimTypes.ResourceAccess),
        new(QueueFullAccess, "fm-queues-full", ClaimTypes.ResourceAccess),
        new(QueueRepair, "fm-queues-repair", ClaimTypes.ResourceAccess)
    ];

    private static void AddNewPolicy(this AuthorizationOptions options, Policy newPolicy)
    {
        options.AddPolicy(newPolicy.PolicyName, policy =>
            policy.RequireAssertion(ctx =>
                ctx.User.HasClaim(claim =>
                    claim.Type == newPolicy.ClaimType &&
                    claim.Value.Contains(newPolicy.ClaimContent))));
    }

    /// <summary>
    /// Configures authorization policies.
    /// </summary>
    /// <param name="options">The authorization options.</param>
    public static void Configure(AuthorizationOptions options)
    {
        foreach (var policy in _policies)
        {
            options.AddNewPolicy(policy);
        }
    }
}