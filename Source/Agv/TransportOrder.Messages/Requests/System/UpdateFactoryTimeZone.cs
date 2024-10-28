namespace Gebhardt.Acc.TransportOrder.Messages.Requests.System;

/// <summary>
/// Represents a request to update the factory time.
/// </summary>
/// <param name="TimeZone">The timezone to set as the new factory time.</param>
public record UpdateFactoryTimeZone(TimeZoneInfo TimeZone);