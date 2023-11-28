namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv.Commands;

/// <summary>
/// Defines a successful result for <see cref="ResetAgvErrors"/> command
/// </summary>
public record ResetAgvErrorsResult(string Description);