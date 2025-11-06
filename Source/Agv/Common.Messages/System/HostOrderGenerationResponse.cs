namespace Gebhardt.Acc.Common.Messages.System;

/// <summary>
/// Represents a response including the created host orders.
/// </summary>
/// <param name="OrderIds">The list of the OrderIds of the created orders.</param>
public record HostOrderGenerationResponse(List<string> OrderIds);