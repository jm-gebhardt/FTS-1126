using Microsoft.AspNetCore.Authorization;

namespace Gebhardt.Acc.TransportOrder.WebApi.Policies;

/// <summary>
/// Provides authorization policies.
/// </summary>
public static class AuthorizationPolicies
{
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
    /// Configures authorization policies.
    /// </summary>
    /// <param name="options">The authorization options.</param>
    public static void Configure(AuthorizationOptions options)
    {
        options.AddPolicy(TriggersFullAccess, policy =>
            policy.RequireAssertion(ctx =>
                ctx.User.HasClaim(claim =>
                    claim.Type == "resource_access" &&
                    claim.Value.Contains("fm-triggers-full"))));

        options.AddPolicy(RestrictedAreasFullAccess, policy =>
            policy.RequireAssertion(ctx =>
                ctx.User.HasClaim(claim =>
                    claim.Type == "resource_access" &&
                    claim.Value.Contains("fm-restrictedAreas-full"))));

        options.AddPolicy(MapFullAccess, policy =>
            policy.RequireAssertion(ctx =>
                ctx.User.HasClaim(claim =>
                    claim.Type == "resource_access" &&
                    claim.Value.Contains("fm-map-full"))));
    }
}