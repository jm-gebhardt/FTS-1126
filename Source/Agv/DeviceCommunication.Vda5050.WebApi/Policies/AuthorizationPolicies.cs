using JetBrains.Annotations;
using Microsoft.AspNetCore.Authorization;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.WebApi.Policies;

/// <summary>
/// Provides authorization policies.
/// </summary>
[PublicAPI]
public static class AuthorizationPolicies
{
    private static class ClaimTypes
    {
        [PublicAPI]
        public const string ResourceAccess = "resource_access";
    }

    /// <summary>
    /// Policy name for allowing full access to lif endpoints.
    /// </summary>
    public const string LifFullAccess = "MapFullAccess";

    [PublicAPI]
    private record Policy(string PolicyName, string ClaimContent, string ClaimType);

    private static readonly IReadOnlyList<Policy> _policies =
    [
        new(LifFullAccess, "fm-lif-full", ClaimTypes.ResourceAccess)
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