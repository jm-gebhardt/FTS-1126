using Gebhardt.Acc.Common.Domain.Models.Errors;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Errors;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
/// <summary>
/// Provides errors for order operations.
/// </summary>
public static class OrderErrors
{
    public static readonly Error ProceedBaseEmpty = new("Could not proceed, base is empty");
    public static readonly Error ProceedNoLastNode = new("Could not proceed, no last node sequence id");
    public static readonly Error ProceedNodeNotOnRoute = new("Could not proceed, last node of agv not on current route");
    public static readonly Error ProceedEdgeNotFound = new("Could not proceed, edge not in current route");
    public static readonly Error InitializationFailed = new("Initialization failed");
    public static readonly Error OrderRouteUpdateStitchingNodeNotFound = new("Route update failed: End of current base not in new route");
    public static readonly Error OrderRouteUpdateOrderCompleted= new("Route update failed: Order already completed");
}
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member