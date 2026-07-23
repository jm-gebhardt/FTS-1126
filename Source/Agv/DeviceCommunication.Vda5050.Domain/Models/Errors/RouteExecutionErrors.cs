using Gebhardt.Acc.Common.Domain.Models.Errors;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Errors;

// ReSharper disable UnusedMember.Global
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
/// <summary>
/// Provides errors for route execution operations.
/// </summary>
public static class RouteExecutionErrors
{
    public static readonly Error RouteStepNotFound = new("Could not release route steps: one or more route steps are not known to this route");
    public static readonly Error ReleaseNotContiguous = new("Could not release route steps: release would leave an unreleased step followed by a released one");
}
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
