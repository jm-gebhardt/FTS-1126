namespace Gebhardt.Acc.TransportOrder.Messages.Requests.System;

/// <summary>
/// Represents a response for <see cref="UpdateFactoryTimeZone"/>
/// </summary>
/// <param name="Result">If the operation was successful.</param>
public record UpdatedFactoryTimeZone(bool Result);